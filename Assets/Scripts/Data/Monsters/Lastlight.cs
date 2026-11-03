using UnityEngine;

namespace AshenVale
{
    // Region 6, the ruins: the city under the forest that was a story until somebody found the stair. Nothing
    // down here is cute. Lastlight is the camp of the ones who found it and stayed. Levels 45-55.
    public static partial class Monsters
    {
        static MonsterDef[] LastlightMonsters()
        {
            return new[]
            {
                new MonsterDef { key = "hollow_pilgrim", name = "Hollow Pilgrim", level = 45, color = new Color(.5f, .45f, .4f),
                    width = 1f, height = 1.5f, hp = 1080, speed = 1.3f, damage = 95, xp = 346, goldMin = 104, goldRange = 46,
                    part = Parts.ShadowWeave, partChance = .35f },
                new MonsterDef { key = "bone_crawler", name = "Bone Crawler", level = 46, color = new Color(.8f, .78f, .7f),
                    width = 1.3f, height = .7f, hp = 1105, speed = 2.6f, damage = 97, xp = 357, goldMin = 106, goldRange = 46,
                    aggressive = true,
                    part = Parts.CursedBone },
                new MonsterDef { key = "gravewisp", name = "Gravewisp", level = 47, color = new Color(.5f, .9f, .6f),
                    width = .8f, height = .9f, hover = .45f, hp = 1130, speed = 3.2f, damage = 99, xp = 371, goldMin = 108, goldRange = 48,
                    aggressive = true,
                    part = Parts.VoidShard, partChance = .15f },
                new MonsterDef { key = "hollow_knight", name = "Hollow Knight", level = 48, color = new Color(.3f, .3f, .35f),
                    width = 1.2f, height = 1.7f, hp = 1150, speed = 1.8f, damage = 101, xp = 384, goldMin = 110, goldRange = 48,
                    aggressive = true,
                    part = Parts.ShadowWeave },
                new MonsterDef { key = "flayed_hound", name = "Flayed Hound", level = 49, color = new Color(.6f, .2f, .2f),
                    width = 1.4f, height = .9f, hp = 1175, speed = 3.5f, damage = 103, xp = 397, goldMin = 112, goldRange = 50,
                    aggressive = true,
                    part = Parts.CursedBone },
                new MonsterDef { key = "idol_warden", name = "Idol Warden", level = 50, color = new Color(.35f, .32f, .3f),
                    width = 1.6f, height = 2.2f, hp = 1800, speed = .7f, damage = 105, xp = 410, goldMin = 115, goldRange = 50,
                    respawn = 15f,
                    part = Parts.VoidShard, partChance = .6f },
                new MonsterDef { key = "cult_shade", name = "Cult Shade", level = 51, color = new Color(.3f, .15f, .35f),
                    width = 1.1f, height = 1.6f, hover = .1f, hp = 1225, speed = 2.5f, damage = 107, xp = 423, goldMin = 118, goldRange = 52,
                    aggressive = true,
                    part = Parts.ShadowWeave },
                new MonsterDef { key = "bone_colossus", name = "Bone Colossus", level = 52, color = new Color(.75f, .72f, .62f),
                    width = 2f, height = 2.4f, hp = 1900, speed = .9f, damage = 109, xp = 436, goldMin = 120, goldRange = 52,
                    respawn = 15f,
                    part = Parts.CursedBone, partChance = .8f },
                new MonsterDef { key = "carrion_horror", name = "Carrion Horror", level = 53, color = new Color(.45f, .3f, .25f),
                    width = 1.6f, height = 1.5f, hp = 1270, speed = 2.2f, damage = 111, xp = 450, goldMin = 122, goldRange = 54,
                    aggressive = true,
                    part = Parts.CursedBone },
                new MonsterDef { key = "herald_of_the_deep", name = "Herald of the Deep", level = 54, color = new Color(.2f, .1f, .3f),
                    width = 1.4f, height = 2f, hover = .2f, hp = 1300, speed = 2.3f, damage = 113, xp = 464, goldMin = 124, goldRange = 54,
                    aggressive = true, respawn = 12f,
                    part = Parts.VoidShard, partChance = .3f },
                new MonsterDef { key = "void_spawn", name = "Void Spawn", level = 55, color = new Color(.35f, .05f, .45f),
                    width = 1f, height = 1f, hover = .3f, hp = 1320, speed = 3.4f, damage = 115, xp = 478, goldMin = 126, goldRange = 56,
                    aggressive = true,
                    part = Parts.VoidShard, partChance = .2f },
                // Region 6's boss: the thing the city was built to keep asleep. It is not asleep.
                new MonsterDef { key = "the_sleeper", name = "The Sleeper Beneath", level = 55, color = new Color(.15f, .05f, .2f),
                    width = 3.4f, height = 4f, hp = 32000, speed = 1.1f, damage = 140, xp = 12000, goldMin = 2500, goldRange = 1500,
                    aggressive = true, boss = true, minion = VoidSpawn, respawn = Tuning.BossRespawn, itemChance = 1f,
                    part = Parts.VoidShard, partChance = 1f,
                    chestParts = new[] { Parts.CursedBone, Parts.ShadowWeave, Parts.VoidShard, Parts.CursedBone, Parts.ShadowWeave, Parts.VoidShard } },
                new MonsterDef { key = "sealed_chest", name = "Sealed Chest", level = 50, color = new Color(.3f, .25f, .3f),
                    width = 1.1f, height = .9f, hp = 2000, speed = 0f, damage = 0, xp = 600, goldMin = 650, goldRange = 300,
                    harmless = true, respawn = 45f, itemChance = .6f,
                    chestParts = new[] { Parts.ShadowWeave, Parts.CursedBone, Parts.ShadowWeave, Parts.CursedBone, Parts.VoidShard } },
            };
        }
    }
}
