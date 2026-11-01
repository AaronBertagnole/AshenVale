using UnityEngine;

namespace AshenVale
{
    // Every number that shapes how the game plays, in one place.
    // Content (heroes, monsters, maps, items) lives in the Data folder.
    public static class Tuning
    {
        // Simulation
        public const float TickRate = 60f;          // the world advances in fixed steps of 1/60 s (needed for networking later)
        public const int MaxTicksPerFrame = 5;      // after a long hitch, skip ahead instead of trying to catch up forever

        // Movement
        public const float Ground = -3.15f;
        public const float Gravity = -24f;
        public const float JumpSpeed = 10.8f;
        public const float ClimbSpeed = 4.2f;
        public const float CrouchSpeedFactor = .34f;
        public const float DashSpeed = 14f;
        public const float DashTime = .18f;
        public const float DashCooldown = 1.5f;
        public const float DashImmunity = .25f;
        public const float GateReach = 1.2f;        // how close to a portal you must be to press Up
        public const float NpcReach = 3.5f;         // merchant / waystone requests are accepted from this close (a bit generous for online lag)
        public const float PortalCastTime = 1.2f;
        public const float DropThroughTime = .25f;  // Down + Jump: how long you fall through the platform you stood on
        public const float KnockbackTime = .22f;    // touching a monster knocks you back a little (not while climbing)
        public const float KnockbackSpeed = 4.5f;
        public const float KnockbackHop = 4.5f;

        // Combat
        public const float HitImmunity = .7f;       // invulnerable time after being hit
        public const float HurtFlash = .23f;
        public const float BasicAttackVisual = .23f;
        public const float SkillAttackVisual = .46f;
        public const int DamagePerLevel = 4;        // added to hero damage per level
        public const float ArrowSpeed = 15f;
        public const float ArrowLife = 1.2f;
        public const float MonsterAggroTime = 6f;           // after you hit a monster it chases you this long (if it can reach you)
        public const float MonsterSight = 7f;               // aggressive monsters chase players this close on their own floor
        public const float MonsterTargetStickiness = 1.5f;  // it only switches to another reachable player this much closer
        public const float MonsterFloorTolerance = .5f;     // how close a player's floor must be to the monster's to count as reachable
        public const float MonsterWanderSpeed = .55f;       // share of its speed a monster ambles at when nobody is around
        public const float GroundRoam = 10f;                // ground monsters stay this close to their spawn point
        public const float PortalCalm = 6f;                 // ...and this far from portals on the ground
        public const float ArmorBase = 10f;          // defense reduces damage by def / (def + ArmorBase + ArmorPerLevel * level)
        public const float ArmorPerLevel = 2f;       // so gear always helps but never makes you immune

        // Progression
        public const int HpPerLevel = 10;
        public const int MpPerLevel = 5;
        public const int LevelUpHeal = 35;
        public const int LevelUpMana = 20;
        public const float DeathXpLoss = .10f;       // share of the current level's XP lost on death (never loses a level)
        public const float RespawnDelay = .6f;       // seconds of the death animation before R / N are accepted
        public static int XpNeeded(int level) { return 40 + 40 * level + 2 * level * level; }   // 82 at Lv 1, 640 at Lv 10, 1090 at Lv 15
        // Monsters well below you give less XP: full XP up to OutLevelGrace levels below you,
        // then 15% less for each level beyond that, but never under 10%.
        public const int OutLevelGrace = 5;
        public const float OutLevelPenalty = .15f;
        public const float OutLevelMinimum = .10f;
        public static int XpFor(int monsterXp, int monsterLevel, int playerLevel)
        {
            int below = playerLevel - monsterLevel - OutLevelGrace;
            float share = below <= 0 ? 1f : Mathf.Max(OutLevelMinimum, 1f - OutLevelPenalty * below);
            return Mathf.Max(1, Mathf.RoundToInt(monsterXp * share));
        }

        // Loot and items
        public const float PartDropChance = .35f;   // chance a monster also drops its part (some monsters override it)
        public const int ChestParts = 3;            // parts a chest spills
        public const int MaxParts = 999;            // most of one part you can carry
        public const int MaxUpgrade = 5;            // smithy upgrades per item
        public const float UpgradeStatGain = .12f;  // each upgrade adds this share of the item's base stats (at least 1)
        public const int InventoryCapacity = 20;
        public const float PickupRangeX = 2f;
        public const float PickupRangeY = 1.8f;
        public const float PotionDropChance = .23f;
        public const float EquipmentDropChance = .24f;  // rolled only when no potion dropped
        public const int StartingPotions = 3;
        public const int HpPotionHeal = 60;
        public const int MpPotionHeal = 45;
        public const int HpPotionPrice = 25;
        public const int MpPotionPrice = 35;
    }
}
