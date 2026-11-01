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

        public HeroDef Def { get { return Heroes.All[hero]; } }
        public int BonusAttack { get { int n = 0; foreach (var i in equipped) if (i != null) n += i.attack; return n; } }
        public int BonusDefense { get { int n = 0; foreach (var i in equipped) if (i != null) n += i.defense; return n; } }
        public int BonusHealth { get { int n = 0; foreach (var i in equipped) if (i != null) n += i.health; return n; } }
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

        // Where the town's return portal sends you.
        public int returnMap;
        public float returnX, returnY;
    }

    public sealed class MonsterState
    {
        public int id, def, map, level;
        public float x, y, px, py, hp, speed, cooldown;
        public float knockbackVelocity, knockbackTime;
        public int target;              // player id it is chasing (0 = none)
        public float aggroTime;         // seconds it stays on the player who last hit it
        public MonsterDef Def { get { return Monsters.All[def]; } }
    }

    public enum DropKind { Gold, HpPotion, MpPotion, Item }

    public sealed class DropState
    {
        public int id, map, value;
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
