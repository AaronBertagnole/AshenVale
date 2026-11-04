using UnityEngine;

namespace AshenVale
{
    // One kind of monster. Like classic MapleStory, every monster kind has a fixed level and fixed stats:
    // a Hedge Shrew is always a level 1 Hedge Shrew, whoever meets it. Maps say which monsters live where
    // (see MapDefs), so a map's level range comes from its monsters.
    //
    // Behaviour:
    //   passive     wanders its platform until someone hits it; from then on it hunts like an aggressive one
    //   aggressive  also chases any player who comes close on its own platform
    //   harmless    never hurts anyone on touch (chests)
    //   speed 0     never moves
    // Touching a monster that isn't harmless hurts, MapleStory style.
    public sealed class MonsterDef
    {
        public string key;              // art folder: Resources/Art/Monsters/<key>/
        public string name;
        public int level;
        public Color color;             // placeholder look until the monster has art
        public float width, height;     // body size in world units (one map cell = 1 unit)
        public float hover;             // drawn this far above its floor (floating monsters)
        public int hp;
        public float speed;
        public int damage;              // touch damage before armor
        public int xp;
        public int goldMin, goldRange;  // gold = goldMin + random(0 .. goldRange - 1)
        public bool aggressive, harmless;
        public float respawn = 8f;      // seconds before its spawn point fills again (not chests, see IsChest)
        public float itemChance = -1f;  // chance of dropping gear (below 0: the usual Tuning.EquipmentDropChance)
        public int part = -1;           // the monster part it drops (Parts), -1 for none
        public float partChance = Tuning.PartDropChance;
        public int[] chestParts;        // chests: spill Tuning.ChestParts parts picked from this list

        // Bosses (World.UpdateBoss): slow giants that telegraph a slam, call minions and enrage when low. They give
        // their XP to everyone alive on the map, always drop their part, and spill chestParts like a chest.
        public bool boss;
        public int minion = -1;         // the monster kind a boss summons
        // Chests don't use respawn: they turn up at a random chest spot now and then (World.UpdateChests).
        public bool IsChest { get { return harmless && chestParts != null; } }
    }

    public static partial class Monsters
    {
        // The first four keep their old numbers.
        public const int HedgeShrew = 0, ThornbackHedgehog = 1, MossCrawler = 2, VioletWraith = 3,
                         AshSprout = 4, OldChest = 5, BrambleBoar = 6, MossyChest = 7, DenShade = 8,
                         MarshToad = 9, BogLurker = 10, AshWisp = 11, SunkenChest = 12,
                         LeafBeetle = 13, EmberFox = 14, Mossling = 15, CragGoat = 16, RockMite = 17,
                         Thornmother = 18, ThornSprout = 19,
                         // Highspire (Monsters/Highspire.cs)
                         FrostWolf = 20, SnowHare = 21, IcicleBat = 22, RimeWisp = 23, FrostTroll = 24, ForgeImp = 25,
                         SlagBeetle = 26, RuneSentinel = 27, CaveTroll = 28, RustedColossus = 29, FrostChest = 30,
                         // Saltmarrow coast (Monsters/Saltmarrow.cs)
                         SandCrab = 31, GullHarpy = 32, TideLurcher = 33, ReefEel = 34, CutlassRat = 35, PowderMonkey = 36,
                         BarrelMimic = 37, DeckGhoul = 38, BosunWraith = 39, HangedCorsair = 40, CaptainMarrow = 41, BarnacleChest = 42,
                         // The deep forest (Monsters/Eldermere.cs)
                         SylvanStag = 43, SporePuff = 44, HexToad = 45, GlimmerMoth = 46, VineStalker = 47, LivingTome = 48,
                         InkWraith = 49, OwlSentry = 50, RunedGolem = 51, WildMote = 52, ArchShade = 53, HollowArchmage = 54, ArcaneChest = 55,
                         // The ruins (Monsters/Lastlight.cs)
                         HollowPilgrim = 56, BoneCrawler = 57, Gravewisp = 58, HollowKnight = 59, FlayedHound = 60, IdolWarden = 61,
                         CultShade = 62, BoneColossus = 63, CarrionHorror = 64, HeraldOfTheDeep = 65, VoidSpawn = 66, TheSleeper = 67, SealedChest = 68;

        // In id order. Later regions live in Data/Monsters/<Region>.cs and are appended here.
        public static MonsterDef[] All { get { return all ?? (all = Assemble()); } }
        static MonsterDef[] all;

        static MonsterDef[] Assemble()
        {
            var list = new System.Collections.Generic.List<MonsterDef>(Core);
            list.AddRange(HighspireMonsters());
            list.AddRange(SaltmarrowMonsters());
            list.AddRange(EldermereMonsters());
            list.AddRange(LastlightMonsters());
            return list.ToArray();
        }

        static readonly MonsterDef[] Core =
        {
            new MonsterDef { key = "hedge_shrew", name = "Hedge Shrew", level = 1, color = new Color(.62f, .48f, .34f),
                width = .96f, height = .58f, hp = 40, speed = 1.8f, damage = 7, xp = 7, goldMin = 4, goldRange = 5,
                part = Parts.ShrewTuft },          // scurries through the leaf litter of Emberfields; art 96 x 58
            new MonsterDef { key = "thornback_hedgehog", name = "Thornback Hedgehog", level = 4, color = new Color(.45f, .32f, .22f),
                width = 1.2f, height = .97f, hp = 130, speed = 1.5f, damage = 14, xp = 22, goldMin = 14, goldRange = 9,
                aggressive = true, respawn = 15f,
                part = Parts.HedgehogQuill, partChance = .5f },
            new MonsterDef { key = "moss_crawler", name = "Moss Crawler", level = 6, color = new Color(.31f, .71f, .45f),
                width = 1f, height = .7f, hp = 140, speed = 1.4f, damage = 14, xp = 20, goldMin = 15, goldRange = 10,
                part = Parts.MossTuft },
            new MonsterDef { key = "violet_wraith", name = "Violet Wraith", level = 10, color = new Color(.58f, .33f, .92f),
                width = 1.2f, height = 1.5f, hover = .15f, hp = 270, speed = 2.2f, damage = 24, xp = 33, goldMin = 23, goldRange = 14,
                aggressive = true,
                part = Parts.WraithSilk },
            new MonsterDef { key = "ash_sprout", name = "Ash Sprout", level = 2, color = new Color(.62f, .7f, .38f),
                width = .8f, height = .92f, hp = 55, speed = 1f, damage = 6, xp = 10, goldMin = 6, goldRange = 6,
                part = Parts.SproutLeaf },
            new MonsterDef { key = "old_chest", name = "Old Chest", level = 4, color = new Color(.62f, .43f, .22f),
                width = 1f, height = .8f, hp = 100, speed = 0f, damage = 0, xp = 20, goldMin = 35, goldRange = 25,
                harmless = true, respawn = 45f, itemChance = .6f,
                chestParts = new[] { Parts.ShrewTuft, Parts.SproutLeaf, Parts.ShrewTuft, Parts.HedgehogQuill, Parts.EmberCore } },
            new MonsterDef { key = "bramble_boar", name = "Bramble Boar", level = 8, color = new Color(.55f, .38f, .27f),
                width = 1.4f, height = 1f, hp = 200, speed = 2.6f, damage = 20, xp = 26, goldMin = 19, goldRange = 12,
                aggressive = true,
                part = Parts.BoarTusk },
            new MonsterDef { key = "mossy_chest", name = "Mossy Chest", level = 8, color = new Color(.4f, .55f, .3f),
                width = 1f, height = .8f, hp = 220, speed = 0f, damage = 0, xp = 35, goldMin = 60, goldRange = 40,
                harmless = true, respawn = 45f, itemChance = .6f,
                chestParts = new[] { Parts.MossTuft, Parts.BoarTusk, Parts.MossTuft, Parts.BoarTusk, Parts.ShadeEssence, Parts.WraithSilk } },
            new MonsterDef { key = "den_shade", name = "Den Shade", level = 9, color = new Color(.3f, .3f, .45f),
                width = 1f, height = 1.4f, hover = .1f, hp = 250, speed = 2f, damage = 22, xp = 32, goldMin = 22, goldRange = 14,
                aggressive = true,
                part = Parts.ShadeEssence },
            new MonsterDef { key = "marsh_toad", name = "Marsh Toad", level = 11, color = new Color(.45f, .58f, .33f),
                width = 1f, height = .8f, hp = 290, speed = 1.2f, damage = 22, xp = 36, goldMin = 25, goldRange = 15,
                part = Parts.ToadSlime },
            new MonsterDef { key = "bog_lurker", name = "Bog Lurker", level = 13, color = new Color(.3f, .42f, .4f),
                width = 1.4f, height = 1.1f, hp = 360, speed = 1.6f, damage = 30, xp = 42, goldMin = 30, goldRange = 18,
                aggressive = true,
                part = Parts.LurkerScale },
            new MonsterDef { key = "ash_wisp", name = "Ash Wisp", level = 14, color = new Color(.85f, .82f, .7f),
                width = .8f, height = .9f, hover = .35f, hp = 330, speed = 2.8f, damage = 28, xp = 45, goldMin = 32, goldRange = 18,
                aggressive = true,
                part = Parts.WispEmber },
            new MonsterDef { key = "sunken_chest", name = "Sunken Chest", level = 15, color = new Color(.35f, .45f, .5f),
                width = 1.1f, height = .9f, hp = 500, speed = 0f, damage = 0, xp = 70, goldMin = 120, goldRange = 60,
                harmless = true, respawn = 45f, itemChance = .6f,
                chestParts = new[] { Parts.ToadSlime, Parts.LurkerScale, Parts.ToadSlime, Parts.LurkerScale, Parts.WispEmber } },
            new MonsterDef { key = "leaf_beetle", name = "Leaf Beetle", level = 3, color = new Color(.62f, .45f, .2f),
                width = .96f, height = .67f, hp = 85, speed = 1.1f, damage = 8, xp = 13, goldMin = 8, goldRange = 8,
                part = Parts.SproutLeaf },
            new MonsterDef { key = "ember_fox", name = "Ember Fox", level = 5, color = new Color(.95f, .5f, .18f),
                width = 1.1f, height = .85f, hp = 120, speed = 2.6f, damage = 13, xp = 18, goldMin = 12, goldRange = 9,
                aggressive = true, part = Parts.EmberCore, partChance = .25f },
            new MonsterDef { key = "mossling", name = "Mossling", level = 4, color = new Color(.42f, .66f, .36f),
                width = .8f, height = .89f, hp = 100, speed = 1.2f, damage = 10, xp = 15, goldMin = 10, goldRange = 8,
                part = Parts.MossTuft, partChance = .3f },
            new MonsterDef { key = "crag_goat", name = "Crag Goat", level = 9, color = new Color(.72f, .66f, .58f),
                width = 1.3f, height = 1.1f, hp = 230, speed = 2.9f, damage = 21, xp = 30, goldMin = 20, goldRange = 13,
                aggressive = true,
                part = Parts.BoarTusk },           // its horn works as tusk does
            new MonsterDef { key = "rock_mite", name = "Rock Mite", level = 10, color = new Color(.45f, .5f, .58f),
                width = .9f, height = .7f, hp = 300, speed = .9f, damage = 25, xp = 34, goldMin = 24, goldRange = 14,
                part = Parts.ShadeEssence, partChance = .25f },   // a second source of Shade Essence outside the den
            // Region 1's boss, in Thornmother's Glade off Mosswood. Her bark is thick: a party job, or a well-geared solo.
            new MonsterDef { key = "thornmother", name = "Thornmother", level = 9, color = new Color(.36f, .30f, .22f),
                width = 2.6f, height = 3.3f, hp = 2600, speed = 1.1f, damage = 26, xp = 420, goldMin = 160, goldRange = 120,
                aggressive = true, boss = true, minion = ThornSprout, respawn = Tuning.BossRespawn, itemChance = .9f,
                part = Parts.WraithSilk, partChance = 1f,
                chestParts = new[] { Parts.BoarTusk, Parts.MossTuft, Parts.ShadeEssence, Parts.WraithSilk, Parts.BoarTusk, Parts.MossTuft } },
            new MonsterDef { key = "thorn_sprout", name = "Thorn Sprout", level = 8, color = new Color(.45f, .62f, .25f),
                width = .8f, height = .8f, hp = 90, speed = 2.4f, damage = 16, xp = 12, goldMin = 8, goldRange = 8,
                aggressive = true,
                part = Parts.MossTuft, partChance = .2f },
        };
    }
}
