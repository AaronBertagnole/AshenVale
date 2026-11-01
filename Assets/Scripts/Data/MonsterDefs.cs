using UnityEngine;

namespace AshenVale
{
    // One kind of monster. Like classic MapleStory, every monster kind has a fixed level and fixed stats:
    // an Emberling is always a level 1 Emberling, whoever meets it. Maps say which monsters live where
    // (see MapDefs), so a map's level range comes from its monsters.
    //
    // Behaviour:
    //   passive     wanders its platform; when hit, chases whoever hit it for a while (if it can reach them)
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

        // Chests don't use respawn: they turn up at a random chest spot now and then (World.UpdateChests).
        public bool IsChest { get { return chestParts != null; } }
    }

    public static class Monsters
    {
        // The first four keep their old numbers.
        public const int Emberling = 0, EliteEmberling = 1, MossCrawler = 2, VioletWraith = 3,
                         AshSprout = 4, OldChest = 5, BrambleBoar = 6, MossyChest = 7, DenShade = 8,
                         MarshToad = 9, BogLurker = 10, AshWisp = 11, SunkenChest = 12,
                         LeafBeetle = 13, EmberFox = 14, Mossling = 15, CragGoat = 16, RockMite = 17;

        public static readonly MonsterDef[] All =
        {
            new MonsterDef { key = "emberling", name = "Emberling", level = 1, color = new Color(.86f, .38f, .25f),
                width = .85f, height = .9f, hp = 40, speed = 1.6f, damage = 7, xp = 7, goldMin = 4, goldRange = 5,
                part = Parts.EmberAsh },
            new MonsterDef { key = "elite_emberling", name = "Elite Emberling", level = 4, color = new Color(.78f, .25f, .62f),
                width = 1.15f, height = 1.35f, hp = 130, speed = 1.5f, damage = 14, xp = 22, goldMin = 14, goldRange = 9,
                aggressive = true, respawn = 15f,
                part = Parts.EmberCore, partChance = .5f },
            new MonsterDef { key = "moss_crawler", name = "Moss Crawler", level = 6, color = new Color(.31f, .71f, .45f),
                width = 1f, height = .7f, hp = 140, speed = 1.4f, damage = 14, xp = 20, goldMin = 15, goldRange = 10,
                part = Parts.MossTuft },
            new MonsterDef { key = "violet_wraith", name = "Violet Wraith", level = 10, color = new Color(.58f, .33f, .92f),
                width = 1.2f, height = 1.5f, hover = .15f, hp = 270, speed = 2.2f, damage = 24, xp = 33, goldMin = 23, goldRange = 14,
                aggressive = true,
                part = Parts.WraithSilk },
            new MonsterDef { key = "ash_sprout", name = "Ash Sprout", level = 2, color = new Color(.62f, .7f, .38f),
                width = .8f, height = .8f, hp = 55, speed = 1f, damage = 6, xp = 10, goldMin = 6, goldRange = 6,
                part = Parts.SproutLeaf },
            new MonsterDef { key = "old_chest", name = "Old Chest", level = 4, color = new Color(.62f, .43f, .22f),
                width = 1f, height = .8f, hp = 100, speed = 0f, damage = 0, xp = 20, goldMin = 35, goldRange = 25,
                harmless = true, respawn = 45f, itemChance = .6f,
                chestParts = new[] { Parts.EmberAsh, Parts.SproutLeaf, Parts.EmberAsh, Parts.SproutLeaf, Parts.EmberCore } },
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
                width = .95f, height = .7f, hp = 85, speed = 1.1f, damage = 8, xp = 13, goldMin = 8, goldRange = 8,
                part = Parts.SproutLeaf },
            new MonsterDef { key = "ember_fox", name = "Ember Fox", level = 5, color = new Color(.95f, .5f, .18f),
                width = 1.1f, height = .85f, hp = 120, speed = 2.6f, damage = 13, xp = 18, goldMin = 12, goldRange = 9,
                aggressive = true, part = Parts.EmberCore, partChance = .25f },
            new MonsterDef { key = "mossling", name = "Mossling", level = 4, color = new Color(.42f, .66f, .36f),
                width = .8f, height = .75f, hp = 100, speed = 1.2f, damage = 10, xp = 15, goldMin = 10, goldRange = 8,
                part = Parts.MossTuft, partChance = .3f },
            new MonsterDef { key = "crag_goat", name = "Crag Goat", level = 9, color = new Color(.72f, .66f, .58f),
                width = 1.3f, height = 1.1f, hp = 230, speed = 2.9f, damage = 21, xp = 30, goldMin = 20, goldRange = 13,
                aggressive = true,
                part = Parts.BoarTusk },           // its horn works as tusk does
            new MonsterDef { key = "rock_mite", name = "Rock Mite", level = 10, color = new Color(.45f, .5f, .58f),
                width = .9f, height = .7f, hp = 300, speed = .9f, damage = 25, xp = 34, goldMin = 24, goldRange = 14,
                part = Parts.ShadeEssence, partChance = .25f },   // a second source of Shade Essence outside the den
        };
    }
}
