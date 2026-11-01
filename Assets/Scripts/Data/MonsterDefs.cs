using UnityEngine;

namespace AshenVale
{
    // One kind of monster. Maps refer to monsters by their index in Monsters.All.
    // A monster's level is the level of the player it spawned near; HP, hits and XP grow with it.
    public sealed class MonsterDef
    {
        public string name;
        public Color color;             // placeholder look until monsters get real sprites
        public float width, height;
        public int baseHp, hpPerLevel;
        public float speed;
        public int damage;              // contact damage at level 1
        public int xpBase, xpPerLevel;  // XP = xpBase + (level - 1) * xpPerLevel
        public int goldMin, goldRange;  // gold = goldMin + random(0 .. goldRange - 1)
    }

    public static class Monsters
    {
        public const int Emberling = 0, EliteEmberling = 1, MossCrawler = 2, VioletWraith = 3;

        public static readonly MonsterDef[] All =
        {
            new MonsterDef { name = "Emberling", color = new Color(.82f, .35f, .25f), width = .85f, height = 1f,
                baseHp = 45, hpPerLevel = 8, speed = 2.1f, damage = 10, xpBase = 13, xpPerLevel = 5, goldMin = 5, goldRange = 10 },
            new MonsterDef { name = "Elite Emberling", color = new Color(.75f, .23f, .62f), width = 1.15f, height = 1.35f,
                baseHp = 110, hpPerLevel = 12, speed = 1.5f, damage = 19, xpBase = 32, xpPerLevel = 10, goldMin = 20, goldRange = 12 },
            new MonsterDef { name = "Moss Crawler", color = new Color(.29f, .7f, .45f), width = .85f, height = 1f,
                baseHp = 65, hpPerLevel = 7, speed = 1.7f, damage = 13, xpBase = 22, xpPerLevel = 7, goldMin = 11, goldRange = 12 },
            new MonsterDef { name = "Violet Wraith", color = new Color(.56f, .3f, .9f), width = 1.2f, height = 1.5f,
                baseHp = 125, hpPerLevel = 10, speed = 2.6f, damage = 23, xpBase = 44, xpPerLevel = 12, goldMin = 27, goldRange = 17 },
        };
    }
}
