using System.Collections.Generic;

namespace AshenVale
{
    // What a save file will hold: everything about a hero that outlives a session.
    public sealed class Character
    {
        public string id;           // save file id
        public string name;
        public float playSeconds;
        public int hero;
        public int level = 1, xp, gold, kills;
        public int hpPotions = Tuning.StartingPotions, mpPotions = Tuning.StartingPotions;
        public readonly List<Item> inventory = new List<Item>();
        public readonly Item[] equipped = new Item[4];
        public readonly int[] parts = new int[Parts.All.Length];   // monster parts carried, by Parts id
        public int lastTown = Maps.FirstTown;           // where town portals (T) and waking up take you
        public int towns = 1 << Maps.FirstTown;         // towns found so far, one bit per map id (waystones travel between them)

        public bool KnowsTown(int map) { return map >= 0 && map < 32 && (towns & (1 << map)) != 0; }
        public void FindTown(int map) { if (Maps.IsTown(map)) { towns |= 1 << map; lastTown = map; } }

        public HeroDef Def { get { return Heroes.All[hero]; } }
        public int BonusAttack { get { int n = 0; foreach (var i in equipped) if (i != null) n += i.Attack; return n; } }
        public int BonusDefense { get { int n = 0; foreach (var i in equipped) if (i != null) n += i.Defense; return n; } }
        public int BonusHealth { get { int n = 0; foreach (var i in equipped) if (i != null) n += i.Health; return n; } }
    }

    // A hero in the world right now: the saved Character plus position, health and timers.
    public sealed class PlayerState
    {
        public int id;
        public Character ch;
        public HeroDef Hero { get { return ch.Def; } }

        public int map;
        public float x, y, vx, vy;
        public float px, py;                // position at the start of the tick (the view draws between px,py and x,y)
        public int facing = 1;
        public bool grounded = true, climbing, crouching;
        public float currentFloor = Tuning.Ground;
        public float dropTimer, dropFloor;  // Down + Jump: falling through the platform at dropFloor
        public float knockTime, knockVx;    // pushed back after touching a monster

        public int hp, mp;
        public int MaxHp { get { return Hero.baseHp + Tuning.HpPerLevel * (ch.level - 1) + ch.BonusHealth; } }
        public int MaxMp { get { return Hero.baseMp + Tuning.MpPerLevel * (ch.level - 1); } }

        public float attackCooldown, attackVisual, dashCooldown, dashTime, immunity, hurtVisual;
        public int comboStep;
        public bool lastWasSkill;

        public bool dead;
        public float deathClock;

        // Town portal (T): cast where you stand, Up to step through to town.
        public float portalCast;
        public bool portalOpen;
        public int portalMap;
        public float portalX, portalY;

        // Where a town's return portal (&) sends you. It only works after arriving by town portal or waking up
        // (returnReady), and closes once used or once you walk out of town another way.
        public bool returnReady;
        public int returnMap;
        public float returnX, returnY;
    }

    public sealed class MonsterState
    {
        public int id, def, map, level;
        public int spawn = -1;          // index of its spawn point in the map (refilled after it dies)
        public float x, y, px, py, hp;  // y is its floor: monsters never leave the platform they live on
        public float left, right;       // the stretch of floor it keeps to
        public int facing = -1;
        public int wanderDir;           // -1, 0 (standing) or 1 while nobody is being chased
        public float wanderTime;
        public float knockbackVelocity, knockbackTime;
        public int target;              // player id it is chasing (0 = none)
        public float aggroTime;         // seconds it stays on the player who last hit it
        public MonsterDef Def { get { return Monsters.All[def]; } }
    }

    public enum DropKind { Gold, HpPotion, MpPotion, Item, Part }

    public sealed class DropState
    {
        public int id, map, value;  // value: gold amount, or the Parts id of a part
        public int owner;       // player whose kill made it (for instanced loot later; anyone can pick it up for now)
        public DropKind kind;
        public float x, y;
        public Item item;
    }

    public sealed class ProjectileState
    {
        public int id, map, owner, damage, pierce;
        public float x, y, px, py, direction, life;
        public HashSet<int> hitSet;   // monster ids already hit; shared by every arrow of one volley
    }

    public enum SimEventKind { Slash, MonsterHit, LevelUp, PlayerDied }

    // Something that happened during a tick, for the view (effects, sounds, numbers) and later for the network.
    public struct SimEvent
    {
        public SimEventKind kind;
        public int map, playerId, targetId, amount, facing;
        public float x, y;
        public bool heavy;
    }
}
