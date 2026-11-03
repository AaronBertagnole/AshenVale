using UnityEngine;

namespace AshenVale
{
    // Region 5, the deep forest: the woods north of the coast where the magicians of Eldermere once lived.
    // Their village stands, their library has gone hollow, and what they left loose in the trees is still loose.
    // Levels 35-44.
    public static partial class Monsters
    {
        static MonsterDef[] EldermereMonsters()
        {
            return new[]
            {
                new MonsterDef { key = "sylvan_stag", name = "Sylvan Stag", level = 35, color = new Color(.55f, .45f, .3f),
                    width = 1.6f, height = 1.5f, hp = 840, speed = 2.8f, damage = 75, xp = 234, goldMin = 80, goldRange = 36,
                    part = Parts.OwlFeather, partChance = .35f },      // antler velvet fletches as well
                new MonsterDef { key = "spore_puff", name = "Spore Puff", level = 35, color = new Color(.75f, .65f, .85f),
                    width = .9f, height = .8f, hover = .3f, hp = 800, speed = 1.2f, damage = 74, xp = 230, goldMin = 78, goldRange = 36,
                    part = Parts.SpiderSilk, partChance = .35f },
                new MonsterDef { key = "hex_toad", name = "Hex Toad", level = 36, color = new Color(.35f, .55f, .3f),
                    width = 1.1f, height = .8f, hp = 865, speed = 1.5f, damage = 77, xp = 245, goldMin = 82, goldRange = 36,
                    aggressive = true,
                    part = Parts.SpiderSilk },
                new MonsterDef { key = "glimmer_moth", name = "Glimmer Moth", level = 37, color = new Color(.85f, .95f, .6f),
                    width = 1.1f, height = .8f, hover = .5f, hp = 890, speed = 3.2f, damage = 79, xp = 255, goldMin = 85, goldRange = 38,
                    aggressive = true,
                    part = Parts.ArcaneDust, partChance = .15f },
                new MonsterDef { key = "vine_stalker", name = "Vine Stalker", level = 39, color = new Color(.25f, .45f, .25f),
                    width = 1.3f, height = 1.7f, hp = 935, speed = 2.3f, damage = 83, xp = 277, goldMin = 90, goldRange = 40,
                    aggressive = true,
                    part = Parts.SpiderSilk },
                new MonsterDef { key = "living_tome", name = "Living Tome", level = 40, color = new Color(.6f, .4f, .3f),
                    width = .9f, height = 1f, hover = .4f, hp = 960, speed = 1.8f, damage = 85, xp = 288, goldMin = 92, goldRange = 40,
                    part = Parts.ArcaneDust, partChance = .3f },
                new MonsterDef { key = "ink_wraith", name = "Ink Wraith", level = 41, color = new Color(.2f, .2f, .35f),
                    width = 1.2f, height = 1.6f, hover = .2f, hp = 985, speed = 2.6f, damage = 87, xp = 299, goldMin = 94, goldRange = 42,
                    aggressive = true,
                    part = Parts.ArcaneDust, partChance = .25f },
                new MonsterDef { key = "owl_sentry", name = "Owl Sentry", level = 42, color = new Color(.65f, .55f, .4f),
                    width = 1.3f, height = 1.3f, hover = .45f, hp = 1010, speed = 3f, damage = 89, xp = 311, goldMin = 96, goldRange = 42,
                    aggressive = true,
                    part = Parts.OwlFeather },
                new MonsterDef { key = "runed_golem", name = "Runed Golem", level = 43, color = new Color(.45f, .5f, .45f),
                    width = 1.6f, height = 2f, hp = 1550, speed = .8f, damage = 91, xp = 323, goldMin = 98, goldRange = 44,
                    respawn = 15f,
                    part = Parts.ArcaneDust, partChance = .6f },
                new MonsterDef { key = "wild_mote", name = "Wild Mote", level = 43, color = new Color(.9f, .6f, 1f),
                    width = .7f, height = .7f, hover = .5f, hp = 900, speed = 3.6f, damage = 90, xp = 320, goldMin = 98, goldRange = 44,
                    aggressive = true,
                    part = Parts.ArcaneDust, partChance = .2f },     // loose magic, still looking for a spell
                new MonsterDef { key = "arch_shade", name = "Arch Shade", level = 44, color = new Color(.4f, .3f, .55f),
                    width = 1.3f, height = 1.8f, hover = .15f, hp = 1060, speed = 2.4f, damage = 93, xp = 335, goldMin = 100, goldRange = 44,
                    aggressive = true, respawn = 12f,
                    part = Parts.OwlFeather },
                // Region 5's boss: what is left of the last magician of Eldermere, still keeping his sanctum.
                new MonsterDef { key = "hollow_archmage", name = "The Hollow Archmage", level = 44, color = new Color(.3f, .22f, .5f),
                    width = 2.2f, height = 3.4f, hover = .3f, hp = 20000, speed = 1.2f, damage = 115, xp = 6500, goldMin = 1500, goldRange = 800,
                    aggressive = true, boss = true, minion = WildMote, respawn = Tuning.BossRespawn, itemChance = .9f,
                    part = Parts.ArcaneDust, partChance = 1f,
                    chestParts = new[] { Parts.OwlFeather, Parts.SpiderSilk, Parts.ArcaneDust, Parts.OwlFeather, Parts.SpiderSilk, Parts.ArcaneDust } },
                new MonsterDef { key = "arcane_chest", name = "Arcane Chest", level = 40, color = new Color(.5f, .4f, .65f),
                    width = 1.1f, height = .9f, hp = 1500, speed = 0f, damage = 0, xp = 400, goldMin = 450, goldRange = 200,
                    harmless = true, respawn = 45f, itemChance = .6f,
                    chestParts = new[] { Parts.SpiderSilk, Parts.OwlFeather, Parts.SpiderSilk, Parts.OwlFeather, Parts.ArcaneDust } },
            };
        }
    }
}
