using UnityEngine;

namespace AshenVale
{
    // Region 4, the Saltmarrow coast: beaches and reefs east of the Ashen Marsh where pirates once ran the tide.
    // Their port is still a port; what they left behind is less friendly. Levels 25-34.
    public static partial class Monsters
    {
        static MonsterDef[] SaltmarrowMonsters()
        {
            return new[]
            {
                new MonsterDef { key = "sand_crab", name = "Sand Crab", level = 25, color = new Color(.9f, .6f, .4f),
                    width = 1.1f, height = .7f, hp = 600, speed = 1.3f, damage = 55, xp = 142, goldMin = 58, goldRange = 26,
                    part = Parts.KelpWeave },
                new MonsterDef { key = "gull_harpy", name = "Gull Harpy", level = 26, color = new Color(.9f, .9f, .85f),
                    width = 1.2f, height = 1f, hover = .5f, hp = 625, speed = 3.2f, damage = 57, xp = 146, goldMin = 60, goldRange = 28,
                    aggressive = true,
                    part = Parts.PirateGold, partChance = .15f },      // it steals shiny things
                new MonsterDef { key = "tide_lurcher", name = "Tide Lurcher", level = 27, color = new Color(.3f, .55f, .55f),
                    width = 1.5f, height = 1.2f, hp = 650, speed = 1.8f, damage = 59, xp = 151, goldMin = 62, goldRange = 28,
                    aggressive = true,
                    part = Parts.SharkFang },
                new MonsterDef { key = "reef_eel", name = "Reef Eel", level = 28, color = new Color(.35f, .7f, .6f),
                    width = 1.6f, height = .6f, hp = 670, speed = 2.6f, damage = 61, xp = 156, goldMin = 64, goldRange = 30,
                    aggressive = true,
                    part = Parts.SharkFang },
                new MonsterDef { key = "cutlass_rat", name = "Cutlass Rat", level = 29, color = new Color(.5f, .4f, .35f),
                    width = .9f, height = .8f, hp = 700, speed = 3.4f, damage = 63, xp = 161, goldMin = 66, goldRange = 30,
                    aggressive = true,
                    part = Parts.PirateGold, partChance = .2f },
                new MonsterDef { key = "powder_monkey", name = "Powder Monkey", level = 30, color = new Color(.7f, .5f, .3f),
                    width = .9f, height = .9f, hp = 720, speed = 3f, damage = 65, xp = 166, goldMin = 70, goldRange = 32,
                    aggressive = true,
                    part = Parts.PirateGold, partChance = .25f },
                new MonsterDef { key = "barrel_mimic", name = "Barrel Mimic", level = 31, color = new Color(.55f, .4f, .25f),
                    width = 1.1f, height = 1.1f, hp = 1100, speed = .6f, damage = 67, xp = 171, goldMin = 72, goldRange = 34,
                    respawn = 15f,
                    part = Parts.PirateGold, partChance = .5f },       // a barrel of it, if you can open it
                new MonsterDef { key = "deck_ghoul", name = "Deck Ghoul", level = 32, color = new Color(.5f, .6f, .5f),
                    width = 1.1f, height = 1.5f, hp = 770, speed = 1.9f, damage = 69, xp = 176, goldMin = 74, goldRange = 34,
                    aggressive = true,
                    part = Parts.KelpWeave },
                new MonsterDef { key = "bosun_wraith", name = "Bosun Wraith", level = 33, color = new Color(.6f, .75f, .9f),
                    width = 1.2f, height = 1.5f, hover = .25f, hp = 790, speed = 2.5f, damage = 71, xp = 181, goldMin = 76, goldRange = 36,
                    aggressive = true,
                    part = Parts.PirateGold, partChance = .3f },
                new MonsterDef { key = "hanged_corsair", name = "Hanged Corsair", level = 34, color = new Color(.4f, .38f, .42f),
                    width = 1.2f, height = 1.6f, hp = 1050, speed = 2.1f, damage = 73, xp = 186, goldMin = 78, goldRange = 36,
                    aggressive = true, respawn = 12f,
                    part = Parts.SharkFang },
                // Region 4's boss: the captain who sank with his gold and would not stay down.
                new MonsterDef { key = "captain_marrow", name = "Captain Marrow", level = 34, color = new Color(.25f, .35f, .4f),
                    width = 2.4f, height = 3.2f, hp = 12000, speed = 1.3f, damage = 90, xp = 3200, goldMin = 900, goldRange = 500,
                    aggressive = true, boss = true, minion = CutlassRat, respawn = Tuning.BossRespawn, itemChance = .9f,
                    part = Parts.PirateGold, partChance = 1f,
                    chestParts = new[] { Parts.SharkFang, Parts.KelpWeave, Parts.PirateGold, Parts.SharkFang, Parts.KelpWeave, Parts.PirateGold } },
                new MonsterDef { key = "barnacle_chest", name = "Barnacle Chest", level = 30, color = new Color(.45f, .55f, .5f),
                    width = 1.1f, height = .9f, hp = 1100, speed = 0f, damage = 0, xp = 220, goldMin = 300, goldRange = 150,
                    harmless = true, respawn = 45f, itemChance = .6f,
                    chestParts = new[] { Parts.KelpWeave, Parts.SharkFang, Parts.KelpWeave, Parts.SharkFang, Parts.PirateGold } },
            };
        }
    }
}
