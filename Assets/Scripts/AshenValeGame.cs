using System.Collections.Generic;
using UnityEngine;
using AshenVale;

// Entry point: the scene's only component (keep this file's .meta so the scene still finds it).
//
// How the code is laid out:
//   Core/Tuning.cs      every gameplay number
//   Data/               content tables: heroes, monsters, items, maps
//   Sim/World.cs        the game state and all rules; driven only by PlayerInput and commands
//   Persistence/        hero save files (one JSON file per hero) and the conversion to and from the World
//   Net/                online co-op: NetSession (host / client messages, snapshots), OnlineSession (Relay join codes)
//   View/               draws the World (scenery, heroes, monsters, effects, camera)
//   UI/                 menus, HUD, inventory, shop, Esc menu (IMGUI)
//   Client/             keyboard and music
//
// Three ways to play:
//   Solo    this game runs the World and draws it.
//   Host    the same, plus friends' inputs are fed into the World and they are sent snapshots (NetSession).
//   Client  no World simulation here: inputs go to the host, and the World is filled from the host's snapshots.
public class AshenValeGame : MonoBehaviour
{
    enum Mode { Solo, Host, Client }

    const float AutosaveInterval = 30f;

    World world;
    WorldView view;
    GameHud hud;
    FrontMenu menu;
    MusicPlayer music;
    NetSession net;
    IGameCommands localCommands, remoteCommands;
    readonly KeyboardInput keyboard = new KeyboardInput();
    readonly Dictionary<int, PlayerInput> inputs = new Dictionary<int, PlayerInput>();
    Mode mode = Mode.Solo;
    int localId = -1;
    bool inMenu = true;
    float accumulator;
    float saveTimer;
    int lastMap = -1;
    string soloNotice = "";

    void Awake()
    {
        Application.targetFrameRate = 60;
        music = new MusicPlayer(gameObject);
        music.Play();
        world = new World(System.Environment.TickCount);
        view = new WorldView(world);
        net = new NetSession(world) { onFailed = OnNetFailed };
        localCommands = new LocalCommands(world, () => localId);
        remoteCommands = new RemoteCommands(net);
        hud = new GameHud(world) { onCharacterSelect = BackToHeroes, onQuit = QuitGame, commands = localCommands };
        menu = new FrontMenu(music, CreateHero, PlaySolo, HostGame, JoinGame, DeleteSaved);
        menu.SetSaved(SaveStore.LoadAll());
    }

    // ---------------------------------------------------------------- starting and leaving

    void CreateHero(int hero, string name)
    {
        var ch = new Character { id = SaveStore.NewId(), name = name, hero = hero };
        StartRun(ch, Maps.Emberfields, -1, -1);
        SaveNow();
    }

    void PlaySolo(CharacterData data)
    {
        StartRun(SaveCodec.ToCharacter(data), data.map, data.hp, data.mp);
    }

    void HostGame(CharacterData data, bool online)
    {
        StartRun(SaveCodec.ToCharacter(data), data.map, data.hp, data.mp);
        mode = Mode.Host;
        hud.online = true;
        if (online) net.HostOnline(localId);
        else if (!net.HostDirect(localId))
        {
            mode = Mode.Solo;
            hud.online = false;
            soloNotice = "COULD NOT HOST (IS PORT " + NetSession.DirectPort + " IN USE?)  ·  PLAYING SOLO";
        }
    }

    void JoinGame(CharacterData data, bool online, string codeOrAddress)
    {
        world.Clear();
        view.Reset();
        hud.Close();
        mode = Mode.Client;
        menu.busy = true;
        menu.status = online ? "Joining with code " + (codeOrAddress ?? "").Trim().ToUpperInvariant() + "..." : "Connecting to " + codeOrAddress + "...";
        if (online) net.JoinOnline(codeOrAddress, data);
        else if (!net.JoinDirect(codeOrAddress, data)) OnNetFailed("Could not start connecting.");
    }

    void DeleteSaved(CharacterData data)
    {
        SaveStore.Delete(data.id);
        menu.SetSaved(SaveStore.LoadAll());
    }

    void StartRun(Character ch, int map, int hp, int mp)
    {
        net.Leave();
        mode = Mode.Solo;
        PrepareRun();
        var me = world.AddPlayer(ch, map, hp, mp);
        localId = me.id;
        lastMap = me.map;
        hud.commands = localCommands;
        hud.online = false;
        inMenu = false;
    }

    void PrepareRun()
    {
        music.Stop();
        world.Clear();
        view.Reset();
        hud.Close();
        keyboard.Clear();
        inputs.Clear();
        accumulator = 0;
        saveTimer = AutosaveInterval;
        soloNotice = "";
        menu.busy = false;
        menu.status = "";
    }

    // Client: the host has welcomed us and our hero has arrived in a snapshot.
    void EnterClientGame()
    {
        music.Stop();
        keyboard.Clear();
        localId = net.LocalPlayerId;
        var me = world.GetPlayer(localId);
        lastMap = me != null ? me.map : -1;
        saveTimer = AutosaveInterval;
        hud.commands = remoteCommands;
        hud.online = true;
        menu.busy = false;
        menu.status = "";
        inMenu = false;
    }

    // A client can only save once the host has sent its full hero (otherwise it would save an empty bag).
    bool CanSave { get { return mode != Mode.Client || net.HasCharacter; } }

    void SaveNow()
    {
        saveTimer = AutosaveInterval;
        if (!CanSave) return;
        var me = world.GetPlayer(localId);
        if (me != null) SaveStore.Save(SaveCodec.ToData(me));
    }

    // N on the death screen, "Save & ..." in the Esc menu, or a lost connection.
    void BackToHeroes()
    {
        if (!inMenu) SaveNow();
        net.Leave();
        mode = Mode.Solo;
        world.Clear();
        view.Reset();
        hud.Close();
        hud.commands = localCommands;
        hud.online = false;
        localId = -1;
        menu.SetSaved(SaveStore.LoadAll());
        menu.screen = FrontMenu.Screen.Characters;
        menu.busy = false;
        inMenu = true;
        music.Play();
    }

    void OnNetFailed(string reason)
    {
        if (mode == Mode.Client)
        {
            BackToHeroes();
            menu.status = reason;
            return;
        }
        if (mode == Mode.Host)
        {
            // Hosting failed (for example online services not set up): carry on alone.
            net.Leave();
            mode = Mode.Solo;
            hud.online = false;
            soloNotice = reason.ToUpperInvariant() + "  ·  PLAYING SOLO";
        }
    }

    void QuitGame()
    {
        SaveNow();
        net.Leave();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Closing the window (or stopping Play in the editor) saves too.
    void OnApplicationQuit()
    {
        if (!inMenu) SaveNow();
        net.Leave();
    }

    // ---------------------------------------------------------------- frame loop

    void Update()
    {
        net.Update(Time.deltaTime);
        if (inMenu)
        {
            if (mode == Mode.Client && net.ClientReady) EnterClientGame();
            else
            {
                menu.HandleKeys();
                if (mode == Mode.Client && Input.GetKeyDown(KeyCode.Escape)) BackToHeroes();   // cancel joining
                view.Sync(localId, Time.deltaTime, 1f);
                return;
            }
        }

        var me = world.GetPlayer(localId);
        if (me != null && me.dead && me.deathClock >= Tuning.RespawnDelay && Input.GetKeyDown(KeyCode.N)) { BackToHeroes(); return; }
        hud.HandleKeys(me);
        keyboard.Sample(!hud.WindowOpen);
        hud.status = mode == Mode.Solo ? soloNotice : net.StatusLine;

        if (mode == Mode.Client)
        {
            net.ClientSendInput(keyboard, Time.deltaTime);
            Autosave();
            view.Sync(localId, Time.deltaTime, net.SnapshotAlpha);
            return;
        }

        const float step = 1f / Tuning.TickRate;
        accumulator += Time.deltaTime;
        int ticks = 0;
        while (accumulator >= step && ticks < Tuning.MaxTicksPerFrame)
        {
            inputs[localId] = keyboard.Take();
            net.FillRemoteInputs(inputs);
            world.Tick(step, inputs);
            accumulator -= step;
            ticks++;
        }
        if (ticks == Tuning.MaxTicksPerFrame) accumulator = 0;   // fell far behind (a hitch): skip ahead

        net.HostAfterTicks(Time.deltaTime);   // sends snapshots; must run before the view clears this frame's events
        Autosave();
        view.Sync(localId, Time.deltaTime, accumulator / step);
    }

    // Saves every 30 seconds, whenever the hero changes map, and on every level up.
    void Autosave()
    {
        var me = world.GetPlayer(localId);
        if (me == null) return;
        bool levelled = false;
        foreach (var e in world.events) if (e.kind == SimEventKind.LevelUp && e.playerId == localId) levelled = true;
        saveTimer -= Time.deltaTime;
        if (saveTimer <= 0 || me.map != lastMap || levelled)
        {
            lastMap = me.map;
            SaveNow();
        }
    }

    void OnGUI()
    {
        if (inMenu) menu.Draw();
        else hud.Draw(world.GetPlayer(localId));
    }
}
