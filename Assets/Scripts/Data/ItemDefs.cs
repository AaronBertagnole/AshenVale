using UnityEngine;

namespace AshenVale
{
    public enum Rarity { Common, Rare, Epic }
    public enum Slot { Weapon, Helmet, Chest, Boots }

    public sealed class Item
    {
        public string name;
        public Rarity rarity;
        public Slot slot;
        public int attack, defense, health, price;
    }

    // How items are rolled and priced.
    public static class Loot
    {
        static readonly string[] SlotNames = { "Weapon", "Helmet", "Armor", "Boots" };

        public static string SlotName(Slot slot, HeroDef hero)
        {
            return slot == Slot.Weapon ? hero.weaponName : SlotNames[(int)slot];
        }

        // Drop roll: 75% Common, 20% Rare, 5% Epic. Stats grow every 3 levels; Rare doubles them, Epic triples.
        public static Item Roll(System.Random rng, int level, HeroDef hero)
        {
            Slot slot = (Slot)rng.Next(4);
            double roll = rng.NextDouble();
            Rarity rarity = roll < .05 ? Rarity.Epic : roll < .25 ? Rarity.Rare : Rarity.Common;
            int mult = rarity == Rarity.Epic ? 3 : rarity == Rarity.Rare ? 2 : 1;
            int power = 1 + level / 3;
            return new Item
            {
                name = rarity + " " + SlotName(slot, hero), slot = slot, rarity = rarity,
                attack = slot == Slot.Weapon ? mult * (2 + power) : 0,
                defense = slot == Slot.Helmet || slot == Slot.Chest ? mult * (1 + power / 2) : 0,
                health = slot == Slot.Boots || slot == Slot.Chest ? mult * (4 + power * 2) : 0,
                price = (25 + level * 10) * mult * mult,
            };
        }

        public static int ShopGearCost(Slot slot, int level)
        {
            return 150 + level * 25 + (slot == Slot.Weapon ? 100 : 0);
        }

        // The merchant's reliable Common gear.
        public static Item ShopGear(Slot slot, int level, HeroDef hero)
        {
            return new Item
            {
                name = "Common " + SlotName(slot, hero), slot = slot, rarity = Rarity.Common,
                price = ShopGearCost(slot, level),
                attack = slot == Slot.Weapon ? 3 + level / 3 : 0,
                defense = slot == Slot.Helmet || slot == Slot.Chest ? 2 + level / 4 : 0,
                health = slot == Slot.Chest || slot == Slot.Boots ? 6 + level : 0,
            };
        }

        public static int SellPrice(Item item) { return Mathf.Max(1, item.price / 2); }

        public static Color RarityColor(Rarity r)
        {
            return r == Rarity.Epic ? new Color(.72f, .35f, 1) : r == Rarity.Rare ? new Color(.3f, .65f, 1) : new Color(.8f, .8f, .8f);
        }
    }
}
