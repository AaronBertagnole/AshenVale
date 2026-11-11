using UnityEngine;

namespace AshenVale
{
    public enum Rarity { Common, Rare, Epic }
    public enum Slot { Weapon, Helmet, Chest, Boots }

    // A piece of gear. attack / defense / health are its base stats; smithy upgrades (+1 .. +5) add to them,
    // so read Attack / Defense / Health for what it actually gives.
    public sealed class Item
    {
        public string name;
        public Rarity rarity;
        public Slot slot;
        public int attack, defense, health, price;
        public int level = 1;       // level of the monster (or hero, for shop gear) it came from: sets its smithing tier
        public int upgrade;         // smithy upgrades, 0 .. Tuning.MaxUpgrade

        public int Attack { get { return attack + upgrade * Smithing.Step(attack); } }
        public int Defense { get { return defense + upgrade * Smithing.Step(defense); } }
        public int Health { get { return health + upgrade * Smithing.Step(health); } }
        public string DisplayName { get { return upgrade > 0 ? name + " +" + upgrade : name; } }
    }

    // Monster parts: dropped by monsters, kept in a bag of their own (they don't take inventory slots),
    // spent at a smithy to upgrade gear.
    public sealed class PartDef
    {
        public string name;
        public Color color;     // drop colour and text tint (the drop blob until the part has an icon)
        public int price;       // what the merchant pays for one
        // Resources/Art/Parts/<key>.png: the part's painted icon (64 x 64), used for drops and in lists if present.
        public string Key { get { return name.ToLowerInvariant().Replace(' ', '_'); } }
    }

    public static class Parts
    {
        // Stored in save files by number: never renumber, only add.
        public const int ShrewTuft = 0, SproutLeaf = 1, EmberCore = 2, MossTuft = 3, BoarTusk = 4, ShadeEssence = 5,
                         WraithSilk = 6, ToadSlime = 7, LurkerScale = 8, WispEmber = 9,
                         // Highspire (snowy mountains, items Lv 16-25)
                         FrostPelt = 10, TrollTooth = 11, DwarfRune = 12,
                         // Saltmarrow coast (items Lv 26-35)
                         KelpWeave = 13, SharkFang = 14, PirateGold = 15,
                         // The deep forest (items Lv 36-45)
                         SpiderSilk = 16, OwlFeather = 17, ArcaneDust = 18,
                         // The ruins (items Lv 46+)
                         ShadowWeave = 19, CursedBone = 20, VoidShard = 21,
                         HedgehogQuill = 22;    // Emberfields' elite; sold, not smithed

        public static readonly PartDef[] All =
        {
            new PartDef { name = "Shrew Tuft", price = 4, color = new Color(.72f, .56f, .38f) },
            new PartDef { name = "Sprout Leaf", price = 4, color = new Color(.65f, .85f, .35f) },
            new PartDef { name = "Ember Core", price = 15, color = new Color(1f, .3f, .55f) },
            new PartDef { name = "Moss Tuft", price = 7, color = new Color(.3f, .75f, .45f) },
            new PartDef { name = "Boar Tusk", price = 9, color = new Color(.95f, .9f, .75f) },
            new PartDef { name = "Shade Essence", price = 20, color = new Color(.45f, .45f, .7f) },
            new PartDef { name = "Wraith Silk", price = 22, color = new Color(.7f, .45f, 1f) },
            new PartDef { name = "Toad Slime", price = 12, color = new Color(.55f, .75f, .35f) },
            new PartDef { name = "Lurker Scale", price = 14, color = new Color(.35f, .6f, .6f) },
            new PartDef { name = "Wisp Ember", price = 30, color = new Color(1f, .95f, .7f) },
            new PartDef { name = "Frost Pelt", price = 18, color = new Color(.85f, .92f, 1f) },
            new PartDef { name = "Troll Tooth", price = 20, color = new Color(.9f, .85f, .7f) },
            new PartDef { name = "Dwarf Rune", price = 45, color = new Color(.4f, .8f, 1f) },
            new PartDef { name = "Kelp Weave", price = 26, color = new Color(.3f, .6f, .45f) },
            new PartDef { name = "Shark Fang", price = 30, color = new Color(.95f, .95f, .9f) },
            new PartDef { name = "Pirate Gold", price = 70, color = new Color(1f, .85f, .3f) },
            new PartDef { name = "Spider Silk", price = 36, color = new Color(.9f, .9f, .95f) },
            new PartDef { name = "Owl Feather", price = 40, color = new Color(.6f, .5f, .4f) },
            new PartDef { name = "Arcane Dust", price = 95, color = new Color(.7f, .5f, 1f) },
            new PartDef { name = "Shadow Weave", price = 48, color = new Color(.3f, .25f, .4f) },
            new PartDef { name = "Cursed Bone", price = 52, color = new Color(.8f, .78f, .7f) },
            new PartDef { name = "Void Shard", price = 130, color = new Color(.5f, .1f, .6f) },
            new PartDef { name = "Hedgehog Quill", price = 9, color = new Color(.8f, .7f, .5f) },
        };
    }

    // What a smithy upgrade costs. Each +1 adds about 12% of the item's base stats (at least 1), up to +5,
    // so a fully upgraded item is roughly 60% stronger: worth the grind, but no runaway power.
    // An upgrade always succeeds. It costs gold plus parts from monsters of the item's level band:
    //   items Lv 1-5    weapons: Shrew Tuft + Ember Core      armor: Sprout Leaf + Ember Core     (Emberfields)
    //   items Lv 6-10   weapons: Boar Tusk + Wraith Silk     armor: Moss Tuft + Shade Essence   (Mosswood, Kingsroad, Hollow Den)
    //   items Lv 11-15  weapons: Lurker Scale + Wisp Ember   armor: Toad Slime + Wisp Ember     (Ashen Marsh)
    //   items Lv 16-25  Troll Tooth / Frost Pelt + Dwarf Rune (Highspire); 26-35 Shark Fang / Kelp Weave + Pirate Gold (coast);
    //   36-45 Owl Feather / Spider Silk + Arcane Dust (deep forest); 46+ Cursed Bone / Shadow Weave + Void Shard (ruins)
    // The second part is the rare one, needed from +3 on.
    public struct UpgradeCost
    {
        public int part, partCount, rarePart, rareCount, gold;
    }

    public static class Smithing
    {
        public const int InventoryTarget = 100;     // Upgrade targets: 0-3 worn slot, 100 + i inventory item i

        // Main part, rare part per tier (see Tier): Emberhaven, Mosswood, Ashen Marsh, Highspire, the coast, the deep forest, the ruins.
        static readonly int[] WeaponParts = { Parts.ShrewTuft, Parts.EmberCore, Parts.BoarTusk, Parts.WraithSilk, Parts.LurkerScale, Parts.WispEmber,
                                              Parts.TrollTooth, Parts.DwarfRune, Parts.SharkFang, Parts.PirateGold, Parts.OwlFeather, Parts.ArcaneDust,
                                              Parts.CursedBone, Parts.VoidShard };
        static readonly int[] ArmorParts = { Parts.SproutLeaf, Parts.EmberCore, Parts.MossTuft, Parts.ShadeEssence, Parts.ToadSlime, Parts.WispEmber,
                                             Parts.FrostPelt, Parts.DwarfRune, Parts.KelpWeave, Parts.PirateGold, Parts.SpiderSilk, Parts.ArcaneDust,
                                             Parts.ShadowWeave, Parts.VoidShard };

        public static int Step(int baseStat) { return baseStat <= 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(baseStat * Tuning.UpgradeStatGain)); }

        public static int Tier(int itemLevel) { return itemLevel <= 5 ? 0 : itemLevel <= 10 ? 1 : itemLevel <= 15 ? 2 : itemLevel <= 25 ? 3 : itemLevel <= 35 ? 4 : itemLevel <= 45 ? 5 : 6; }

        // Cost of the next upgrade (from +k to +k+1).
        public static UpgradeCost Cost(Item item)
        {
            int k = item.upgrade, tier = Tier(item.level);
            var table = item.slot == Slot.Weapon ? WeaponParts : ArmorParts;
            return new UpgradeCost
            {
                part = table[tier * 2], partCount = 3 + 2 * k,
                rarePart = table[tier * 2 + 1], rareCount = k >= 2 ? k - 1 : 0,
                gold = (k + 1) * (20 + 8 * Mathf.Max(1, item.level)),
            };
        }

        public static bool CanPay(Character ch, UpgradeCost cost)
        {
            return ch.gold >= cost.gold && ch.parts[cost.part] >= cost.partCount && ch.parts[cost.rarePart] >= cost.rareCount;
        }

        public static void Pay(Character ch, UpgradeCost cost)
        {
            ch.gold -= cost.gold;
            ch.parts[cost.part] -= cost.partCount;
            ch.parts[cost.rarePart] -= cost.rareCount;
        }
    }

    // How items are rolled and priced.
    public static class Loot
    {
        static readonly string[] SlotNames = { "Weapon", "Helmet", "Armor", "Boots" };

        // Painted icon for a piece of gear (under Resources): one per armour slot, and one per hero's weapon.
        // An item doesn't record its hero, so a weapon is told apart by its name ("Rare Bow").
        public static string IconPath(Item item)
        {
            if (item == null) return null;
            switch (item.slot)
            {
                case Slot.Helmet: return "Art/Items/frostguard_helmet";
                case Slot.Chest: return "Art/Items/wayfarer_vest";
                case Slot.Boots: return "Art/Items/wayfarer_boots";
            }
            string name = item.name ?? "";
            if (name.EndsWith("Bow")) return "Art/Items/glimmer_bow";
            if (name.EndsWith("Crystal")) return "Art/Items/sapphire_cluster";
            return "Art/Items/briar_spear";
        }

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
                price = (25 + level * 10) * mult * mult, level = Mathf.Max(1, level),
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
                price = ShopGearCost(slot, level), level = Mathf.Max(1, level),
                attack = slot == Slot.Weapon ? 3 + level / 3 : 0,
                defense = slot == Slot.Helmet || slot == Slot.Chest ? 2 + level / 4 : 0,
                health = slot == Slot.Chest || slot == Slot.Boots ? 6 + level : 0,
            };
        }

        // Half its value; each upgrade adds a quarter of the base value.
        public static int SellPrice(Item item) { return Mathf.Max(1, item.price * (4 + item.upgrade) / 8); }

        public static Color RarityColor(Rarity r)
        {
            return r == Rarity.Epic ? new Color(.72f, .35f, 1) : r == Rarity.Rare ? new Color(.3f, .65f, 1) : new Color(.8f, .8f, .8f);
        }
    }
}
