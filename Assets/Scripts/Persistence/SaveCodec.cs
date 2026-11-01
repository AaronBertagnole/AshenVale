using System;
using UnityEngine;

namespace AshenVale
{
    // Converts between a hero in the world and its save data, and checks loaded values.
    public static class SaveCodec
    {
        // 1: first save format.  2: adds lastTown and towns (older files start with Emberhaven only).
        public const int Version = 2;

        // forSave: true for the save file (a fallen hero is stored as woken in town); false for sending a
        // hero's live data to its player online.
        public static CharacterData ToData(PlayerState p, bool forSave = true)
        {
            var ch = p.ch;
            var d = new CharacterData
            {
                id = ch.id, name = ch.name, hero = ch.hero, level = ch.level, xp = ch.xp, gold = ch.gold, kills = ch.kills,
                hpPotions = ch.hpPotions, mpPotions = ch.mpPotions, playSeconds = ch.playSeconds,
                map = p.map, hp = p.hp, mp = p.mp, lastTown = ch.lastTown, towns = ch.towns, savedAt = DateTime.UtcNow.ToString("o"),
            };
            if (p.dead && forSave)
            {
                // Quitting while fallen counts as waking in town: same XP cost as pressing R.
                d.xp = Mathf.Max(0, ch.xp - Mathf.RoundToInt(Tuning.XpNeeded(ch.level) * Tuning.DeathXpLoss));
                d.map = Maps.IsTown(ch.lastTown) ? ch.lastTown : Maps.FirstTown;
                d.hp = 0;
            }
            d.inventory = new ItemData[ch.inventory.Count];
            for (int i = 0; i < ch.inventory.Count; i++) d.inventory[i] = ToData(ch.inventory[i]);
            int worn = 0;
            foreach (var item in ch.equipped) if (item != null) worn++;
            d.equipped = new ItemData[worn];
            worn = 0;
            foreach (var item in ch.equipped) if (item != null) d.equipped[worn++] = ToData(item);
            return d;
        }

        public static Character ToCharacter(CharacterData d)
        {
            var ch = new Character
            {
                id = d.id,
                name = string.IsNullOrEmpty(d.name) ? "Hero" : d.name,
                hero = Mathf.Clamp(d.hero, 0, Heroes.All.Length - 1),
                level = Mathf.Max(1, d.level),
                xp = Mathf.Max(0, d.xp),
                gold = Mathf.Max(0, d.gold),
                kills = Mathf.Max(0, d.kills),
                hpPotions = Mathf.Max(0, d.hpPotions),
                mpPotions = Mathf.Max(0, d.mpPotions),
                playSeconds = Mathf.Max(0, d.playSeconds),
            };
            // Towns: keep only real town ids; everyone knows the first town.
            int towns = 1 << Maps.FirstTown;
            if (d.version >= 2)
                for (int i = 0; i < Maps.All.Length && i < 32; i++)
                    if ((d.towns & (1 << i)) != 0 && Maps.IsTown(i)) towns |= 1 << i;
            ch.towns = towns;
            ch.lastTown = d.version >= 2 && Maps.IsTown(d.lastTown) && ch.KnowsTown(d.lastTown) ? d.lastTown : Maps.FirstTown;
            if (d.inventory != null)
                foreach (var item in d.inventory)
                    if (item != null && ch.inventory.Count < Tuning.InventoryCapacity) ch.inventory.Add(ToItem(item));
            if (d.equipped != null)
                foreach (var data in d.equipped)
                {
                    if (data == null) continue;
                    var item = ToItem(data);
                    int slot = (int)item.slot;
                    if (ch.equipped[slot] == null) ch.equipped[slot] = item;
                    else if (ch.inventory.Count < Tuning.InventoryCapacity) ch.inventory.Add(item);
                }
            return ch;
        }

        static ItemData ToData(Item item)
        {
            return new ItemData { name = item.name, rarity = (int)item.rarity, slot = (int)item.slot,
                                  attack = item.attack, defense = item.defense, health = item.health, price = item.price };
        }

        static Item ToItem(ItemData d)
        {
            return new Item
            {
                name = d.name ?? "Item",
                rarity = (Rarity)Mathf.Clamp(d.rarity, 0, (int)Rarity.Epic),
                slot = (Slot)Mathf.Clamp(d.slot, 0, (int)Slot.Boots),
                attack = d.attack, defense = d.defense, health = d.health, price = Mathf.Max(0, d.price),
            };
        }

        // "3h 12m" / "14m" for the hero list.
        public static string FormatPlayTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            return minutes >= 60 ? (minutes / 60) + "h " + (minutes % 60) + "m" : minutes + "m";
        }
    }
}
