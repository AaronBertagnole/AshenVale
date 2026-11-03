using System;

namespace AshenVale
{
    // The save-file shape of a hero (one JSON file each). Kept separate from the runtime Character so the
    // file format can stay stable while the game code changes; SaveCodec converts between the two.
    // Online, this is also what a player's game will send the host when joining.
    [Serializable]
    public sealed class CharacterData
    {
        public int version = SaveCodec.Version;
        public string id, name, savedAt;
        public int hero, level = 1, xp, gold, kills, hpPotions, mpPotions;
        public int map, hp, mp;                         // where and how healthy the hero was when saved (hp 0 = fallen)
        public int lastTown = Maps.FirstTown;           // version 2: where town portals and waking up lead
        public long towns = 1L << Maps.FirstTown;       // version 2: towns found (one bit per map id), for waystones (long since the ruins: map ids past 31)
        public int[] parts = new int[0];                // version 2: monster parts carried, by Parts id
        public long maps = 1L << Maps.FirstTown;        // version 3: maps visited (one bit per map id), for the world map
        public float playSeconds;
        public ItemData[] inventory = new ItemData[0];
        public ItemData[] equipped = new ItemData[0];   // only filled slots; each item knows its slot
    }

    [Serializable]
    public sealed class ItemData
    {
        public string name;
        public int rarity, slot, attack, defense, health, price;
        public int level, upgrade;                      // version 2 (0 in older files: read as level 1, +0)
    }
}
