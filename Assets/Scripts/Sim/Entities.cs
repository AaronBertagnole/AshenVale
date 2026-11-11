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
        public long towns = 1L << Maps.FirstTown;       // towns found so far, one bit per map id (waystones travel between them)

        public long mapsSeen = 1L << Maps.FirstTown;     // maps visited, one bit per map id (the world map shows these)

        // Ability points: one per level from level 2. The basic attack starts at rank 1; the skill and the
        // movement ability start locked (rank 0). Every rank costs a point. Picks are the mutation choices
        // (0 = none yet). Unspent points are worked out from the level, so they can never drift from it.
        public readonly int[] abilityRanks = { 1, 0, 0 };
        public readonly int[] abilityPicks = { 0, 0, 0 };
        public int PointsEarned { get { return level - 1; } }
        public int PointsSpent { get { int n = 0; foreach (int r in abilityRanks) n += r; return n - 1; } }   // the basic attack's first rank is free
        public int UnspentPoints { get { return System.Math.Max(0, PointsEarned - PointsSpent); } }

        public bool KnowsTown(int map) { return map >= 0 && map < 64 && (towns & (1L << map)) != 0; }
        public void FindTown(int map) { if (Maps.IsTown(map) && map < 64) { towns |= 1L << map; lastTown = map; } }
        public bool HasSeen(int map) { return map >= 0 && map < 64 && (mapsSeen & (1L << map)) != 0; }
        public void SeeMap(int map) { if (map >= 0 && map < 64) mapsSeen |= 1L << map; }

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

        public float attackCooldown, skillRecharge, attackVisual, dashCooldown, dashTime, immunity, hurtVisual;
        // Transient combat state; do not add to CharacterData/save files.
        public float attackWindup, attackDuration;
        public bool attackPending, leapActive;
        public int attackFacing, attackDamage, attackSerial;
        public float leapDirection;
        public float fangLungeTime; // grounded Dragon Fang movement after contact
        public float rangerFlipTime; // visual only: one somersault after ladder/platform jump
        public int rangerFlipFacing = 1;
        public const float RangerFlipDuration = .36f;
        public float landVisual; // brief landing clip, canceled by an attack
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
        public float maxHp;             // its HP when it appeared (more with a party on the map)
        public readonly Dictionary<int, int> damageBy = new Dictionary<int, int>();   // player id -> damage dealt (loot owner, XP share)
        public readonly Dictionary<int, float> lastHitAt = new Dictionary<int, float>();  // player id -> world time of their last hit
        public float left, right;       // the stretch of floor it keeps to
        public float chaseLeft, chaseRight;   // wider stretch it may cross while going after a player (no portal calm zone)
        public int facing = -1;
        public int wanderDir;           // -1, 0 (standing) or 1 while nobody is being chased
        public float wanderTime;
        public float knockbackVelocity, knockbackTime;
        public int target;              // player id it is chasing (0 = none)
        public float aggroTime;         // seconds it stays on the player who last hit it
        public bool angry;              // a passive monster that has been hit: it stays on the hunt like an aggressive one
        public float life;              // chests: seconds before it moves on if nobody opens it
        // Bosses: what it is about to do (0 nothing, 1 root slam, 2 summon), how long until it lands, how long the
        // strike pose shows, and its cooldowns. Clients get windup / strike from the snapshot for the telegraph.
        public int attack;
        public float windup, strike, attackCooldown, summonCooldown;
        public MonsterDef Def { get { return Monsters.All[def]; } }
    }

    public enum DropKind { Gold, HpPotion, MpPotion, Item, Part }

    public sealed class DropState
    {
        public int id, map, value;  // value: gold amount, or the Parts id of a part
        public int owner;       // player who earned it (most damage); only they can take it for Tuning.LootReserve seconds
        public float born;      // world time it dropped
        public bool reservedForOther;   // clients: the host says someone else still owns it (drawn dim)
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

    public enum SimEventKind { Slash, MonsterHit, LevelUp, PlayerDied, BossSlam, DragonThrust, DragonFang, DragonFissure, DragonSweep, EmbermarksPickedUp, RimeBurst, RimeStorm, RimeTeleport, RangerRelease, HeroImpact, ItemDropped }

    // Something that happened during a tick, for the view (effects, sounds, numbers) and later for the network.
    public struct SimEvent
    {
        public SimEventKind kind;
        public int map, playerId, targetId, amount, facing;
        public float x, y;
        public bool heavy;
    }
}
