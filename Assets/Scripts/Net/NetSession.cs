using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace AshenVale
{
    // Online co-op, host-authoritative. The host runs the one real World. Clients send their PlayerInput
    // (30 times a second) and inventory/shop commands; the host sends each client a snapshot of what it can see
    // (30 times a second) and that client's own hero data (twice a second, so the client can save it).
    //
    // Uses Netcode for GameObjects only as a connection + message layer (named messages), with no NetworkObjects,
    // because the World is plain C#. The NetworkManager is created in code, so the scene needs nothing.
    //
    // Messages (all start with "av."):
    //   client -> host   join (hero JSON) · input (axes + presses) · cmd (equip / sell / buy / waystone travel)
    //   host -> client   welcome (your player id) · snap (world snapshot) · char (your hero JSON)
    public sealed class NetSession
    {
        public enum Role { None, Host, Client }

        public const ushort DirectPort = 7777;
        public const int MaxPlayers = 4;
        const float SnapshotInterval = 1f / 30f, CharacterInterval = .5f, InputInterval = 1f / 30f;
        const string MsgJoin = "av.join", MsgWelcome = "av.welcome", MsgInput = "av.input", MsgCommand = "av.cmd",
                     MsgSnapshot = "av.snap", MsgCharacter = "av.char";

        public Role CurrentRole { get; private set; }
        public bool Online { get; private set; }
        public int LocalPlayerId { get; private set; }     // client: the id the host gave our hero
        public bool HasCharacter { get; private set; }     // client: we have our hero's full data (safe to save)
        public Action<string> onFailed;                     // could not connect, or the connection was lost
        public int hostPlayerId = -1;                       // host: the host's own hero (joiners appear on its map)

        string status = "";
        readonly World world;
        readonly OnlineSession onlineSession = new OnlineSession();
        NetworkManager nm;
        UnityTransport transport;
        bool handlersRegistered, joinSent, welcomed;
        int generation;                                     // bumped by Leave(), so late async results are ignored
        CharacterData joinData, pendingCharacter;
        float snapshotTimer, characterTimer, inputTimer, snapshotAge;

        readonly Dictionary<ulong, int> clientPlayers = new Dictionary<ulong, int>();   // host: connection -> player id
        readonly Dictionary<int, PlayerInput> remoteInputs = new Dictionary<int, PlayerInput>();
        readonly List<int> scratchIds = new List<int>();
        readonly List<SimEvent> pendingEvents = new List<SimEvent>();
        readonly HashSet<int> seen = new HashSet<int>();
        readonly Dictionary<int, MonsterState> monsterLookup = new Dictionary<int, MonsterState>();
        readonly Dictionary<int, DropState> dropLookup = new Dictionary<int, DropState>();
        readonly Dictionary<int, ProjectileState> shotLookup = new Dictionary<int, ProjectileState>();

        public NetSession(World world)
        {
            this.world = world;
            LocalPlayerId = -1;
        }

        public bool ClientReady { get { return CurrentRole == Role.Client && welcomed && world.GetPlayer(LocalPlayerId) != null; } }
        public float SnapshotAlpha { get { return Mathf.Clamp01(snapshotAge / SnapshotInterval); } }
        public int PlayerCount { get { return clientPlayers.Count + 1; } }

        // One line for the HUD: join code, player count or connection state.
        public string StatusLine
        {
            get
            {
                if (CurrentRole == Role.Host) return status + "   ·   " + PlayerCount + (PlayerCount == 1 ? " PLAYER" : " PLAYERS");
                return status;
            }
        }

        // ---------------------------------------------------------------- starting and stopping

        void EnsureManager()
        {
            if (nm != null) return;
            var go = new GameObject("Network");
            go.SetActive(false);                                  // configure before NetworkManager wakes up
            UnityEngine.Object.DontDestroyOnLoad(go);
            transport = go.AddComponent<UnityTransport>();
            nm = go.AddComponent<NetworkManager>();
            nm.NetworkConfig = new NetworkConfig { NetworkTransport = transport, EnableSceneManagement = false, ConnectionApproval = false };
            go.SetActive(true);
            nm.OnClientDisconnectCallback += OnClientDisconnect;
        }

        // Host on this PC / local network: friends join with this PC's IP address, port 7777.
        public bool HostDirect(int hostPlayer)
        {
            Leave();
            EnsureManager();
            hostPlayerId = hostPlayer;
            transport.SetConnectionData("127.0.0.1", DirectPort, "0.0.0.0");
            if (!nm.StartHost()) return false;
            CurrentRole = Role.Host;
            status = "HOSTING ON THIS PC (PORT " + DirectPort + ")";
            return true;
        }

        // Host online: creates a Relay session; the join code appears in the HUD once it's ready.
        public async void HostOnline(int hostPlayer)
        {
            Leave();
            EnsureManager();
            hostPlayerId = hostPlayer;
            CurrentRole = Role.Host;
            Online = true;
            status = "CREATING ONLINE GAME...";
            int gen = generation;
            try
            {
                await onlineSession.HostAsync(MaxPlayers);
                if (gen != generation) { onlineSession.Leave(); return; }
                status = "ONLINE  ·  JOIN CODE: " + onlineSession.Code;
            }
            catch (Exception e)
            {
                if (gen != generation) return;
                Debug.LogException(e);
                Leave();
                if (onFailed != null) onFailed("Could not create an online game: " + OnlineSession.Explain(e));
            }
        }

        public bool JoinDirect(string address, CharacterData hero)
        {
            Leave();
            EnsureManager();
            joinData = hero;
            string ip = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim();
            transport.SetConnectionData(ip, DirectPort);
            if (!nm.StartClient()) return false;
            CurrentRole = Role.Client;
            status = "CONNECTED TO " + ip;
            return true;
        }

        public async void JoinOnline(string code, CharacterData hero)
        {
            Leave();
            EnsureManager();
            joinData = hero;
            CurrentRole = Role.Client;
            Online = true;
            status = "ONLINE  ·  CODE " + (code ?? "").Trim().ToUpperInvariant();
            int gen = generation;
            try
            {
                await onlineSession.JoinAsync(code);
                if (gen != generation) onlineSession.Leave();
            }
            catch (Exception e)
            {
                if (gen != generation) return;
                Debug.LogException(e);
                Leave();
                if (onFailed != null) onFailed("Could not join: " + OnlineSession.Explain(e));
            }
        }

        // Stops hosting or leaves the game. Safe to call at any time.
        public void Leave()
        {
            generation++;
            CurrentRole = Role.None;                              // first, so disconnect callbacks during shutdown are ignored
            Online = false;
            onlineSession.Leave();
            if (nm != null && (nm.IsListening || nm.IsServer || nm.IsClient) && !nm.ShutdownInProgress) nm.Shutdown();
            handlersRegistered = joinSent = welcomed = false;
            HasCharacter = false;
            LocalPlayerId = -1;
            joinData = pendingCharacter = null;
            clientPlayers.Clear();
            remoteInputs.Clear();
            pendingEvents.Clear();
            snapshotTimer = characterTimer = inputTimer = snapshotAge = 0;
            status = "";
        }

        void OnClientDisconnect(ulong clientId)
        {
            if (CurrentRole == Role.Host)
            {
                int playerId;
                if (clientId == NetworkManager.ServerClientId || !clientPlayers.TryGetValue(clientId, out playerId)) return;
                world.RemovePlayer(playerId);
                remoteInputs.Remove(playerId);
                clientPlayers.Remove(clientId);
            }
            else if (CurrentRole == Role.Client)
            {
                string reason = nm != null ? nm.DisconnectReason : "";
                string message = welcomed ? "Lost connection to the host." : "Could not connect to the host.";
                if (!string.IsNullOrEmpty(reason)) message += " (" + reason + ")";
                // Tell the game first, while our hero data is still here, so it can save before we reset.
                if (onFailed != null) onFailed(message);
                Leave();
            }
        }

        // Called every frame by the game.
        public void Update(float dt)
        {
            if (CurrentRole == Role.None || nm == null) return;
            if (!handlersRegistered && nm.IsListening && nm.CustomMessagingManager != null) RegisterHandlers();
            if (CurrentRole != Role.Client) return;
            snapshotAge += dt;
            if (!joinSent && handlersRegistered && nm.IsConnectedClient && joinData != null)
            {
                joinSent = true;
                var w = new FastBufferWriter(1024, Allocator.Temp, 65536);
                try
                {
                    w.WriteValueSafe(JsonUtility.ToJson(joinData));
                    nm.CustomMessagingManager.SendNamedMessage(MsgJoin, NetworkManager.ServerClientId, w, NetworkDelivery.ReliableFragmentedSequenced);
                }
                finally { w.Dispose(); }
            }
        }

        void RegisterHandlers()
        {
            var messages = nm.CustomMessagingManager;
            if (CurrentRole == Role.Host)
            {
                messages.RegisterNamedMessageHandler(MsgJoin, OnJoin);
                messages.RegisterNamedMessageHandler(MsgInput, OnInput);
                messages.RegisterNamedMessageHandler(MsgCommand, OnCommand);
            }
            else
            {
                messages.RegisterNamedMessageHandler(MsgWelcome, OnWelcome);
                messages.RegisterNamedMessageHandler(MsgSnapshot, OnSnapshot);
                messages.RegisterNamedMessageHandler(MsgCharacter, OnCharacter);
            }
            handlersRegistered = true;
        }

        // ---------------------------------------------------------------- host side

        void OnJoin(ulong sender, FastBufferReader reader)
        {
            if (CurrentRole != Role.Host || clientPlayers.ContainsKey(sender)) return;
            if (clientPlayers.Count >= MaxPlayers - 1) { nm.DisconnectClient(sender); return; }
            reader.ReadValueSafe(out string json);
            CharacterData data = null;
            try { data = JsonUtility.FromJson<CharacterData>(json); } catch (Exception) { }
            if (data == null) { nm.DisconnectClient(sender); return; }

            var host = world.GetPlayer(hostPlayerId);
            var p = world.AddPlayer(SaveCodec.ToCharacter(data), host != null ? host.map : Maps.Emberfields, data.hp, data.mp);
            clientPlayers[sender] = p.id;
            remoteInputs[p.id] = new PlayerInput();

            var w = new FastBufferWriter(16, Allocator.Temp);
            try
            {
                w.WriteValueSafe(p.id);
                nm.CustomMessagingManager.SendNamedMessage(MsgWelcome, sender, w, NetworkDelivery.ReliableSequenced);
            }
            finally { w.Dispose(); }
            SendCharacter(sender, p);
        }

        void OnInput(ulong sender, FastBufferReader reader)
        {
            int playerId;
            if (CurrentRole != Role.Host || !clientPlayers.TryGetValue(sender, out playerId)) return;
            reader.ReadValueSafe(out float moveX);
            reader.ReadValueSafe(out float moveY);
            reader.ReadValueSafe(out ushort presses);
            PlayerInput input;
            remoteInputs.TryGetValue(playerId, out input);
            input.moveX = Mathf.Clamp(moveX, -1f, 1f);
            input.moveY = Mathf.Clamp(moveY, -1f, 1f);
            // Presses add up until the next tick uses them, so a quick tap is never lost.
            if ((presses & 1) != 0) input.jump = true;
            if ((presses & 2) != 0) input.attack = true;
            if ((presses & 4) != 0) input.skill = true;
            if ((presses & 8) != 0) input.dash = true;
            if ((presses & 16) != 0) input.pickup = true;
            if ((presses & 32) != 0) input.up = true;
            if ((presses & 64) != 0) input.townPortal = true;
            if ((presses & 128) != 0) input.hpPotion = true;
            if ((presses & 256) != 0) input.mpPotion = true;
            if ((presses & 512) != 0) input.respawn = true;
            remoteInputs[playerId] = input;
        }

        void OnCommand(ulong sender, FastBufferReader reader)
        {
            int playerId;
            if (CurrentRole != Role.Host || !clientPlayers.TryGetValue(sender, out playerId)) return;
            reader.ReadValueSafe(out byte command);
            reader.ReadValueSafe(out int argument);
            switch (command)
            {
                case 0: world.Equip(playerId, argument); break;
                case 1: world.Sell(playerId, argument); break;
                case 2: world.BuyPotion(playerId, argument != 0); break;
                case 3: world.BuyGear(playerId, (Slot)Mathf.Clamp(argument, 0, (int)Slot.Boots)); break;
                case 4: world.Travel(playerId, argument); break;
            }
            var p = world.GetPlayer(playerId);
            if (p != null) SendCharacter(sender, p);           // show the result right away
        }

        // Before each host tick: hand every remote player's input to the World.
        public void FillRemoteInputs(Dictionary<int, PlayerInput> inputs)
        {
            if (CurrentRole != Role.Host) return;
            scratchIds.Clear();
            scratchIds.AddRange(remoteInputs.Keys);
            foreach (var id in scratchIds)
            {
                var input = remoteInputs[id];
                inputs[id] = input;
                remoteInputs[id] = input.HeldOnly();
            }
        }

        // After the host's ticks for this frame (before the view clears the event list).
        public void HostAfterTicks(float dt)
        {
            if (CurrentRole != Role.Host || !handlersRegistered) return;
            foreach (var e in world.events) if (e.kind == SimEventKind.Slash) pendingEvents.Add(e);
            snapshotTimer += dt;
            characterTimer += dt;
            if (snapshotTimer >= SnapshotInterval)
            {
                snapshotTimer = Mathf.Min(snapshotTimer - SnapshotInterval, SnapshotInterval);
                foreach (var kv in clientPlayers) SendSnapshot(kv.Key, kv.Value);
                pendingEvents.Clear();
            }
            if (characterTimer >= CharacterInterval)
            {
                characterTimer = 0;
                foreach (var kv in clientPlayers)
                {
                    var p = world.GetPlayer(kv.Value);
                    if (p != null) SendCharacter(kv.Key, p);
                }
            }
        }

        void SendCharacter(ulong client, PlayerState p)
        {
            var w = new FastBufferWriter(2048, Allocator.Temp, 65536);
            try
            {
                w.WriteValueSafe(JsonUtility.ToJson(SaveCodec.ToData(p, false)));
                nm.CustomMessagingManager.SendNamedMessage(MsgCharacter, client, w, NetworkDelivery.ReliableFragmentedSequenced);
            }
            finally { w.Dispose(); }
        }

        // Everything one client needs to draw its map. The order here must match ApplySnapshot exactly.
        void SendSnapshot(ulong client, int playerId)
        {
            var viewer = world.GetPlayer(playerId);
            if (viewer == null) return;
            var map = world.maps[viewer.map];
            var w = new FastBufferWriter(2048, Allocator.Temp, 65536);
            try
            {
                w.WriteValueSafe(world.players.Count);
                foreach (var p in world.players)
                {
                    w.WriteValueSafe(p.id);
                    w.WriteValueSafe((byte)p.ch.hero);
                    w.WriteValueSafe(p.ch.name ?? "");
                    w.WriteValueSafe((byte)p.map);
                    w.WriteValueSafe(p.x); w.WriteValueSafe(p.y); w.WriteValueSafe(p.vx); w.WriteValueSafe(p.vy);
                    w.WriteValueSafe((sbyte)p.facing);
                    int flags = (p.grounded ? 1 : 0) | (p.climbing ? 2 : 0) | (p.crouching ? 4 : 0) | (p.dead ? 8 : 0)
                              | (p.lastWasSkill ? 16 : 0) | (p.portalOpen ? 32 : 0) | (p.returnReady ? 64 : 0);
                    w.WriteValueSafe((byte)flags);
                    w.WriteValueSafe((byte)(p.comboStep & 255));
                    w.WriteValueSafe(p.attackVisual); w.WriteValueSafe(p.hurtVisual); w.WriteValueSafe(p.immunity);
                    w.WriteValueSafe((short)p.hp); w.WriteValueSafe((short)p.mp); w.WriteValueSafe((short)p.ch.level);
                    w.WriteValueSafe(p.deathClock); w.WriteValueSafe(p.portalCast);
                    w.WriteValueSafe((byte)p.portalMap); w.WriteValueSafe(p.portalX); w.WriteValueSafe(p.portalY);
                    w.WriteValueSafe((byte)p.returnMap);
                }

                w.WriteValueSafe((byte)viewer.map);
                w.WriteValueSafe(map.monsters.Count);
                foreach (var m in map.monsters)
                {
                    w.WriteValueSafe(m.id); w.WriteValueSafe((byte)m.def); w.WriteValueSafe(m.x); w.WriteValueSafe(m.y);
                    w.WriteValueSafe((byte)Mathf.Clamp(Mathf.CeilToInt(255f * m.hp / Mathf.Max(1, m.Def.hp)), 0, 255));
                    w.WriteValueSafe((sbyte)m.facing);
                }
                w.WriteValueSafe(map.drops.Count);
                foreach (var d in map.drops)
                {
                    w.WriteValueSafe(d.id); w.WriteValueSafe((byte)d.kind); w.WriteValueSafe(d.x); w.WriteValueSafe(d.y);
                    if (d.kind == DropKind.Item) { w.WriteValueSafe((byte)d.item.rarity); w.WriteValueSafe(d.item.name ?? ""); }
                }
                w.WriteValueSafe(map.shots.Count);
                foreach (var s in map.shots) { w.WriteValueSafe(s.id); w.WriteValueSafe(s.x); w.WriteValueSafe(s.y); }
                int slashes = 0;
                foreach (var e in pendingEvents) if (e.map == viewer.map) slashes++;
                w.WriteValueSafe(slashes);
                foreach (var e in pendingEvents)
                {
                    if (e.map != viewer.map) continue;
                    w.WriteValueSafe(e.x); w.WriteValueSafe(e.y); w.WriteValueSafe((sbyte)e.facing); w.WriteValueSafe(e.heavy);
                }
                nm.CustomMessagingManager.SendNamedMessage(MsgSnapshot, client, w, NetworkDelivery.ReliableFragmentedSequenced);
            }
            finally { w.Dispose(); }
        }

        // ---------------------------------------------------------------- client side

        // Sends our input to the host 30 times a second (presses collected in between are all included).
        public void ClientSendInput(KeyboardInput keyboard, float dt)
        {
            if (CurrentRole != Role.Client || !welcomed || !handlersRegistered) { keyboard.Clear(); return; }
            inputTimer += dt;
            if (inputTimer < InputInterval) return;
            inputTimer = 0;
            var input = keyboard.Take();
            int presses = (input.jump ? 1 : 0) | (input.attack ? 2 : 0) | (input.skill ? 4 : 0) | (input.dash ? 8 : 0)
                        | (input.pickup ? 16 : 0) | (input.up ? 32 : 0) | (input.townPortal ? 64 : 0) | (input.hpPotion ? 128 : 0)
                        | (input.mpPotion ? 256 : 0) | (input.respawn ? 512 : 0);
            var w = new FastBufferWriter(16, Allocator.Temp);
            try
            {
                w.WriteValueSafe(input.moveX);
                w.WriteValueSafe(input.moveY);
                w.WriteValueSafe((ushort)presses);
                nm.CustomMessagingManager.SendNamedMessage(MsgInput, NetworkManager.ServerClientId, w, NetworkDelivery.ReliableSequenced);
            }
            finally { w.Dispose(); }
        }

        public void SendCommand(byte command, int argument)
        {
            if (CurrentRole != Role.Client || !handlersRegistered) return;
            var w = new FastBufferWriter(16, Allocator.Temp);
            try
            {
                w.WriteValueSafe(command);
                w.WriteValueSafe(argument);
                nm.CustomMessagingManager.SendNamedMessage(MsgCommand, NetworkManager.ServerClientId, w, NetworkDelivery.ReliableSequenced);
            }
            finally { w.Dispose(); }
        }

        void OnWelcome(ulong sender, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int id);
            LocalPlayerId = id;
            welcomed = true;
        }

        void OnCharacter(ulong sender, FastBufferReader reader)
        {
            reader.ReadValueSafe(out string json);
            CharacterData data = null;
            try { data = JsonUtility.FromJson<CharacterData>(json); } catch (Exception) { }
            if (data == null) return;
            var me = world.GetPlayer(LocalPlayerId);
            if (me != null) me.ch = SaveCodec.ToCharacter(data);
            else pendingCharacter = data;
            HasCharacter = true;
        }

        void OnSnapshot(ulong sender, FastBufferReader reader)
        {
            if (CurrentRole != Role.Client) return;
            ApplySnapshot(ref reader);
            snapshotAge = 0;
        }

        // Mirrors SendSnapshot. Positions move from where they were to the new value over the next 1/30 s.
        void ApplySnapshot(ref FastBufferReader r)
        {
            r.ReadValueSafe(out int playerCount);
            seen.Clear();
            for (int i = 0; i < playerCount; i++)
            {
                r.ReadValueSafe(out int id);
                r.ReadValueSafe(out byte hero);
                r.ReadValueSafe(out string name);
                r.ReadValueSafe(out byte map);
                r.ReadValueSafe(out float x); r.ReadValueSafe(out float y); r.ReadValueSafe(out float vx); r.ReadValueSafe(out float vy);
                r.ReadValueSafe(out sbyte facing);
                r.ReadValueSafe(out byte flags);
                r.ReadValueSafe(out byte combo);
                r.ReadValueSafe(out float attackVisual); r.ReadValueSafe(out float hurtVisual); r.ReadValueSafe(out float immunity);
                r.ReadValueSafe(out short hp); r.ReadValueSafe(out short mp); r.ReadValueSafe(out short level);
                r.ReadValueSafe(out float deathClock); r.ReadValueSafe(out float portalCast);
                r.ReadValueSafe(out byte portalMap); r.ReadValueSafe(out float portalX); r.ReadValueSafe(out float portalY);
                r.ReadValueSafe(out byte returnMap);

                seen.Add(id);
                var p = world.GetPlayer(id);
                bool fresh = p == null;
                if (fresh)
                {
                    p = new PlayerState { id = id, ch = new Character() };
                    if (id == LocalPlayerId && pendingCharacter != null) { p.ch = SaveCodec.ToCharacter(pendingCharacter); pendingCharacter = null; }
                    world.players.Add(p);
                }
                int mapId = Mathf.Clamp(map, 0, Maps.All.Length - 1);
                bool jump = fresh || p.map != mapId || Mathf.Abs(p.x - x) > 3f || Mathf.Abs(p.y - y) > 3f;
                p.px = jump ? x : p.x;
                p.py = jump ? y : p.y;
                p.map = mapId; p.x = x; p.y = y; p.vx = vx; p.vy = vy; p.facing = facing < 0 ? -1 : 1;
                p.grounded = (flags & 1) != 0; p.climbing = (flags & 2) != 0; p.crouching = (flags & 4) != 0;
                p.dead = (flags & 8) != 0; p.lastWasSkill = (flags & 16) != 0; p.portalOpen = (flags & 32) != 0;
                p.returnReady = (flags & 64) != 0;
                p.comboStep = combo;
                p.attackVisual = attackVisual; p.hurtVisual = hurtVisual; p.immunity = immunity;
                p.hp = hp; p.mp = mp;
                p.deathClock = deathClock; p.portalCast = portalCast;
                p.portalMap = Mathf.Clamp(portalMap, 0, Maps.All.Length - 1); p.portalX = portalX; p.portalY = portalY;
                p.returnMap = Mathf.Clamp(returnMap, 0, Maps.All.Length - 1);
                p.ch.hero = Mathf.Clamp(hero, 0, Heroes.All.Length - 1);
                p.ch.name = name;
                p.ch.level = Mathf.Max(1, (int)level);
            }
            world.players.RemoveAll(q => !seen.Contains(q.id));

            r.ReadValueSafe(out byte viewMap);
            int shown = Mathf.Clamp(viewMap, 0, Maps.All.Length - 1);
            for (int i = 0; i < world.maps.Length; i++)
                if (i != shown) { world.maps[i].monsters.Clear(); world.maps[i].drops.Clear(); world.maps[i].shots.Clear(); }
            var runtime = world.maps[shown];

            r.ReadValueSafe(out int monsterCount);
            monsterLookup.Clear();
            foreach (var m in runtime.monsters) monsterLookup[m.id] = m;
            runtime.monsters.Clear();
            for (int i = 0; i < monsterCount; i++)
            {
                r.ReadValueSafe(out int id); r.ReadValueSafe(out byte def); r.ReadValueSafe(out float x); r.ReadValueSafe(out float y);
                r.ReadValueSafe(out byte health); r.ReadValueSafe(out sbyte facing);
                MonsterState m;
                if (monsterLookup.TryGetValue(id, out m)) { m.px = m.x; m.py = m.y; }
                else
                {
                    int kind = Mathf.Clamp(def, 0, Monsters.All.Length - 1);
                    m = new MonsterState { id = id, def = kind, level = Monsters.All[kind].level, map = shown, px = x, py = y };
                }
                m.x = x; m.y = y;
                m.hp = health / 255f * m.Def.hp;
                m.facing = facing < 0 ? -1 : 1;
                runtime.monsters.Add(m);
            }

            r.ReadValueSafe(out int dropCount);
            dropLookup.Clear();
            foreach (var d in runtime.drops) dropLookup[d.id] = d;
            runtime.drops.Clear();
            for (int i = 0; i < dropCount; i++)
            {
                r.ReadValueSafe(out int id); r.ReadValueSafe(out byte kind); r.ReadValueSafe(out float x); r.ReadValueSafe(out float y);
                var dropKind = (DropKind)Mathf.Clamp(kind, 0, (int)DropKind.Item);
                Item item = null;
                if (dropKind == DropKind.Item)
                {
                    r.ReadValueSafe(out byte rarity);
                    r.ReadValueSafe(out string itemName);
                    item = new Item { rarity = (Rarity)Mathf.Clamp(rarity, 0, (int)Rarity.Epic), name = itemName };
                }
                DropState d;
                if (!dropLookup.TryGetValue(id, out d)) d = new DropState { id = id, map = shown };
                d.kind = dropKind; d.x = x; d.y = y; d.item = item;
                runtime.drops.Add(d);
            }

            r.ReadValueSafe(out int shotCount);
            shotLookup.Clear();
            foreach (var s in runtime.shots) shotLookup[s.id] = s;
            runtime.shots.Clear();
            for (int i = 0; i < shotCount; i++)
            {
                r.ReadValueSafe(out int id); r.ReadValueSafe(out float x); r.ReadValueSafe(out float y);
                ProjectileState s;
                if (shotLookup.TryGetValue(id, out s)) { s.px = s.x; s.py = s.y; }
                else s = new ProjectileState { id = id, map = shown, px = x, py = y };
                s.x = x; s.y = y;
                runtime.shots.Add(s);
            }

            r.ReadValueSafe(out int slashCount);
            for (int i = 0; i < slashCount; i++)
            {
                r.ReadValueSafe(out float x); r.ReadValueSafe(out float y); r.ReadValueSafe(out sbyte facing); r.ReadValueSafe(out bool heavy);
                world.events.Add(new SimEvent { kind = SimEventKind.Slash, map = shown, x = x, y = y, facing = facing < 0 ? -1 : 1, heavy = heavy });
            }
        }
    }

    // Inventory and shop actions from a client, sent to the host.
    public sealed class RemoteCommands : IGameCommands
    {
        readonly NetSession net;
        public RemoteCommands(NetSession net) { this.net = net; }
        public void Equip(int inventoryIndex) { net.SendCommand(0, inventoryIndex); }
        public void Sell(int inventoryIndex) { net.SendCommand(1, inventoryIndex); }
        public void BuyPotion(bool health) { net.SendCommand(2, health ? 1 : 0); }
        public void BuyGear(Slot slot) { net.SendCommand(3, (int)slot); }
        public void Travel(int town) { net.SendCommand(4, town); }
    }
}
