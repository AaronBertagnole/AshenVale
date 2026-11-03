using UnityEngine;

namespace AshenVale
{
    // Region 3, Highspire: the snowy mountains above Shale Cliffs, where the dwarves of Highspire hold out in
    // their hall and the old forge still burns under the rock. Levels 15-24. Curve from here on: HP about 24 a
    // level (tough kinds half again), touch damage about 2 a level + 5, XP 3.2 a level + level squared / 10.
    public static partial class Monsters
    {
        static MonsterDef[] HighspireMonsters()
        {
            return new[]
            {
                new MonsterDef { key = "frost_wolf", name = "Frost Wolf", level = 15, color = new Color(.78f, .84f, .92f),
                    width = 1.4f, height = 1f, hp = 360, speed = 3f, damage = 34, xp = 70, goldMin = 36, goldRange = 20,
                    aggressive = true,
                    part = Parts.FrostPelt },
                new MonsterDef { key = "snow_hare", name = "Snow Hare", level = 16, color = new Color(.95f, .96f, 1f),
                    width = .9f, height = .8f, hp = 330, speed = 2.3f, damage = 30, xp = 72, goldMin = 34, goldRange = 18,
                    part = Parts.FrostPelt, partChance = .3f },
                new MonsterDef { key = "icicle_bat", name = "Icicle Bat", level = 17, color = new Color(.6f, .78f, .95f),
                    width = 1f, height = .7f, hover = .45f, hp = 380, speed = 3.2f, damage = 36, xp = 78, goldMin = 38, goldRange = 20,
                    aggressive = true,
                    part = Parts.DwarfRune, partChance = .2f },
                new MonsterDef { key = "rime_wisp", name = "Rime Wisp", level = 18, color = new Color(.75f, .95f, 1f),
                    width = .8f, height = .9f, hover = .35f, hp = 400, speed = 3f, damage = 40, xp = 84, goldMin = 42, goldRange = 22,
                    aggressive = true,
                    part = Parts.DwarfRune, partChance = .25f },
                new MonsterDef { key = "frost_troll", name = "Frost Troll", level = 19, color = new Color(.55f, .66f, .72f),
                    width = 1.6f, height = 1.7f, hp = 640, speed = 1.5f, damage = 48, xp = 100, goldMin = 48, goldRange = 26,
                    aggressive = true, respawn = 12f,
                    part = Parts.TrollTooth },
                new MonsterDef { key = "forge_imp", name = "Forge Imp", level = 20, color = new Color(.95f, .45f, .15f),
                    width = .8f, height = .85f, hp = 420, speed = 3.3f, damage = 42, xp = 92, goldMin = 46, goldRange = 24,
                    aggressive = true,
                    part = Parts.DwarfRune, partChance = .2f },
                new MonsterDef { key = "slag_beetle", name = "Slag Beetle", level = 21, color = new Color(.4f, .3f, .28f),
                    width = 1.1f, height = .75f, hp = 560, speed = 1f, damage = 46, xp = 98, goldMin = 50, goldRange = 26,
                    part = Parts.DwarfRune, partChance = .3f },        // it eats rune-stone
                new MonsterDef { key = "rune_sentinel", name = "Rune Sentinel", level = 22, color = new Color(.5f, .58f, .7f),
                    width = 1.4f, height = 1.8f, hp = 900, speed = .7f, damage = 50, xp = 120, goldMin = 58, goldRange = 30,
                    respawn = 15f,
                    part = Parts.DwarfRune, partChance = .6f },        // the dwarves' old guardians: slow, hard, worth it
                new MonsterDef { key = "cave_troll", name = "Cave Troll", level = 23, color = new Color(.42f, .45f, .4f),
                    width = 1.7f, height = 1.8f, hp = 760, speed = 1.7f, damage = 56, xp = 118, goldMin = 56, goldRange = 30,
                    aggressive = true, respawn = 12f,
                    part = Parts.TrollTooth },
                // Region 3's boss: the war golem the dwarves built and could not stop, rusting in its vault under the forge.
                new MonsterDef { key = "rusted_colossus", name = "Rusted Colossus", level = 24, color = new Color(.5f, .32f, .2f),
                    width = 3f, height = 3.6f, hp = 7000, speed = 1f, damage = 64, xp = 1500, goldMin = 500, goldRange = 300,
                    aggressive = true, boss = true, minion = ForgeImp, respawn = Tuning.BossRespawn, itemChance = .9f,
                    part = Parts.DwarfRune, partChance = 1f,
                    chestParts = new[] { Parts.TrollTooth, Parts.FrostPelt, Parts.DwarfRune, Parts.TrollTooth, Parts.FrostPelt, Parts.DwarfRune } },
                new MonsterDef { key = "frost_chest", name = "Frost Chest", level = 20, color = new Color(.6f, .75f, .9f),
                    width = 1.1f, height = .9f, hp = 700, speed = 0f, damage = 0, xp = 110, goldMin = 180, goldRange = 90,
                    harmless = true, respawn = 45f, itemChance = .6f,
                    chestParts = new[] { Parts.FrostPelt, Parts.TrollTooth, Parts.FrostPelt, Parts.TrollTooth, Parts.DwarfRune } },
            };
        }
    }
}
