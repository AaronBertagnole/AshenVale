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
        public float playSeconds;
        public ItemData[] inventory = new ItemData[0];
        public ItemData[] equipped = new ItemData[0];   // only filled slots; each item knows its slot
    }

    [Serializable]
    public sealed class ItemData
    {
        public string name;
        public int rarity, slot, attack, defense, health, price;
    }
}
