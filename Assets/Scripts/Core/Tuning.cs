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
        public const float GateReach = 1.5f;        // how close to a map gate you must be to press Up
        public const float PortalCastTime = 1.2f;

        // Combat
        public const float HitImmunity = .7f;       // invulnerable time after being hit
        public const float HurtFlash = .23f;
        public const float BasicAttackVisual = .23f;
        public const float SkillAttackVisual = .46f;
        public const int DamagePerLevel = 4;        // added to hero damage per level
        public const float ArrowSpeed = 15f;
        public const float ArrowLife = 1.2f;
        public const float MonsterAttackCooldown = 1.15f;
        public const float FoeDamagePerLevel = .07f; // monster hits grow 7% per monster level (monster level = level of the player it spawned near)
        public const float ArmorBase = 10f;          // defense reduces damage by def / (def + ArmorBase + ArmorPerLevel * level)
        public const float ArmorPerLevel = 2f;       // so gear always helps but never makes you immune

        // Progression
        public const int HpPerLevel = 10;
        public const int MpPerLevel = 5;
        public const int LevelUpHeal = 35;
        public const int LevelUpMana = 20;
        public const float DeathXpLoss = .10f;       // share of the current level's XP lost on death (never loses a level)
        public const float RespawnDelay = .6f;       // seconds of the death animation before R / N are accepted
        public static int XpNeeded(int level) { return 35 + level * 25; }

        // Monsters
        public const int MaxMonstersPerMap = 9;
        public const float SpawnInterval = 2.2f;

        // Loot and items
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
