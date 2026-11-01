using System.Collections.Generic;
using UnityEngine;

namespace AshenVale
{
    // Monsters, drops and arrows of one map. Every map exists all the time; a map only moves
    // while a living player is on it (so in a party, players can split up across maps).
    public sealed class MapRuntime
    {
        public readonly MapDef def;
        public readonly List<MonsterState> monsters = new List<MonsterState>();
        public readonly List<DropState> drops = new List<DropState>();
        public readonly List<ProjectileState> shots = new List<ProjectileState>();
        public float spawnTimer;
        public MapRuntime(MapDef def) { this.def = def; }
    }

    // The whole game state and every rule that changes it. Nothing here draws, plays sound or reads the keyboard:
    // players act only through PlayerInput (each tick) and the command methods (equip, buy, sell).
    // In online play this runs on the host, which owns the truth; clients send input and draw what the host reports.
    public sealed class World
    {
        public readonly MapRuntime[] maps;
        public readonly List<PlayerState> players = new List<PlayerState>();
        public readonly List<SimEvent> events = new List<SimEvent>();   // the view reads these and clears them
        public float time;

        readonly System.Random rng;
        int nextId = 1;
        static readonly PlayerInput NoInput = new PlayerInput();

        public World(int seed)
        {
            rng = new System.Random(seed);
            maps = new MapRuntime[Maps.All.Length];
            for (int i = 0; i < maps.Length; i++) maps[i] = new MapRuntime(Maps.All[i]);
        }

        int NextId() { return nextId++; }

        // ---------------------------------------------------------------- players

        public PlayerState AddPlayer(int hero)
        {
            var p = new PlayerState { id = NextId(), ch = new Character { hero = hero } };
            Teleport(p, Maps.Emberfields, Maps.All[Maps.Emberfields].startX, Tuning.Ground);
            p.hp = p.MaxHp;
            p.mp = p.MaxMp;
            p.returnMap = Maps.Emberfields;
            p.returnX = p.x;
            p.returnY = p.y;
            players.Add(p);
            return p;
        }

        public void RemovePlayer(int id)
        {
            var p = GetPlayer(id);
            if (p == null) return;
            players.Remove(p);
            foreach (var map in maps) map.shots.RemoveAll(s => s.owner == id);
        }

        public PlayerState GetPlayer(int id)
        {
            foreach (var p in players) if (p.id == id) return p;
            return null;
        }

        // Empties the world: no players, monsters, drops or arrows.
        public void Clear()
        {
            players.Clear();
            events.Clear();
            foreach (var map in maps)
            {
                map.monsters.Clear();
                map.drops.Clear();
                map.shots.Clear();
                map.spawnTimer = 0;
            }
        }

        bool HasLivingPlayer(int mapId)
        {
            foreach (var p in players) if (!p.dead && p.map == mapId) return true;
            return false;
        }

        // ---------------------------------------------------------------- tick

        public void Tick(float dt, Dictionary<int, PlayerInput> inputs)
        {
            time += dt;
            foreach (var p in players) { p.px = p.x; p.py = p.y; }
            foreach (var map in maps)
            {
                foreach (var m in map.monsters) { m.px = m.x; m.py = m.y; }
                foreach (var s in map.shots) { s.px = s.x; s.py = s.y; }
            }

            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                PlayerInput input;
                if (inputs == null || !inputs.TryGetValue(p.id, out input)) input = NoInput;
                TickPlayer(p, input, dt);
            }

            foreach (var map in maps)
            {
                if (!HasLivingPlayer(map.def.id)) continue;   // nobody here: the map waits, as it always has
                UpdateMonsters(map, dt);
                UpdateShots(map, dt);
                UpdateSpawning(map, dt);
            }
        }

        void TickPlayer(PlayerState p, PlayerInput input, float dt)
        {
            if (p.dead)
            {
                p.deathClock += dt;
                if (input.respawn && p.deathClock >= Tuning.RespawnDelay) Respawn(p);
                return;
            }

            var map = maps[p.map];
            bool town = map.def.kind == MapKind.Town;

            if (town)
            {
                if (input.up && Mathf.Abs(p.x - map.def.returnPortalX) < 1.35f) { ReturnFromTown(p); return; }
            }
            else
            {
                if (input.up && p.grounded)
                    foreach (var gate in map.def.gates)
                        if (Mathf.Abs(p.x - gate.x) < Tuning.GateReach) { UseGate(p, gate); return; }
                if (input.townPortal && !p.portalOpen && p.portalCast <= 0) p.portalCast = Tuning.PortalCastTime;
                if (p.portalCast > 0)
                {
                    p.portalCast -= dt;
                    if (p.portalCast <= 0) { p.portalOpen = true; p.portalMap = p.map; p.portalX = p.x; p.portalY = p.y; }
                }
                if (input.up && p.portalOpen && p.portalMap == p.map && Mathf.Abs(p.x - p.portalX) < 1.5f && Mathf.Abs(p.y - p.portalY) < 1.8f)
                { EnterTown(p); return; }
            }

            p.hurtVisual = Mathf.Max(0, p.hurtVisual - dt);
            p.attackCooldown = Mathf.Max(0, p.attackCooldown - dt);
            p.attackVisual = Mathf.Max(0, p.attackVisual - dt);
            p.dashCooldown = Mathf.Max(0, p.dashCooldown - dt);
            p.immunity = Mathf.Max(0, p.immunity - dt);

            Move(p, map.def, input, dt, !town);

            if (input.hpPotion && p.ch.hpPotions > 0 && p.hp < p.MaxHp) { p.ch.hpPotions--; p.hp = Mathf.Min(p.MaxHp, p.hp + Tuning.HpPotionHeal); }
            if (input.mpPotion && p.ch.mpPotions > 0 && p.mp < p.MaxMp) { p.ch.mpPotions--; p.mp = Mathf.Min(p.MaxMp, p.mp + Tuning.MpPotionHeal); }

            if (!town)
            {
                if (!p.crouching && input.attack) Attack(p, map, false);
                if (!p.crouching && input.skill) Attack(p, map, true);
                if (input.pickup) Pickup(p, map);
            }
        }

        // ---------------------------------------------------------------- movement

        void Move(PlayerState p, MapDef def, PlayerInput input, float dt, bool allowDash)
        {
            float axis = Mathf.Clamp(input.moveX, -1f, 1f);
            float vertical = Mathf.Clamp(input.moveY, -1f, 1f);
            if (axis != 0) p.facing = axis > 0 ? 1 : -1;
            bool jump = input.jump;

            Ladder nearby = new Ladder();
            bool atLadder = false;
            foreach (var ladder in def.ladders)
                if (Mathf.Abs(p.x - ladder.x) < .68f && p.y >= ladder.bottom - .35f && p.y <= ladder.top + .45f) { nearby = ladder; atLadder = true; break; }
            if (!p.climbing && atLadder && Mathf.Abs(vertical) > .1f) { p.climbing = true; p.grounded = false; p.vy = 0; p.x = nearby.x; }
            if (p.climbing && (!atLadder || jump)) { p.climbing = false; if (jump) { p.vy = Tuning.JumpSpeed; p.grounded = false; } }
            p.crouching = p.grounded && !p.climbing && vertical < -.1f;
            if (!p.climbing && p.grounded && jump) { p.vy = Tuning.JumpSpeed; p.grounded = false; }
            if (allowDash && input.dash && p.dashCooldown <= 0) { p.dashTime = Tuning.DashTime; p.dashCooldown = Tuning.DashCooldown; p.immunity = Tuning.DashImmunity; }
            if (p.dashTime > 0) { p.dashTime -= dt; p.vx = p.facing * Tuning.DashSpeed; }
            else p.vx = axis * p.Hero.moveSpeed * (p.crouching ? Tuning.CrouchSpeedFactor : 1f);
            p.x = Mathf.Clamp(p.x + (p.climbing ? 0 : p.vx * dt), def.minX, def.maxX);

            if (p.climbing)
            {
                p.vx = 0; p.vy = 0;
                p.y = Mathf.Clamp(p.y + vertical * Tuning.ClimbSpeed * dt, nearby.bottom, nearby.top);
                if (p.y >= nearby.top - .04f && vertical > 0) { p.climbing = false; p.grounded = true; p.currentFloor = nearby.top; p.y = nearby.top; }
                return;
            }

            float previousY = p.y;
            if (!p.grounded) { p.vy += Tuning.Gravity * dt; p.y += p.vy * dt; }
            if (p.vy <= 0)
            {
                float landing = Tuning.Ground;
                foreach (var plat in def.platforms)
                    if (p.x >= plat.left + .16f && p.x <= plat.right - .16f && previousY >= plat.top - .03f && p.y <= plat.top && plat.top > landing)
                        landing = plat.top;
                if (p.y <= landing && previousY >= landing - .03f) { p.y = landing; p.vy = 0; p.grounded = true; p.currentFloor = landing; }
            }
            if (p.grounded)
            {
                bool supported = p.currentFloor == Tuning.Ground;
                foreach (var plat in def.platforms)
                    if (Mathf.Abs(p.currentFloor - plat.top) < .05f && p.x >= plat.left + .16f && p.x <= plat.right - .16f) supported = true;
                if (!supported) { p.grounded = false; p.vy = 0; }
            }
        }

        // Puts a player somewhere instantly (map change, town, respawn), standing still.
        void Teleport(PlayerState p, int mapId, float x, float y)
        {
            p.map = mapId;
            p.x = p.px = x;
            p.y = p.py = y;
            p.vx = p.vy = 0;
            p.grounded = true;
            p.climbing = p.crouching = false;
            p.currentFloor = y;
            p.dashTime = 0;
        }

        void UseGate(PlayerState p, Gate gate)
        {
            p.portalOpen = false;   // a town portal only lasts while you stay on its map
            p.portalCast = 0;
            Teleport(p, gate.targetMap, gate.targetX, Tuning.Ground);
        }

        void EnterTown(PlayerState p)
        {
            p.returnMap = p.map; p.returnX = p.x; p.returnY = p.y;
            Teleport(p, Maps.Town, Maps.All[Maps.Town].startX, Tuning.Ground);
        }

        void ReturnFromTown(PlayerState p)
        {
            Teleport(p, p.returnMap, p.returnX, p.returnY);
            var map = maps[p.map];
            map.spawnTimer = Mathf.Max(1, map.spawnTimer);
        }

        // R on the death screen: wake in town with full HP / MP, minus part of this level's XP.
        void Respawn(PlayerState p)
        {
            p.ch.xp = Mathf.Max(0, p.ch.xp - Mathf.RoundToInt(Tuning.XpNeeded(p.ch.level) * Tuning.DeathXpLoss));
            p.dead = false; p.deathClock = 0;
            p.hp = p.MaxHp; p.mp = p.MaxMp;
            p.hurtVisual = p.immunity = p.attackVisual = p.portalCast = 0;
            p.portalOpen = false;
            int fell = maps[p.map].def.kind == MapKind.Field ? p.map : Maps.Emberfields;
            EnterTown(p);
            // The return portal leads to the start of the map you fell on, not into the monsters that beat you.
            p.returnMap = fell; p.returnX = maps[fell].def.startX; p.returnY = Tuning.Ground;
        }

        // ---------------------------------------------------------------- combat

        void Attack(PlayerState p, MapRuntime map, bool skill)
        {
            if (p.attackCooldown > 0) return;
            var hero = p.Hero;
            if (skill && p.mp < hero.skillManaCost) return;
            if (skill) p.mp -= hero.skillManaCost;
            p.lastWasSkill = skill;
            if (!skill) p.comboStep++;
            p.attackCooldown = skill ? hero.skillCooldown : hero.attackCooldown;
            p.attackVisual = skill ? Tuning.SkillAttackVisual : Tuning.BasicAttackVisual;
            int damage = hero.baseDamage + p.ch.level * Tuning.DamagePerLevel + p.ch.BonusAttack + (skill ? hero.skillBonus : 0);

            if (hero.style == AttackStyle.Arrows)
            {
                int count = skill ? hero.arrowsPerSkill : 1;
                var volley = new HashSet<int>();
                for (int i = 0; i < count; i++)
                    map.shots.Add(new ProjectileState
                    {
                        id = NextId(), map = map.def.id, owner = p.id, damage = damage, pierce = skill ? hero.skillPierce : 1,
                        x = p.x + p.facing * .7f, y = p.y + 1.1f + (i - 1) * .17f, direction = p.facing, life = Tuning.ArrowLife, hitSet = volley,
                    });
                for (int i = map.shots.Count - count; i < map.shots.Count; i++) { map.shots[i].px = map.shots[i].x; map.shots[i].py = map.shots[i].y; }
                return;
            }

            events.Add(new SimEvent { kind = SimEventKind.Slash, map = map.def.id, playerId = p.id, heavy = skill, facing = p.facing,
                                      x = p.x + p.facing * (skill ? 1.5f : 1.1f), y = p.y + 1.1f });
            float range = skill ? hero.skillRange : hero.meleeRange;
            var targets = new List<MonsterState>();
            foreach (var m in map.monsters)
                if (Mathf.Abs(m.x - p.x) <= range && Mathf.Abs(m.y - p.y) < 1.7f && (m.x - p.x) * p.facing >= -.3f) targets.Add(m);
            targets.Sort((a, b) => ((a.x - p.x) * p.facing).CompareTo((b.x - p.x) * p.facing));
            for (int i = 0; i < targets.Count; i++)
            {
                var m = targets[i];
                int dealt = skill ? Mathf.Max(1, Mathf.RoundToInt(damage * Mathf.Pow(.8f, i))) : damage;
                HitMonster(map, m, dealt, p);
                if (skill) continue;                 // Dragon Strike carries on through the group
                if (map.monsters.Contains(m)) { m.knockbackVelocity = p.facing * 5.5f; m.knockbackTime = .22f; }
                break;                                // a basic thrust stops at the nearest monster
            }
        }

        void UpdateShots(MapRuntime map, float dt)
        {
            for (int i = map.shots.Count - 1; i >= 0; i--)
            {
                var s = map.shots[i];
                s.x += s.direction * Tuning.ArrowSpeed * dt;
                s.life -= dt;
                for (int j = 0; j < map.monsters.Count; j++)
                {
                    var m = map.monsters[j];
                    if (s.hitSet.Contains(m.id) || Mathf.Abs(m.x - s.x) >= .5f || Mathf.Abs((m.y + .7f) - s.y) >= 1f) continue;
                    s.hitSet.Add(m.id);
                    HitMonster(map, m, s.damage, GetPlayer(s.owner));
                    s.pierce--;
                    break;
                }
                if (s.life <= 0 || s.pierce <= 0) map.shots.RemoveAt(i);
            }
        }

        void HitMonster(MapRuntime map, MonsterState m, int damage, PlayerState attacker)
        {
            if (!map.monsters.Contains(m)) return;
            m.hp -= damage;
            events.Add(new SimEvent { kind = SimEventKind.MonsterHit, map = map.def.id, targetId = m.id, amount = damage,
                                      playerId = attacker != null ? attacker.id : 0, x = m.x, y = m.y });
            if (m.hp > 0) return;
            map.monsters.Remove(m);
            if (attacker == null) return;

            var def = m.Def;
            attacker.ch.kills++;
            GainXp(attacker, def.xpBase + (m.level - 1) * def.xpPerLevel);

            int potion = rng.NextDouble() < Tuning.PotionDropChance ? (rng.NextDouble() < .5 ? 1 : 2) : 0;
            bool equipment = potion == 0 && rng.NextDouble() < Tuning.EquipmentDropChance;
            var drop = new DropState
            {
                id = NextId(), map = map.def.id, owner = attacker.id, x = m.x, y = m.y + .25f,
                kind = potion == 1 ? DropKind.HpPotion : potion == 2 ? DropKind.MpPotion : equipment ? DropKind.Item : DropKind.Gold,
                item = equipment ? Loot.Roll(rng, attacker.ch.level, attacker.Hero) : null,
                value = def.goldMin + rng.Next(def.goldRange),
            };
            map.drops.Add(drop);
        }

        void GainXp(PlayerState p, int amount)
        {
            p.ch.xp += amount;
            while (p.ch.xp >= Tuning.XpNeeded(p.ch.level))
            {
                p.ch.xp -= Tuning.XpNeeded(p.ch.level);
                p.ch.level++;
                p.hp = Mathf.Min(p.MaxHp, p.hp + Tuning.LevelUpHeal);
                p.mp = Mathf.Min(p.MaxMp, p.mp + Tuning.LevelUpMana);
                events.Add(new SimEvent { kind = SimEventKind.LevelUp, map = p.map, playerId = p.id, amount = p.ch.level, x = p.x, y = p.y });
            }
        }

        // Monster HP grows with its level, so its hits do too. Defense is a percentage that shrinks as you level:
        // two Epic armor pieces give roughly 35-40% reduction, never immunity.
        int DamageTaken(int raw, int monsterLevel, PlayerState victim)
        {
            float scaled = raw * (1f + Tuning.FoeDamagePerLevel * (monsterLevel - 1));
            int def = victim.ch.BonusDefense;
            float reduction = def / (def + Tuning.ArmorBase + Tuning.ArmorPerLevel * victim.ch.level);
            return Mathf.Max(1, Mathf.RoundToInt(scaled * (1f - reduction)));
        }

        // ---------------------------------------------------------------- monsters

        PlayerState NearestLivingPlayer(int mapId, float x)
        {
            PlayerState best = null;
            float bestDist = float.MaxValue;
            foreach (var p in players)
            {
                if (p.dead || p.map != mapId) continue;
                float d = Mathf.Abs(p.x - x);
                if (d < bestDist) { bestDist = d; best = p; }
            }
            return best;
        }

        void UpdateMonsters(MapRuntime map, float dt)
        {
            foreach (var m in map.monsters)
            {
                var target = NearestLivingPlayer(map.def.id, m.x);
                if (target == null) continue;
                float dist = target.x - m.x;
                if (m.knockbackTime > 0f)
                {
                    float step = Mathf.Min(dt, m.knockbackTime);
                    m.x = Mathf.Clamp(m.x + m.knockbackVelocity * step, map.def.minX + .5f, map.def.maxX - .5f);
                    m.knockbackTime = Mathf.Max(0f, m.knockbackTime - dt);
                    m.knockbackVelocity = Mathf.MoveTowards(m.knockbackVelocity, 0f, 20f * dt);
                }
                else if (Mathf.Abs(dist) > .95f) m.x += Mathf.Sign(dist) * m.speed * dt;

                m.cooldown -= dt;
                if (Mathf.Abs(dist) < 1.1f && Mathf.Abs(m.y - target.y) < 1.4f && m.cooldown <= 0)
                {
                    m.cooldown = Tuning.MonsterAttackCooldown;
                    if (target.immunity <= 0)
                    {
                        target.hp -= DamageTaken(m.Def.damage, m.level, target);
                        target.immunity = Tuning.HitImmunity;
                        target.hurtVisual = Tuning.HurtFlash;
                        if (target.hp <= 0)
                        {
                            target.hp = 0; target.dead = true; target.deathClock = 0; target.portalCast = 0;
                            events.Add(new SimEvent { kind = SimEventKind.PlayerDied, map = map.def.id, playerId = target.id, x = target.x, y = target.y });
                        }
                    }
                }
            }
        }

        void UpdateSpawning(MapRuntime map, float dt)
        {
            if (map.def.kind != MapKind.Field || map.def.spawns.Length == 0) return;
            map.spawnTimer -= dt;
            if (map.spawnTimer > 0 || map.monsters.Count >= Tuning.MaxMonstersPerMap) return;
            map.spawnTimer = Tuning.SpawnInterval;

            // Spawn 7-11 units to one side of a random player on this map, at that player's level.
            var here = new List<PlayerState>();
            foreach (var p in players) if (!p.dead && p.map == map.def.id) here.Add(p);
            if (here.Count == 0) return;
            var anchor = here[rng.Next(here.Count)];
            float sx = Mathf.Clamp(anchor.x + (rng.Next(2) == 0 ? -1 : 1) * (7 + rng.Next(0, 5)), map.def.minX + 1.5f, map.def.maxX - 1.5f);

            int kind = PickSpawn(map.def.spawns);
            var def = Monsters.All[kind];
            int level = anchor.ch.level;
            map.monsters.Add(new MonsterState
            {
                id = NextId(), def = kind, map = map.def.id, level = level,
                x = sx, y = Tuning.Ground, px = sx, py = Tuning.Ground,
                hp = def.baseHp + def.hpPerLevel * level, speed = def.speed,
            });
        }

        int PickSpawn(SpawnEntry[] table)
        {
            float total = 0;
            foreach (var e in table) total += e.weight;
            float r = (float)rng.NextDouble() * total;
            foreach (var e in table)
            {
                if (r < e.weight) return e.monster;
                r -= e.weight;
            }
            return table[table.Length - 1].monster;
        }

        // ---------------------------------------------------------------- loot

        void Pickup(PlayerState p, MapRuntime map)
        {
            for (int i = map.drops.Count - 1; i >= 0; i--)
            {
                var d = map.drops[i];
                if (Mathf.Abs(d.x - p.x) > Tuning.PickupRangeX || Mathf.Abs(d.y - p.y) > Tuning.PickupRangeY) continue;
                switch (d.kind)
                {
                    case DropKind.HpPotion: p.ch.hpPotions++; break;
                    case DropKind.MpPotion: p.ch.mpPotions++; break;
                    case DropKind.Item:
                        if (p.ch.inventory.Count >= Tuning.InventoryCapacity) continue;   // bag full: leave it on the ground
                        p.ch.inventory.Add(d.item);
                        break;
                    default: p.ch.gold += d.value; break;
                }
                map.drops.RemoveAt(i);
            }
        }

        // ---------------------------------------------------------------- commands
        // Requests a player makes outside the tick (inventory and shop). The world checks each one,
        // so online the host can accept or refuse what a client asks for.

        public bool Equip(int playerId, int index)
        {
            var p = GetPlayer(playerId);
            if (p == null || p.dead || index < 0 || index >= p.ch.inventory.Count) return false;
            int before = p.ch.BonusHealth;
            var item = p.ch.inventory[index];
            int slot = (int)item.slot;
            p.ch.inventory.RemoveAt(index);
            if (p.ch.equipped[slot] != null) p.ch.inventory.Add(p.ch.equipped[slot]);
            p.ch.equipped[slot] = item;
            p.hp = Mathf.Clamp(p.hp + Mathf.Max(0, p.ch.BonusHealth - before), 1, p.MaxHp);
            return true;
        }

        public bool InTown(PlayerState p) { return p != null && !p.dead && maps[p.map].def.kind == MapKind.Town; }

        public bool NearMerchant(PlayerState p, float reach)
        {
            return InTown(p) && Mathf.Abs(p.x - maps[p.map].def.merchantX) < reach;
        }

        public bool Sell(int playerId, int index)
        {
            var p = GetPlayer(playerId);
            if (!InTown(p) || index < 0 || index >= p.ch.inventory.Count) return false;
            p.ch.gold += Loot.SellPrice(p.ch.inventory[index]);
            p.ch.inventory.RemoveAt(index);
            return true;
        }

        public bool BuyPotion(int playerId, bool health)
        {
            var p = GetPlayer(playerId);
            int price = health ? Tuning.HpPotionPrice : Tuning.MpPotionPrice;
            if (!InTown(p) || p.ch.gold < price) return false;
            p.ch.gold -= price;
            if (health) p.ch.hpPotions++; else p.ch.mpPotions++;
            return true;
        }

        public bool BuyGear(int playerId, Slot slot)
        {
            var p = GetPlayer(playerId);
            if (!InTown(p) || p.ch.inventory.Count >= Tuning.InventoryCapacity) return false;
            int cost = Loot.ShopGearCost(slot, p.ch.level);
            if (p.ch.gold < cost) return false;
            p.ch.gold -= cost;
            p.ch.inventory.Add(Loot.ShopGear(slot, p.ch.level, p.Hero));
            return true;
        }
    }
}
