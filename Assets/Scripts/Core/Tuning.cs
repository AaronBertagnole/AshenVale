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
        public const float Gravity = -30f;           // heavier than before: shorter hang time (was -24)
        public const float JumpSpeed = 11.4f;        // reaches ~2.17 units, still clears a 2-row ledge (was 10.8 / 2.43 u)
        public const float AirSpeedFactor = .7f;     // sideways speed while airborne, as a share of ground speed
        public const float FlipEdge = 1.2f;          // the Ranger somersaults off a platform only this close to its end, facing out
        public const float ClimbSpeed = 3.4f;        // was 4.2; a touch slower reads more natural with the stepped climb poses
        public const float CrouchSpeedFactor = .34f;
        public const float DashSpeed = 14f;
        public const float DashTime = .18f;
        public const float DashCooldown = 1.5f;
        public const float DashImmunity = .25f;
        public const float GateReach = 1.2f;        // how close to a portal you must be to press Up
        public const float NpcReach = 3.5f;         // merchant / waystone requests are accepted from this close (a bit generous for online lag)
        public const float PortalCastTime = 1.2f;
        public const float DropThroughTime = .25f;  // Down + Jump: how long you fall through the platform you stood on
        // Monsters that survive a hit are shoved away from the attacker for PushTime seconds (speed in units/s, slowing down).
        public const float PushTime = .22f, PushMelee = 5.5f, PushFissure = 6.5f, PushArrow = 3.5f, PushOrb = 4f;
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
        public const float ArrowLife = .6f;         // arrows fly about 9 units (15 * .6); they used to cross a whole screen
        public const float MonsterAggroTime = 6f;           // after you hit a monster it chases you this long (if it can reach you)
        public const float MonsterSight = 7f;               // aggressive monsters chase players this close on their own floor
        public const float MonsterTargetStickiness = 1.5f;  // it only switches to another reachable player this much closer
        public const float MonsterFloorTolerance = .5f;     // how close a player's floor must be to the monster's to count as reachable
        public const float MonsterWanderSpeed = .55f;       // share of its speed a monster ambles at when nobody is around
        public const float GroundRoam = 10f;                // ground monsters stay this close to their spawn point
        public const float PortalCalm = 6f;                 // ...and this far from portals on the ground
        // Spawning (owner, 2026-11-05: more monsters, faster refills)
        public const int SpawnPackSize = 2;                 // monsters each spawn point keeps alive (bosses 1)
        public const float RespawnScale = .6f;              // multiplies each kind's respawn time (8 s -> 4.8 s)
        public const float PackStagger = 1.5f;              // seconds between pack members filling in
        public const float PackSpread = 2.5f;               // pack members land within this of the spawn point
        // Bosses
        public const float MonsterStrikeShow = .4f;    // how long an ordinary monster holds its strike pose after a hit
        public const float BossLootChance = .5f;       // each hero's chance of the boss's own marked gear per kill
        public const float BossEpicChance = .2f;       // that gear is Epic this often, Rare otherwise
        public const float BossWeaponDamage = 1.25f;   // a boss's weapon: this much more attack than an ordinary one of its level
        public const int BossWeaponHaste = 10, BossWeaponHasteEpic = 15;   // and % faster basic attacks (Rare, Epic)
        public const int MaxHaste = 40;                // however much gear adds
        public const int BossLootLevels = 3;           // and this many levels above the boss
        public const float BossRespawn = 180f;         // seconds before a beaten boss returns to its glade
        public const float BossSlamWindup = .9f;       // it rears up this long before the root slam lands
        public const float BossSlamRange = 3.4f;       // slam reach on either side, on its floor
        public const float BossSlamDamage = 1.6f;      // times its touch damage
        public const float BossSlamCooldown = 5f;      // (halved while enraged)
        // Bramble eruption: thorns burst from the floor under a hero, wherever they stand, after a warning mark.
        public const float BossEruptEvery = 6f;        // (faster while enraged; see also BossRootErupt)
        public const float BossEruptWindup = .7f;      // her call, before the mark appears
        public const float BossEruptWarn = 1f;         // the mark shows this long before the thorns
        public const float BossEruptRadius = 1.15f;
        public const float BossEruptDamage = 1.3f;     // times her touch damage
        // The rooted phase: at BossRootHp she roots herself and thorn walls close in from both ends of her floor
        // until she has taken BossBreakDamage (a share of her full HP) or BossRootMax seconds pass.
        public const float BossRootHp = .5f;
        public const float BossRootTime = .8f;         // the rooting and the breaking-free animations
        public const float BossRootMax = 22f;
        public const float BossBreakDamage = .12f;
        public const float BossRootErupt = 2.6f;       // while rooted: a mark under every hero this often
        public const float BossWallSpeed = .45f;       // units a second, each wall
        public const float BossWallGap = 9f;           // they stop this far apart
        public const float BossWallTick = .6f;         // heroes in the thorns are hurt this often...
        public const float BossWallDamage = .5f;       // ...for this share of her touch damage
        // Sprouts within reach of their mother heal her in pulses, standing still for a moment each time.
        public const float SproutHealRange = 3.5f;
        public const float SproutHealEvery = 1.6f;
        public const float SproutHealPause = .55f;
        public const float SproutHeal = .004f;         // share of her full HP per pulse
        public const float BossSummonWindup = 1.1f;
        public const float BossSummonEvery = 12f;
        public const int BossMaxMinions = 4;           // it stops calling sprouts while this many are alive
        public const int BossMinionsPerCall = 2;
        public const float BossEnrageHp = .3f;         // below this share of HP it moves and slams faster
        public const float BossEnrageSpeed = 1.5f;
        public const float BossStrikeVisual = .35f;    // how long the slam pose shows
        public const float ArmorBase = 6f;           // defense reduces damage by def / (def + ArmorBase + ArmorPerLevel * level)
        public const float ArmorPerLevel = 1f;       // at Lv 9: Common set ~30%, Epic ~55%, Epic +5 ~66%...
        public const float ArmorCap = .75f;          // ...but never past this, so nothing is harmless

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

        // Party play
        public const float PartyHpPerPlayer = .3f;      // monsters appear with this much more HP per extra living player on the map
        public const float PartyShareRange = 16f;       // a kill gives full XP to everyone alive this close on the map (about a screen)
        public const float HitMemory = 10f;             // ...and to anyone who hit it within this many seconds, wherever they are
        public const float LootReserve = 8f;            // a drop belongs to whoever did the most damage for this long, then anyone

        // Loot and items
        public const float PartDropChance = .35f;   // chance a monster also drops its part (some monsters override it)
        public const int ChestParts = 3;            // parts a boss spills
        public const int MaxParts = 999;            // most of one part you can carry
        public const int MaxUpgrade = 5;            // smithy upgrades per item
        public const float UpgradeStatGain = .12f;  // each upgrade adds this share of the item's base stats (at least 1)
        public const int InventoryCapacity = 20;
        public const float PickupRangeX = 1.1f;      // you have to be nearly on a drop to take it
        public const float PickupRangeY = 1.2f;
        public const float PotionDropChance = .23f;
        public const float EquipmentDropChance = .24f;  // rolled only when no potion dropped
        public const int StartingPotions = 3;
        // Potions heal a share of your maximum, so they keep up as you level (never less than the old flat amount).
        public const int HpPotionHeal = 60;
        public const int MpPotionHeal = 45;
        public const float HpPotionShare = .4f;
        public const float MpPotionShare = .4f;
        public static int PotionHeal(int flat, float share, int maximum) { return Mathf.Max(flat, Mathf.RoundToInt(maximum * share)); }
        public const int HpPotionPrice = 25;
        public const int MpPotionPrice = 35;
    }
}
