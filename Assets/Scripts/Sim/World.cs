using System.Collections.Generic;
using UnityEngine;

namespace AshenVale
{
    // Monsters, drops and arrows of one map. Every map exists all the time; a map only moves
    // while a living player is on it (so in a party, players can split up across maps).
    // Each spawn point holds a small pack of monsters (Tuning.SpawnPackSize; bosses one); when one dies the point
    // refills one after the monster's respawn time, scaled by Tuning.RespawnScale.
    public sealed class MapRuntime
    {
        public readonly MapDef def;
        public readonly List<MonsterState> monsters = new List<MonsterState>();
        public readonly List<DropState> drops = new List<DropState>();
        public readonly List<ProjectileState> shots = new List<ProjectileState>();
        public readonly int[] spawnCount;        // monsters alive from each spawn point
        public readonly float[] spawnTimers;
        public MapRuntime(MapDef def)
        {
            this.def = def;
            spawnCount = new int[def.spawns.Length];
            spawnTimers = new float[def.spawns.Length];
        }
    }

    // The whole game state and every rule that changes it. Nothing here draws, plays sound or reads the keyboard:
    // players act only through PlayerInput (each tick) and the command methods (equip, buy, sell).
    // In online play this runs on the host, which owns the truth; clients send input and draw what the host reports.
    public sealed partial class World
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

        // Brings a hero into the world at the start (@) of `map`. hp / mp below 0 mean full; hp of exactly 0
        // means the hero was saved while fallen, so they wake in their last town at full health.
        public PlayerState AddPlayer(Character ch, int map = Maps.Emberfields, int hp = -1, int mp = -1)
        {
            if (map < 0 || map >= maps.Length) map = Maps.Emberfields;
            if (hp == 0) map = Maps.IsTown(ch.lastTown) ? ch.lastTown : Maps.FirstTown;
            var p = new PlayerState { id = NextId(), ch = ch, map = -1 };   // -1: the first Teleport counts as arriving
            var def = maps[map].def;
            Teleport(p, map, def.startX, def.startY);
            if (def.kind == MapKind.Town) ch.FindTown(map);
            p.hp = hp <= 0 ? p.MaxHp : Mathf.Min(hp, p.MaxHp);
            p.mp = mp < 0 ? p.MaxMp : Mathf.Min(mp, p.MaxMp);
            p.returnMap = def.kind == MapKind.Field ? map : Maps.Emberfields;
            p.returnX = maps[p.returnMap].def.startX;
            p.returnY = maps[p.returnMap].def.startY;
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
            rimeCasts.Clear();
            players.Clear();
            events.Clear();
            foreach (var map in maps)
            {
                map.monsters.Clear();
                map.drops.Clear();
                map.shots.Clear();
                for (int i = 0; i < map.spawnCount.Length; i++) { map.spawnCount[i] = 0; map.spawnTimers[i] = 0; }
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

            TickRimecaller(dt);
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
            p.ch.playSeconds += dt;
            if (p.dead)
            {
                p.deathClock += dt;
                if (input.respawn && p.deathClock >= Tuning.RespawnDelay) Respawn(p);
                return;
            }

            var map = maps[p.map];
            var def = map.def;
            bool town = def.kind == MapKind.Town;

            if (input.up && p.grounded)
            {
                foreach (var portal in def.portals)
                    if (Mathf.Abs(p.x - portal.x) < Tuning.GateReach && Mathf.Abs(p.y - portal.y) < .6f) { UsePortal(p, portal); return; }
                if (town && p.returnReady && Mathf.Abs(p.x - def.returnX) < Tuning.GateReach && Mathf.Abs(p.y - def.returnY) < .6f)
                { ReturnFromTown(p); return; }
            }
            if (!town)
            {
                if (input.townPortal && !p.portalOpen && p.portalCast <= 0) p.portalCast = Tuning.PortalCastTime;
                if (p.portalCast > 0)
                {
                    p.portalCast -= dt;
                    if (p.portalCast <= 0) { p.portalOpen = true; p.portalMap = p.map; p.portalX = p.x; p.portalY = p.y; }
                }
                if (input.up && p.portalOpen && p.portalMap == p.map && Mathf.Abs(p.x - p.portalX) < 1.5f && Mathf.Abs(p.y - p.portalY) < 1.8f)
                { EnterTown(p); return; }
            }

            p.rangerFlipTime = Mathf.Max(0, p.rangerFlipTime - dt);
            p.landVisual = Mathf.Max(0, p.landVisual - dt);
            p.hurtVisual = Mathf.Max(0, p.hurtVisual - dt);
            p.attackCooldown = Mathf.Max(0, p.attackCooldown - dt);
            p.skillRecharge = Mathf.Max(0, p.skillRecharge - dt);
            p.attackVisual = Mathf.Max(0, p.attackVisual - dt);
            p.dashCooldown = Mathf.Max(0, p.dashCooldown - dt);
            p.immunity = Mathf.Max(0, p.immunity - dt);

            // A committed attack/leap is dropped when the knight grabs a ladder (a hit does not interrupt it);
            // no delayed hits after death/teleport.
            if (p.Hero.dragonKnight && p.climbing)
            { p.attackPending = false; p.attackVisual = 0; p.leapActive = false; p.landVisual = p.fangLungeTime = 0; }
            Move(p, def, input, dt, !town);
            if (p.grounded || p.climbing || p.knockTime > 0 || p.hurtVisual > 0)
                p.rangerFlipTime = 0;
            if (p.attackPending)
            {
                p.attackWindup -= dt;
                if (p.attackWindup <= 0)
                {
                    p.attackPending = false;
                    if (p.Hero.dragonKnight && p.lastWasSkill && p.grounded)
                        p.fangLungeTime = Mathf.Max(0f, p.Hero.fangLungeDuration);
                    if (p.Hero.style == AttackStyle.Arrows) ReleaseArrows(p, map, p.lastWasSkill, p.attackDamage, p.attackFacing);
                    else ResolveMelee(p, map, p.lastWasSkill, p.attackDamage, p.attackFacing);
                }
            }
            if (p.leapActive && p.grounded)
            {
                p.leapActive = false;
                LandDragonLeap(p, map);
            }

            if (input.hpPotion && p.ch.hpPotions > 0 && p.hp < p.MaxHp) { p.ch.hpPotions--; p.hp = Mathf.Min(p.MaxHp, p.hp + Tuning.PotionHeal(Tuning.HpPotionHeal, Tuning.HpPotionShare, p.MaxHp)); }
            if (input.mpPotion && p.ch.mpPotions > 0 && p.mp < p.MaxMp) { p.ch.mpPotions--; p.mp = Mathf.Min(p.MaxMp, p.mp + Tuning.PotionHeal(Tuning.MpPotionHeal, Tuning.MpPotionShare, p.MaxMp)); }

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
            bool knocked = p.knockTime > 0;
            if (knocked) { p.knockTime -= dt; axis = 0; }
            if (p.Hero.dragonKnight && (p.attackVisual > 0 || p.leapActive))
            { axis = 0; vertical = 0; }
            if (axis != 0) p.facing = axis > 0 ? 1 : -1;
            bool jump = input.jump && !knocked && !(p.Hero.dragonKnight && (p.attackVisual > 0 || p.leapActive));
            p.dropTimer = Mathf.Max(0, p.dropTimer - dt);

            Ladder nearby = new Ladder();
            bool atLadder = false;
            foreach (var ladder in def.ladders)
                if (Mathf.Abs(p.x - ladder.x) < .68f && p.y >= ladder.bottom - .35f && p.y <= ladder.top + .45f) { nearby = ladder; atLadder = true; break; }
            if (!p.climbing && atLadder && !knocked && !p.leapActive && !(p.Hero.dragonKnight && p.attackVisual > 0))
            {
                // Up grabs a ladder you are below the top of; Down grabs one you are standing on top of.
                bool climbUp = vertical > .1f && p.y < nearby.top - .05f;
                bool climbDown = vertical < -.1f && p.y > nearby.bottom + .05f;
                if (climbUp || climbDown) { p.climbing = true; p.grounded = false; p.vy = 0; p.x = nearby.x; p.dashTime = 0; }
            }
            if (p.climbing && (!atLadder || jump))
            {
                p.climbing = false;
                if (jump) { p.vy = Tuning.JumpSpeed; p.grounded = false; BeginRangerFlip(p); }
            }
            p.crouching = p.grounded && !p.climbing && !knocked && vertical < -.1f;
            if (!p.climbing && p.grounded && jump)
            {
                if (vertical < -.1f)
                {
                    // Down + Jump drops through the platform you stand on (the ground is solid).
                    if (p.currentFloor > Tuning.Ground + .05f)
                    { p.dropTimer = Tuning.DropThroughTime; p.dropFloor = p.currentFloor; p.grounded = false; p.crouching = false; p.vy = 0; }
                }
                else
                {
                    bool atEdge = NearPlatformEdge(def, p);
                    p.vy = Tuning.JumpSpeed; p.grounded = false;
                    if (atEdge) BeginRangerFlip(p);
                }
            }
            if (allowDash && input.dash && p.dashCooldown <= 0 && !knocked && !p.climbing && Abilities.Unlocked(p, Abilities.Movement))
            {
                if(RimecallerRules.Is(p.Hero)) { Froststep(p,def,vertical); axis=0; }
                else if (p.Hero.dragonKnight)
                {
                    if (p.grounded && p.attackVisual <= 0 && p.mp >= Abilities.MoveCost(p))
                    {
                        p.mp -= Abilities.MoveCost(p);
                        p.dashCooldown = Abilities.MoveCooldown(p);
                        p.leapActive = true; p.leapDirection = p.facing;
                        p.grounded = false; p.crouching = false;
                        p.dropTimer = 0; p.landVisual = 0; p.vy = p.Hero.leapJumpSpeed;
                    }
                }
                else
                { p.dashTime = Tuning.DashTime; p.dashCooldown = Abilities.MoveCooldown(p); p.immunity = Mathf.Max(p.immunity, Abilities.DashImmunity(p)); }
            }
            if (knocked) p.vx = p.knockVx;
            else if (p.fangLungeTime > 0 && p.grounded)
            {
                // Integrate only the remaining lunge time, including a partial final tick.
                float step = Mathf.Min(dt, p.fangLungeTime);
                p.vx = p.attackFacing * Mathf.Max(0f, p.Hero.fangLungeDistance) /
                    Mathf.Max(.001f, p.Hero.fangLungeDuration) * step / Mathf.Max(.000001f, dt);
                p.fangLungeTime = Mathf.Max(0f, p.fangLungeTime - step);
            }
            else if (p.leapActive) p.vx = p.leapDirection * Abilities.LeapSpeed(p);
            else if (p.dashTime > 0) { p.dashTime -= dt; p.vx = p.facing * Abilities.DashSpeed(p); }
            else p.vx = axis * p.Hero.moveSpeed * (p.crouching ? Tuning.CrouchSpeedFactor : p.grounded || p.climbing ? 1f : Tuning.AirSpeedFactor);
            if (!p.grounded) p.fangLungeTime = 0;
            p.x = Mathf.Clamp(p.x + (p.climbing ? 0 : p.vx * dt), def.minX, def.maxX);

            if (p.climbing)
            {
                p.vx = 0; p.vy = 0;
                p.y = Mathf.Clamp(p.y + vertical * Tuning.ClimbSpeed * dt, nearby.bottom, nearby.top);
                if (p.y >= nearby.top - .04f && vertical > 0) { p.climbing = false; p.grounded = true; p.currentFloor = nearby.top; p.y = nearby.top; }
                else if (p.y <= nearby.bottom + .04f && vertical < 0) { p.climbing = false; p.grounded = true; p.currentFloor = nearby.bottom; p.y = nearby.bottom; }
                return;
            }

            float previousY = p.y;
            if (!p.grounded) { p.vy += Tuning.Gravity * dt; p.y += p.vy * dt; }
            if (p.vy <= 0)
            {
                float landing = Tuning.Ground;
                foreach (var plat in def.platforms)
                {
                    if (p.dropTimer > 0 && Mathf.Abs(plat.top - p.dropFloor) < .05f) continue;   // the one we are dropping through
                    if (p.x >= plat.left + .16f && p.x <= plat.right - .16f && previousY >= plat.top - .03f && p.y <= plat.top && plat.top > landing)
                        landing = plat.top;
                }
                if (p.y <= landing && previousY >= landing - .03f) { p.y = landing; p.vy = 0; p.grounded = true; p.currentFloor = landing; }
            }
            if (p.grounded)
            {
                bool supported = Mathf.Abs(p.currentFloor - Tuning.Ground) < .01f;
                foreach (var plat in def.platforms)
                    if (Mathf.Abs(p.currentFloor - plat.top) < .05f && p.x >= plat.left + .16f && p.x <= plat.right - .16f) supported = true;
                if (!supported) { p.grounded = false; p.vy = 0; }
            }
        }

        // Standing on a raised platform within FlipEdge of one of its ends, facing out over the drop.
        static bool NearPlatformEdge(MapDef def, PlayerState p)
        {
            if (p.currentFloor <= Tuning.Ground + .05f) return false;
            foreach (var plat in def.platforms)
            {
                if (Mathf.Abs(plat.top - p.currentFloor) > .05f || p.x < plat.left - .2f || p.x > plat.right + .2f) continue;
                bool nearLeft = p.x - plat.left <= Tuning.FlipEdge, nearRight = plat.right - p.x <= Tuning.FlipEdge;
                return (nearLeft && p.facing < 0) || (nearRight && p.facing > 0);
            }
            return false;
        }

        static void BeginRangerFlip(PlayerState p)
        {
            if (p.Hero.folder != "ElvenRanger" || p.hurtVisual > 0 || p.attackVisual > 0) return;
            p.rangerFlipTime = PlayerState.RangerFlipDuration;
            p.rangerFlipFacing = p.facing;
        }

        // Puts a player somewhere instantly (map change, town, respawn), standing still.
        void Teleport(PlayerState p, int mapId, float x, float y)
        {
            p.attackPending = p.leapActive = false;
            p.attackWindup = p.attackVisual = p.attackDuration = p.landVisual = p.fangLungeTime = 0;
            p.rangerFlipTime = 0;
            bool arriving = p.map != mapId;
            p.map = mapId;
            p.ch.SeeMap(mapId);     // the world map shows every map you have been to
            p.x = p.px = x;
            p.y = p.py = y;
            p.vx = p.vy = 0;
            p.grounded = true;
            p.climbing = p.crouching = false;
            p.currentFloor = y;
            p.dashTime = p.knockTime = p.dropTimer = 0;
        }

        // Up at a map portal: come out of the matching portal on the other map.
        void UsePortal(PlayerState p, Portal portal)
        {
            p.portalOpen = false;   // a town portal only lasts while you stay on its map
            p.portalCast = 0;
            p.returnReady = false;  // walking out of town closes the way back
            var target = maps[portal.targetMap].def;
            Portal exit;
            if (target.FindPortal(portal.targetKey, out exit)) Teleport(p, target.id, exit.x, exit.y);
            else Teleport(p, target.id, target.startX, target.startY);
            if (target.kind == MapKind.Town) p.ch.FindTown(target.id);
        }

        // Arrive at the return portal (&) of your last town, which can then take you back out.
        void GoToTown(PlayerState p)
        {
            int town = Maps.IsTown(p.ch.lastTown) ? p.ch.lastTown : Maps.FirstTown;
            var def = maps[town].def;
            Teleport(p, town, def.returnX, def.returnY);
            p.returnReady = true;
        }

        // Up at your open town portal (T).
        void EnterTown(PlayerState p)
        {
            p.returnMap = p.map; p.returnX = p.x; p.returnY = p.y;
            GoToTown(p);
        }

        void ReturnFromTown(PlayerState p)
        {
            p.returnReady = false;
            Teleport(p, p.returnMap, p.returnX, p.returnY);
        }

        // R on the death screen: wake in your last town with full HP / MP, minus part of this level's XP.
        void Respawn(PlayerState p)
        {
            p.ch.xp = Mathf.Max(0, p.ch.xp - Mathf.RoundToInt(Tuning.XpNeeded(p.ch.level) * Tuning.DeathXpLoss));
            p.dead = false; p.deathClock = 0;
            p.hp = p.MaxHp; p.mp = p.MaxMp;
            p.hurtVisual = p.immunity = p.attackVisual = p.portalCast = 0;
            p.portalOpen = false;
            int fell = maps[p.map].def.kind == MapKind.Field ? p.map : Maps.Emberfields;
            GoToTown(p);
            // The return portal leads to the start of the map you fell on, not into the monsters that beat you.
            p.returnMap = fell; p.returnX = maps[fell].def.startX; p.returnY = maps[fell].def.startY;
        }

        // ---------------------------------------------------------------- combat

        void Attack(PlayerState p, MapRuntime map, bool skill)
        {
            if (skill && !Abilities.Unlocked(p, Abilities.Skill)) return;
            if(RimecallerRules.Is(p.Hero)) { BeginRimeAttack(p,map,skill); return; }
            if (p.attackCooldown > 0 || (p.Hero.dragonKnight && (p.leapActive || p.climbing || p.knockTime > 0))) return;
            var hero = p.Hero;
            if (skill && (p.mp < Abilities.SkillCost(p) || p.skillRecharge > 0)) return;
            if (skill) { p.mp -= Abilities.SkillCost(p); p.skillRecharge = Abilities.SkillRecharge(p); }
            p.landVisual = 0;
            p.rangerFlipTime = 0;
            p.lastWasSkill = skill;
            if (!skill) p.comboStep++;
            p.attackCooldown = skill ? Abilities.SkillSwing(p) : Abilities.BasicCooldown(p);
            p.attackVisual = hero.dragonKnight ? p.attackCooldown : (skill ? Tuning.SkillAttackVisual : hero.style == AttackStyle.Arrows ? p.attackCooldown : Tuning.BasicAttackVisual);
            p.attackDuration = p.attackVisual;
            p.attackSerial++;
            int damage = skill ? Abilities.SkillDamage(p) : Abilities.BasicDamage(p);

            if (hero.dragonKnight || hero.style == AttackStyle.Arrows)
            {
                // Melee lands, or the arrow leaves the bow, when the windup runs out (the draw of the clip).
                p.attackPending = true;
                p.attackWindup = skill ? hero.skillWindup : hero.basicWindup;
                p.attackFacing = p.facing; p.attackDamage = damage;
                return;
            }

            ResolveMelee(p, map, skill, damage, p.facing);
        }

        void ReleaseArrows(PlayerState p, MapRuntime map, bool skill, int damage, int facing)
        {
            var hero = p.Hero;
            int count = skill ? Abilities.SkillArrows(p) : 1;
            float middle = count <= 3 ? 1f : (count - 1) * .5f;     // one arrow and volleys of three sit where they always did
            events.Add(new SimEvent
            {
                kind = SimEventKind.RangerRelease, map = map.def.id, playerId = p.id,
                x = p.x + facing * .7f, y = p.y + 1.1f + ((count - 1) * .5f - middle) * .17f,
                facing = facing, heavy = skill
            });
            var volley = new HashSet<int>();
            for (int i = 0; i < count; i++)
                map.shots.Add(new ProjectileState
                {
                    id = NextId(), map = map.def.id, owner = p.id, damage = damage, pierce = skill ? Abilities.SkillPierce(p) : 1,
                    x = p.x + facing * .7f, y = p.y + 1.1f + (i - middle) * .17f, direction = facing, life = Tuning.ArrowLife, hitSet = volley,
                });
            for (int i = map.shots.Count - count; i < map.shots.Count; i++) { map.shots[i].px = map.shots[i].x; map.shots[i].py = map.shots[i].y; }
        }

        void ResolveMelee(PlayerState p, MapRuntime map, bool skill, int damage, int facing)
        {
            var hero = p.Hero;
            events.Add(new SimEvent { kind = hero.dragonKnight ? (skill ? SimEventKind.DragonFang : (p.comboStep % 2 == 0 ? SimEventKind.DragonSweep : SimEventKind.DragonThrust)) : SimEventKind.Slash, map = map.def.id, playerId = p.id, heavy = skill, facing = facing,
                                      x = p.x + facing * (skill ? 1.5f : 1.1f), y = p.y + 1.1f });
            // Reach is measured to the monster's near edge, not its centre (so a wide boss can be hit from where you
            // touch it), and the weapon's height band (knees to above the head) must overlap the monster's body, so a
            // tall boss can be hit from a ledge level with its trunk.
            float range = skill ? Abilities.SkillRange(p) : Abilities.BasicRange(p);
            var targets = new List<MonsterState>();
            foreach (var m in map.monsters)
            {
                var d = m.Def;
                float edge = Mathf.Max(0f, Mathf.Abs(m.x - p.x) - d.width * .5f);
                float bottom = m.y + d.hover, top = bottom + d.height;
                if (edge <= range && p.y + 1.7f > bottom - .3f && p.y + .3f < top + .3f && (m.x - p.x) * facing >= -d.width * .5f) targets.Add(m);
            }
            targets.Sort((a, b) => ((a.x - p.x) * facing).CompareTo((b.x - p.x) * facing));
            for (int i = 0; i < targets.Count; i++)
            {
                var m = targets[i];
                int dealt = skill ? Mathf.Max(1, Mathf.RoundToInt(damage * Mathf.Pow(.8f, i))) : damage;
                HitMonster(map, m, dealt, p, Tuning.PushMelee);   // survivors are knocked back (HitMonster)
                if (skill) continue;                 // Dragon Strike carries on through the group
                break;                                // a basic thrust stops at the nearest monster
            }
        }

        void LandDragonLeap(PlayerState p, MapRuntime map)
        {
            p.landVisual = .24f;
            events.Add(new SimEvent { kind = SimEventKind.DragonFissure, map = p.map,
                playerId = p.id, x = p.x, y = p.y, facing = p.facing });
            int damage = Abilities.LeapDamage(p);
            // Snapshot targets: deaths remove entries from the live monster list.
            var targets = new List<MonsterState>();
            foreach (var m in map.monsters)
                if (Mathf.Abs(m.y - p.currentFloor) < .35f &&
                    Mathf.Abs(m.x - p.x) - m.Def.width * .5f <= Abilities.FissureRange(p))
                    targets.Add(m);
            foreach (var m in targets) HitMonster(map, m, damage, p, Tuning.PushFissure);
        }

        void UpdateShots(MapRuntime map, float dt)
        {
            for (int i = map.shots.Count - 1; i >= 0; i--)
            {
                var s = map.shots[i];
                if(IsRimeShot(s)) { UpdateRimeShot(map,s,dt); if(s.life<=0 || s.pierce<=0) map.shots.RemoveAt(i); continue; }
                s.x += s.direction * Tuning.ArrowSpeed * dt;
                s.life -= dt;
                for (int j = 0; j < map.monsters.Count; j++)
                {
                    var m = map.monsters[j];
                    var d = m.Def;
                    float middle = m.y + d.hover + d.height * .5f;
                    if (s.hitSet.Contains(m.id) || Mathf.Abs(m.x - s.x) >= d.width * .5f + .1f || Mathf.Abs(middle - s.y) >= d.height * .5f + .5f) continue;
                    s.hitSet.Add(m.id);
                    HitMonster(map, m, s.damage, GetPlayer(s.owner), Tuning.PushArrow);
                    s.pierce--;
                    break;
                }
                if (s.life <= 0 || s.pierce <= 0) map.shots.RemoveAt(i);
            }
        }

        // push: how hard a monster that survives the hit is shoved away from the attacker (0 = none). Bosses are too
        // heavy and chests never move, so neither is pushed.
        void HitMonster(MapRuntime map, MonsterState m, int damage, PlayerState attacker, float push = 0f)
        {
            if (!map.monsters.Contains(m)) return;
            m.hp -= damage;
            if (attacker != null)
            {
                m.target = attacker.id; m.aggroTime = Tuning.MonsterAggroTime; m.angry = true;   // it turns on whoever hit it, and stays on the hunt
                int dealt;
                m.damageBy.TryGetValue(attacker.id, out dealt);
                m.damageBy[attacker.id] = dealt + Mathf.Min(damage, Mathf.CeilToInt(m.hp + damage));
                m.lastHitAt[attacker.id] = time;
            }
            events.Add(new SimEvent { kind = SimEventKind.MonsterHit, map = map.def.id, targetId = m.id, amount = damage,
                                      playerId = attacker != null ? attacker.id : 0, x = m.x, y = m.y });
            if (attacker != null && (attacker.Hero.dragonKnight || attacker.Hero.style == AttackStyle.Arrows))
            {
                events.Add(new SimEvent
                {
                    kind = SimEventKind.HeroImpact, map = map.def.id, playerId = attacker.id,
                    x = m.x, y = m.y + m.Def.hover + m.Def.height * .5f
                });
            }
            if (m.hp > 0)
            {
                var hitDef = m.Def;
                if (push > 0f && attacker != null && !hitDef.boss && hitDef.speed > 0f)
                {
                    float away = m.x - attacker.x;
                    m.knockbackVelocity = (Mathf.Abs(away) > .05f ? Mathf.Sign(away) : attacker.facing) * push;
                    m.knockbackTime = Tuning.PushTime;
                }
                return;
            }
            map.monsters.Remove(m);
            var def = m.Def;
            if (m.spawn >= 0 && m.spawn < map.spawnCount.Length)
            {
                map.spawnCount[m.spawn] = Mathf.Max(0, map.spawnCount[m.spawn] - 1);
                map.spawnTimers[m.spawn] = Mathf.Max(map.spawnTimers[m.spawn], def.respawn * (def.boss ? 1f : Tuning.RespawnScale));
            }
            if (attacker == null) return;

            attacker.ch.kills++;
            // Party play: full XP to everyone alive nearby on the map and to anyone who hit it recently (a boss: the
            // whole map). Loot belongs to whoever did the most damage, for a while.
            foreach (var p in players)
            {
                if (p.dead || p.map != map.def.id) continue;
                float hitAt;
                bool helped = p == attacker || def.boss || Mathf.Abs(p.x - m.x) <= Tuning.PartyShareRange
                              || (m.lastHitAt.TryGetValue(p.id, out hitAt) && time - hitAt <= Tuning.HitMemory);
                if (helped) GainXp(p, Tuning.XpFor(def.xp, def.level, p.ch.level));
            }
            if (def.boss) map.monsters.RemoveAll(other => other.def == def.minion && other.spawn < 0);   // its minions fall with it
            int best = 0;
            foreach (var kv in m.damageBy)
                if (kv.Value > best && GetPlayer(kv.Key) != null) { best = kv.Value; attacker = GetPlayer(kv.Key); }

            // Chests (and bosses) roll for gear first and always add gold; other monsters roll for a potion first.
            bool chest = def.itemChance >= 0f;
            float itemChance = chest ? def.itemChance : Tuning.EquipmentDropChance;
            bool equipment = chest && rng.NextDouble() < itemChance;
            int potion = !equipment && rng.NextDouble() < Tuning.PotionDropChance ? (rng.NextDouble() < .5 ? 1 : 2) : 0;
            if (!chest) equipment = potion == 0 && rng.NextDouble() < itemChance;
            var kind = potion == 1 ? DropKind.HpPotion : potion == 2 ? DropKind.MpPotion : equipment ? DropKind.Item : DropKind.Gold;
            AddDrop(map, attacker, kind, m.x, m.y, equipment ? Loot.Roll(rng, def.level, attacker.Hero) : null, def.goldMin + rng.Next(def.goldRange));
            if (chest && kind != DropKind.Gold) AddDrop(map, attacker, DropKind.Gold, m.x + .45f, m.y, null, def.goldMin + rng.Next(def.goldRange));

            // Monster parts for the smithy, on top of the usual drop.
            if (def.part >= 0 && rng.NextDouble() < def.partChance) AddDrop(map, attacker, DropKind.Part, m.x - .45f, m.y, null, def.part);
            if (def.chestParts != null && def.chestParts.Length > 0)
                for (int i = 0; i < Tuning.ChestParts; i++)
                    AddDrop(map, attacker, DropKind.Part, m.x - .4f - i * .35f, m.y, null, def.chestParts[rng.Next(def.chestParts.Length)]);
        }

        void AddDrop(MapRuntime map, PlayerState owner, DropKind kind, float x, float floor, Item item, int gold)
        {
            map.drops.Add(new DropState { id = NextId(), map = map.def.id, owner = owner.id, born = time, kind = kind, x = x, y = floor + .25f, item = item, value = gold });
        }

        // Puts an item from the bag on the ground just in front of the hero. Nobody owns it: anyone can pick it up.
        public void DropItem(int playerId, int inventoryIndex)
        {
            var p = GetPlayer(playerId);
            if (p == null || p.dead || inventoryIndex < 0 || inventoryIndex >= p.ch.inventory.Count) return;
            var map = maps[p.map];
            var item = p.ch.inventory[inventoryIndex];
            p.ch.inventory.RemoveAt(inventoryIndex);
            float x = Mathf.Clamp(p.x + p.facing * .7f, map.def.minX, map.def.maxX);
            map.drops.Add(new DropState { id = NextId(), map = map.def.id, owner = 0, born = time, kind = DropKind.Item,
                                          x = x, y = FloorAt(map.def, x, p.y) + .25f, item = item });
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

        // A monster's touch damage is fixed by its kind. Defense is a percentage that shrinks as you level:
        // a Common set gives about 30%, an Epic set 55%, capped at ArmorCap so nothing is harmless.
        int DamageTaken(int raw, PlayerState victim)
        {
            int def = victim.ch.BonusDefense;
            float reduction = Mathf.Min(Tuning.ArmorCap, def / (def + Tuning.ArmorBase + Tuning.ArmorPerLevel * victim.ch.level));
            return Mathf.Max(1, Mathf.RoundToInt(raw * (1f - reduction)));
        }

        // ---------------------------------------------------------------- monsters

        // A monster can reach a player standing on its own stretch of floor. Uses the floor the player last
        // stood on, so a player jumping still counts as reachable.
        static bool CanReach(MonsterState m, PlayerState p)
        {
            return Mathf.Abs(p.currentFloor - m.y) < Tuning.MonsterFloorTolerance && p.x >= m.chaseLeft - .6f && p.x <= m.chaseRight + .6f;
        }

        // Who a monster goes after (null = nobody, it wanders):
        //  1. the player who hit it in the last few seconds, if it can reach them;
        //  2. aggressive monsters, and passive ones that have been hit (angry): the nearest player it can reach
        //     within sight, sticking with its current target unless someone is clearly closer, so it doesn't flip
        //     back and forth.
        PlayerState PickTarget(int mapId, MonsterState m, float dt)
        {
            m.aggroTime = Mathf.Max(0f, m.aggroTime - dt);
            PlayerState current = null, nearest = null;
            float nearestDist = float.MaxValue;
            foreach (var p in players)
            {
                if (p.dead || p.map != mapId || !CanReach(m, p)) continue;
                float d = Mathf.Abs(p.x - m.x);
                if (p.id == m.target) current = p;
                if (d < nearestDist) { nearestDist = d; nearest = p; }
            }
            PlayerState pick = null;
            if (current != null && m.aggroTime > 0f) pick = current;
            else if (m.Def.aggressive || m.angry)
            {
                if (current != null && Mathf.Abs(current.x - m.x) <= Tuning.MonsterSight &&
                    Mathf.Abs(current.x - m.x) <= nearestDist + Tuning.MonsterTargetStickiness) pick = current;
                else if (nearest != null && nearestDist <= Tuning.MonsterSight) pick = nearest;
            }
            if (pick != null) m.target = pick.id;
            else if (m.aggroTime <= 0f) m.target = 0;   // while still angry it remembers who hit it
            return pick;
        }

        void UpdateMonsters(MapRuntime map, float dt)
        {
            foreach (var m in map.monsters)
            {
                var d = m.Def;
                var target = PickTarget(map.def.id, m, dt);
                if (d.speed <= 0f) { m.knockbackTime = 0f; continue; }   // chests and other things that never move
                float inset = Mathf.Min(d.width * .5f, .5f);
                float lo = m.left + inset, hi = m.right - inset;
                if (lo > hi) lo = hi = (m.left + m.right) * .5f;
                // Going after someone, it may leave its calm stretch (into the strip by a portal, say); it walks
                // back once it loses interest, rather than standing at the edge while the player hits it from beyond.
                float chaseLo = m.chaseLeft + inset, chaseHi = m.chaseRight - inset;
                if (chaseLo > chaseHi) chaseLo = chaseHi = (m.chaseLeft + m.chaseRight) * .5f;

                if (d.boss) { UpdateBoss(map, m, target, dt); m.x = Mathf.Clamp(m.x, lo, hi); continue; }
                if (m.knockbackTime > 0f)
                {
                    float step = Mathf.Min(dt, m.knockbackTime);
                    m.x += m.knockbackVelocity * step;
                    m.knockbackTime = Mathf.Max(0f, m.knockbackTime - dt);
                    m.knockbackVelocity = Mathf.MoveTowards(m.knockbackVelocity, 0f, 20f * dt);
                }
                else if (target != null)
                {
                    float dist = target.x - m.x;
                    if (Mathf.Abs(dist) > .2f) { m.facing = dist > 0 ? 1 : -1; m.x += m.facing * d.speed * dt; }
                }
                else
                {
                    m.wanderTime -= dt;
                    if (m.wanderTime <= 0f) { m.wanderDir = rng.Next(3) - 1; m.wanderTime = 1.2f + (float)rng.NextDouble() * 2.3f; }
                    if (m.x < lo) m.wanderDir = 1; else if (m.x > hi) m.wanderDir = -1;   // outside its stretch after a chase: head home
                    if (m.wanderDir != 0)
                    {
                        m.facing = m.wanderDir;
                        m.x += m.wanderDir * d.speed * Tuning.MonsterWanderSpeed * dt;
                        if ((m.x <= lo && m.wanderDir < 0) || (m.x >= hi && m.wanderDir > 0)) m.wanderDir = -m.wanderDir;
                    }
                }
                m.x = Mathf.Clamp(m.x, chaseLo, chaseHi);
            }
            TouchDamage(map);
        }

        // A boss: too heavy to knock back. It lumbers after its target; near enough it rears up (windup, which the
        // view shows) and slams the roots, hurting everyone on its floor within reach; now and then it calls minions
        // instead. Below BossEnrageHp it moves and slams faster. Touch still hurts like any monster.
        void UpdateBoss(MapRuntime map, MonsterState m, PlayerState target, float dt)
        {
            var d = m.Def;
            bool enraged = m.hp < m.maxHp * Tuning.BossEnrageHp;
            m.knockbackTime = 0f;
            m.strike = Mathf.Max(0f, m.strike - dt);
            m.attackCooldown = Mathf.Max(0f, m.attackCooldown - dt);
            m.summonCooldown = Mathf.Max(0f, m.summonCooldown - dt);
            if (m.windup > 0f)
            {
                m.windup -= dt;
                if (m.windup > 0f) return;                      // rearing up: stands still so the tell is readable
                m.windup = 0f;
                m.strike = Tuning.BossStrikeVisual;
                if (m.attack == 1) Slam(map, m, Mathf.RoundToInt(d.damage * Tuning.BossSlamDamage));
                else if (m.attack == 2 && d.minion >= 0) Summon(map, m, d.minion);
                m.attack = 0;
                return;
            }
            if (target == null)
            {
                // Nobody on its floor: it still stalks the nearest hero on the map (a ledge above it, say) and
                // keeps calling sprouts, so the glade is never a free shooting gallery.
                float best = float.MaxValue;
                foreach (var p in players)
                    if (!p.dead && p.map == map.def.id && Mathf.Abs(p.x - m.x) < best) { best = Mathf.Abs(p.x - m.x); target = p; }
                if (target == null) return;                     // truly nobody here: it waits in its glade
            }
            float dist = target.x - m.x;
            if (Mathf.Abs(dist) > .3f) m.facing = dist > 0 ? 1 : -1;
            if (m.attackCooldown <= 0f)
            {
                int minions = 0;
                foreach (var other in map.monsters) if (other.def == d.minion) minions++;
                if (m.summonCooldown <= 0f && minions < Tuning.BossMaxMinions && Mathf.Abs(dist) > 1.5f)
                {
                    m.attack = 2; m.windup = Tuning.BossSummonWindup;
                    m.summonCooldown = Tuning.BossSummonEvery; m.attackCooldown = 2f;
                    return;
                }
                if (Mathf.Abs(dist) < Tuning.BossSlamRange - .4f)
                {
                    m.attack = 1; m.windup = Tuning.BossSlamWindup;
                    m.attackCooldown = enraged ? Tuning.BossSlamCooldown * .5f : Tuning.BossSlamCooldown;
                    return;
                }
            }
            if (Mathf.Abs(dist) > 1.2f) m.x += m.facing * d.speed * (enraged ? Tuning.BossEnrageSpeed : 1f) * dt;
        }

        void Slam(MapRuntime map, MonsterState m, int damage)
        {
            events.Add(new SimEvent { kind = SimEventKind.BossSlam, map = map.def.id, targetId = m.id, x = m.x, y = m.y, facing = m.facing });
            foreach (var p in players)
            {
                if (p.dead || p.map != map.def.id || p.immunity > 0f) continue;
                if (Mathf.Abs(p.x - m.x) > Tuning.BossSlamRange || Mathf.Abs(p.currentFloor - m.y) > Tuning.MonsterFloorTolerance || !p.grounded && p.y > m.y + 1.5f) continue;
                Hurt(map, p, m, damage);
            }
        }

        // Minions appear at the boss's sides, keep to its floor, never refill, and vanish when it falls.
        void Summon(MapRuntime map, MonsterState boss, int kind)
        {
            var d = Monsters.All[kind];
            for (int i = 0; i < Tuning.BossMinionsPerCall; i++)
            {
                int side = i % 2 == 0 ? -1 : 1;
                float x = Mathf.Clamp(boss.x + side * (boss.Def.width * .5f + .8f), boss.left + .5f, boss.right - .5f);
                map.monsters.Add(new MonsterState
                {
                    id = NextId(), def = kind, map = map.def.id, level = d.level, spawn = -1,
                    x = x, y = boss.y, px = x, py = boss.y, hp = PartyHp(map, d), maxHp = PartyHp(map, d), left = boss.left, right = boss.right, chaseLeft = boss.chaseLeft, chaseRight = boss.chaseRight, facing = side,
                    target = boss.target, aggroTime = Tuning.MonsterAggroTime,
                });
            }
        }

        // MapleStory rules: bumping into a monster hurts (unless it's harmless) and knocks you back a little.
        void TouchDamage(MapRuntime map)
        {
            foreach (var p in players)
            {
                if (p.dead || p.map != map.def.id || p.immunity > 0f) continue;
                foreach (var m in map.monsters)
                {
                    var d = m.Def;
                    if (d.harmless) continue;
                    float bottom = m.y + d.hover, top = bottom + d.height;
                    if (Mathf.Abs(p.x - m.x) >= d.width * .5f + .3f || p.y >= top - .1f || p.y + 1.4f <= bottom) continue;
                    Hurt(map, p, m);
                    break;
                }
            }
        }

        void Hurt(MapRuntime map, PlayerState p, MonsterState m) { Hurt(map, p, m, m.Def.damage); }

        void Hurt(MapRuntime map, PlayerState p, MonsterState m, int damage)
        {
            p.rangerFlipTime = 0;
            p.hp -= DamageTaken(damage, p);
            p.immunity = Tuning.HitImmunity;
            p.hurtVisual = Tuning.HurtFlash;
            if (!p.climbing)
            {
                p.knockTime = Tuning.KnockbackTime;
                p.knockVx = (p.x >= m.x ? 1 : -1) * Tuning.KnockbackSpeed;
                p.dashTime = 0;
                if (p.grounded) { p.vy = Tuning.KnockbackHop; p.grounded = false; }
            }
            if (p.hp > 0) return;
            p.attackPending = p.leapActive = false; p.attackVisual = p.landVisual = 0;
            p.hp = 0; p.dead = true; p.deathClock = 0; p.portalCast = 0; p.knockTime = 0;
            events.Add(new SimEvent { kind = SimEventKind.PlayerDied, map = map.def.id, playerId = p.id, x = p.x, y = p.y });
        }

        // Spawn points below their pack size count down and refill one at a time. A map nobody is on doesn't count down.
        void UpdateSpawning(MapRuntime map, float dt)
        {
            var points = map.def.spawns;
            for (int i = 0; i < points.Length; i++)
            {
                var def = Monsters.All[points[i].monster];
                int capacity = def.boss ? 1 : Tuning.SpawnPackSize;
                if (map.spawnCount[i] >= capacity) continue;
                map.spawnTimers[i] -= dt;
                if (map.spawnTimers[i] > 0f) continue;
                map.spawnCount[i]++;
                SpawnAt(map, i);
                if (map.spawnCount[i] < capacity) map.spawnTimers[i] = Tuning.PackStagger;   // the rest of the pack trickles in
            }
        }

        MonsterState SpawnAt(MapRuntime map, int spawn)
        {
            var point = map.def.spawns[spawn];
            var def = Monsters.All[point.monster];
            // Pack members spread out along the point's stretch instead of stacking on one spot.
            float inset = Mathf.Min(def.width * .5f, .5f) + .2f;
            float x = def.boss ? point.x : Mathf.Clamp(point.x + ((float)rng.NextDouble() * 2f - 1f) * Tuning.PackSpread, point.left + inset, point.right - inset);
            if (point.right - point.left < inset * 2f) x = point.x;
            var m = new MonsterState
            {
                id = NextId(), def = point.monster, map = map.def.id, level = def.level, spawn = spawn,
                x = x, y = point.y, px = x, py = point.y, hp = PartyHp(map, def), maxHp = PartyHp(map, def),
                left = point.left, right = point.right, chaseLeft = point.chaseLeft, chaseRight = point.chaseRight,
                facing = rng.Next(2) == 0 ? -1 : 1,
                wanderTime = (float)rng.NextDouble() * 2f,
            };
            map.monsters.Add(m);
            return m;
        }

        // ---------------------------------------------------------------- loot

        void Pickup(PlayerState p, MapRuntime map)
        {
            for (int i = map.drops.Count - 1; i >= 0; i--)
            {
                var d = map.drops[i];
                if (Mathf.Abs(d.x - p.x) > Tuning.PickupRangeX || Mathf.Abs(d.y - p.y) > Tuning.PickupRangeY) continue;
                if (ReservedFor(d) != 0 && ReservedFor(d) != p.id) continue;    // still someone else's for a few seconds
                switch (d.kind)
                {
                    case DropKind.HpPotion: p.ch.hpPotions++; break;
                    case DropKind.MpPotion: p.ch.mpPotions++; break;
                    case DropKind.Item:
                        if (p.ch.inventory.Count >= Tuning.InventoryCapacity) continue;   // bag full: leave it on the ground
                        p.ch.inventory.Add(d.item);
                        break;
                    case DropKind.Part:
                        if (d.value < 0 || d.value >= p.ch.parts.Length || p.ch.parts[d.value] >= Tuning.MaxParts) continue;
                        p.ch.parts[d.value]++;
                        break;
                    case DropKind.Gold:
                        p.ch.gold += d.value;
                        events.Add(new SimEvent { kind = SimEventKind.EmbermarksPickedUp,
                            map = p.map, playerId = p.id, amount = d.value, x = p.x, y = p.y + 1.8f });
                        break;
                }
                map.drops.RemoveAt(i);
            }
        }

        // Who a drop still belongs to (0: anyone may take it).
        public int ReservedFor(DropState d)
        {
            return d.owner != 0 && time - d.born < Tuning.LootReserve && GetPlayer(d.owner) != null ? d.owner : 0;
        }

        // Monsters appear tougher with a party on the map, so hunting together stays a hunt.
        float PartyHp(MapRuntime map, MonsterDef def)
        {
            int living = 0;
            foreach (var p in players) if (!p.dead && p.map == map.def.id) living++;
            return def.hp * (1f + Tuning.PartyHpPerPlayer * Mathf.Max(0, living - 1));
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
            if (!InTown(p)) return false;
            foreach (var stand in maps[p.map].def.merchants)
                if (Mathf.Abs(p.x - stand.x) < reach && Mathf.Abs(p.y - stand.y) < 1.5f) return true;
            return false;
        }

        public bool NearWaystone(PlayerState p, float reach)
        {
            if (!InTown(p)) return false;
            var def = maps[p.map].def;
            return Mathf.Abs(p.x - def.waystoneX) < reach && Mathf.Abs(p.y - def.waystoneY) < 1.5f;
        }

        public bool NearSmithy(PlayerState p, float reach)
        {
            if (!InTown(p)) return false;
            var def = maps[p.map].def;
            return def.hasSmithy && Mathf.Abs(p.x - def.smithyX) < reach && Mathf.Abs(p.y - def.smithyY) < 1.5f;
        }

        // The item an upgrade target points at: 0-3 a worn slot, Smithing.InventoryTarget + i inventory item i.
        public static Item UpgradeTarget(Character ch, int target)
        {
            if (target >= 0 && target < ch.equipped.Length) return ch.equipped[target];
            int i = target - Smithing.InventoryTarget;
            return i >= 0 && i < ch.inventory.Count ? ch.inventory[i] : null;
        }

        // Smithy: spend gold and monster parts to raise an item by +1 (always succeeds, up to Tuning.MaxUpgrade).
        public bool Upgrade(int playerId, int target)
        {
            var p = GetPlayer(playerId);
            if (!NearSmithy(p, Tuning.NpcReach)) return false;
            var item = UpgradeTarget(p.ch, target);
            if (item == null || item.upgrade >= Tuning.MaxUpgrade) return false;
            var cost = Smithing.Cost(item);
            if (!Smithing.CanPay(p.ch, cost)) return false;
            int before = p.ch.BonusHealth;
            Smithing.Pay(p.ch, cost);
            item.upgrade++;
            p.hp = Mathf.Clamp(p.hp + Mathf.Max(0, p.ch.BonusHealth - before), 1, p.MaxHp);   // worn health gear heals the difference
            return true;
        }

        // Waystone: jump to another town you have already found, arriving at its waystone.
        public bool Travel(int playerId, int town)
        {
            var p = GetPlayer(playerId);
            if (!NearWaystone(p, Tuning.NpcReach) || !Maps.IsTown(town) || town == p.map || !p.ch.KnowsTown(town)) return false;
            var def = maps[town].def;
            Teleport(p, town, def.waystoneX, def.waystoneY);
            p.ch.FindTown(town);
            return true;
        }

        // Merchant: sell monster parts you don't need (count parts of one kind; 0 = all of them).
        public bool SellPart(int playerId, int part, int count)
        {
            var p = GetPlayer(playerId);
            if (!NearMerchant(p, Tuning.NpcReach) || part < 0 || part >= Parts.All.Length) return false;
            int have = p.ch.parts[part];
            if (count <= 0 || count > have) count = have;
            if (count <= 0) return false;
            p.ch.parts[part] -= count;
            p.ch.gold += Parts.All[part].price * count;
            return true;
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
