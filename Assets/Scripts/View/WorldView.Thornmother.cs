using System.Collections.Generic;
using UnityEngine;

namespace AshenVale
{
    // The Thornmother fight's pictures: her and her sprouts' special poses (MonsterState.pose), the warning
    // mark and bramble eruption, the thorn walls of her rooted phase, and the sprouts' healing motes.
    // Art: Resources/Art/Monsters/thornmother/{slam,summon,root,rooted,break}, .../thorn_sprout/heal and
    // Resources/Art/Effects/thornmother/{warning,eruption,wall,motes}. Anything missing is simply not drawn.
    public sealed partial class WorldView
    {
        const string ThornFx = "Art/Effects/thornmother/";
        const int ThornGroundOrder = MonsterOrder + 1, ThornFrontOrder = HeroOrder + 1;
        const float EruptShow = .55f, WallGrow = .9f, WallFall = .45f, WallScale = 1.5f, MoteFlight = .7f;

        // The frame for a monster's special pose, or null when it is just standing or walking.
        Sprite PoseFrame(MonsterView v, MonsterState m, float dt)
        {
            if (m.pose != v.pose)
            {
                v.pose = m.pose; v.poseClock = 0f;
                // Her calls and her rooting are heard as the pose begins (the slam, marks and walls sound from their events).
                if (m.pose == 3 || m.pose == 4) GameSfx.Play("thorn_summon", .8f);
                else if (m.pose == 5) GameSfx.Play("thorn_root");
            }
            else v.poseClock += dt;
            string anim; float span = 0f; int from = 0, count = 4; float loopRate = 0f;
            switch (m.pose)
            {
                case 1: anim = "slam"; count = 2; span = Tuning.BossSlamWindup; break;              // rearing up
                case 2: anim = "slam"; from = 2; count = 2; span = Tuning.BossStrikeVisual; break;  // the blow, then recovering
                case 3: anim = "summon"; span = Tuning.BossSummonWindup; break;
                case 4: anim = "summon"; span = Tuning.BossEruptWindup; break;
                case 5: anim = "root"; span = Tuning.BossRootTime; break;
                case 6: anim = "rooted"; loopRate = 5f; break;
                case 7: anim = "break"; span = Tuning.BossRootTime; break;
                case 8: anim = "heal"; loopRate = 8f; break;
                default: return null;
            }
            var frames = MonsterFrames(v.def, anim);
            if (frames.Length == 0) return null;
            int index = loopRate > 0f ? Mathf.FloorToInt(v.poseClock * loopRate) % frames.Length
                                      : from + Mathf.Min(count - 1, Mathf.FloorToInt(v.poseClock / Mathf.Max(.01f, span) * count));
            return frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }

        sealed class Bramble { public GameObject go; public SpriteRenderer sr; public float x, y, age; public bool erupted; }
        sealed class ThornWall { public GameObject go; public SpriteRenderer sr; public float x, y, age, falling = -1f; public int side; }
        sealed class HealMote { public GameObject go; public SpriteRenderer sr; public float x, y, age; public int target; }
        readonly List<Bramble> bramblesShown = new List<Bramble>();
        readonly List<ThornWall> thornWalls = new List<ThornWall>();
        readonly List<HealMote> healMotes = new List<HealMote>();

        static SpriteRenderer ThornSprite(string name, int order, out GameObject go)
        {
            go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            return sr;
        }

        // Stands a picture on the floor at (x, floor), whatever pivot it was imported with.
        static void StandOn(SpriteRenderer sr, Sprite sprite, float x, float floor, float scale = 1f)
        {
            sr.sprite = sprite;
            if (sprite == null) return;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            sr.transform.position = new Vector3(x - sprite.bounds.center.x * scale * (sr.flipX ? -1f : 1f), floor - sprite.bounds.min.y * scale, 0f);
        }

        void SpawnBramble(SimEvent e)
        {
            GameObject go;
            var sr = ThornSprite("Bramble", ThornGroundOrder, out go);
            bramblesShown.Add(new Bramble { go = go, sr = sr, x = e.x, y = e.y });
            GameSfx.Play("thorn_warn", .8f);
        }

        void SpawnThornWalls(SimEvent e)
        {
            EndThornWalls();
            GameSfx.Play("thorn_walls");
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject go;
                var sr = ThornSprite("Thorn Wall", ThornFrontOrder, out go);
                sr.flipX = side > 0;
                thornWalls.Add(new ThornWall { go = go, sr = sr, side = side, x = side < 0 ? e.x : e.amount / 100f, y = e.y });
            }
        }

        void EndThornWalls()
        {
            bool any = false;
            foreach (var wall in thornWalls) if (wall.falling < 0f) { wall.falling = 0f; any = true; }
            if (any) GameSfx.Play("thorn_break");
        }

        void SpawnHealMote(SimEvent e)
        {
            GameObject go;
            var sr = ThornSprite("Healing Mote", SlashOrder, out go);
            healMotes.Add(new HealMote { go = go, sr = sr, x = e.x, y = e.y + .8f, target = e.amount });
            GameSfx.Play("sprout_heal", .5f, .25f);
        }

        void UpdateThornFx(float dt)
        {
            var warning = Sprites.Frames(ThornFx + "warning"); var eruption = Sprites.Frames(ThornFx + "eruption");
            for (int i = bramblesShown.Count - 1; i >= 0; i--)
            {
                var b = bramblesShown[i];
                b.age += dt;
                if (b.age >= Tuning.BossEruptWarn + EruptShow) { Object.Destroy(b.go); bramblesShown.RemoveAt(i); continue; }
                if (b.age < Tuning.BossEruptWarn)
                {
                    // The mark brightens through its frames, and pulses faster as the thorns come.
                    float t = b.age / Tuning.BossEruptWarn;
                    b.sr.sortingOrder = ThornGroundOrder;
                    StandOn(b.sr, Frame(warning, t), b.x, b.y - .05f, Tuning.BossEruptRadius * 2f / 2.5f);
                    float pulse = .75f + .25f * Mathf.Sin(b.age * (10f + 20f * t));
                    b.sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(.55f + t * .45f) * pulse);
                }
                else
                {
                    if (!b.erupted) { b.erupted = true; GameSfx.Play("thorn_erupt"); }
                    float t = (b.age - Tuning.BossEruptWarn) / EruptShow;
                    b.sr.sortingOrder = ThornFrontOrder;
                    StandOn(b.sr, Frame(eruption, t), b.x, b.y - .05f);
                    b.sr.color = new Color(1f, 1f, 1f, t > .8f ? (1f - t) / .2f : 1f);
                }
            }

            var wallFrames = Sprites.Frames(ThornFx + "wall");
            MonsterState mother = null;
            if (thornWalls.Count > 0 && shownMap >= 0)
                foreach (var m in world.maps[shownMap].monsters) if (m.Def.boss) { mother = m; break; }
            for (int i = thornWalls.Count - 1; i >= 0; i--)
            {
                var wall = thornWalls[i];
                wall.age += dt;
                if (wall.falling >= 0f)
                {
                    wall.falling += dt;
                    if (wall.falling >= WallFall || wallFrames.Length == 0) { Object.Destroy(wall.go); thornWalls.RemoveAt(i); continue; }
                }
                else if (mother != null)
                {
                    // The same creep as the sim's (World.UpdateRooted): inward until BossWallGap apart around her.
                    float stop = mother.x + wall.side * Tuning.BossWallGap * .5f;
                    wall.x = wall.side < 0 ? Mathf.Min(wall.x + Tuning.BossWallSpeed * dt, Mathf.Max(wall.x, stop))
                                           : Mathf.Max(wall.x - Tuning.BossWallSpeed * dt, Mathf.Min(wall.x, stop));
                }
                if (wallFrames.Length == 0) continue;
                // Frames: sprouting, half grown, full, crumbling.
                Sprite frame = wall.falling >= 0f ? wallFrames[wallFrames.Length - 1]
                             : wallFrames[Mathf.Min(wallFrames.Length - 2, Mathf.FloorToInt(wall.age / WallGrow * (wallFrames.Length - 1)))];
                float half = frame.bounds.size.x * WallScale * .5f;
                StandOn(wall.sr, frame, wall.x + wall.side * (half - .4f), wall.y, WallScale);    // its inner face is where the thorns begin
                wall.sr.color = new Color(1f, 1f, 1f, wall.falling >= 0f ? 1f - wall.falling / WallFall : 1f);
            }

            var moteFrames = Sprites.Frames(ThornFx + "motes");
            for (int i = healMotes.Count - 1; i >= 0; i--)
            {
                var mote = healMotes[i];
                mote.age += dt;
                float t = mote.age / MoteFlight;
                if (t >= 1f || moteFrames.Length == 0) { Object.Destroy(mote.go); healMotes.RemoveAt(i); continue; }
                float tx = mote.x, ty = mote.y + 1.5f;
                if (shownMap >= 0)
                    foreach (var m in world.maps[shownMap].monsters) if (m.id == mote.target) { tx = m.x; ty = m.y + m.Def.height * .55f; break; }
                float ease = t * t * (3f - 2f * t);
                mote.sr.sprite = Frame(moteFrames, t);
                mote.sr.flipX = tx < mote.x;
                mote.go.transform.localScale = new Vector3(.55f, .55f, 1f);
                mote.go.transform.position = new Vector3(Mathf.Lerp(mote.x, tx, ease), Mathf.Lerp(mote.y, ty, ease) + Mathf.Sin(t * Mathf.PI) * .5f, 0f);
                mote.sr.color = new Color(1f, 1f, 1f, t > .8f ? (1f - t) / .2f : 1f);
            }
        }

        static Sprite Frame(Sprite[] frames, float t)
        {
            return frames.Length == 0 ? null : frames[Mathf.Clamp(Mathf.FloorToInt(t * frames.Length), 0, frames.Length - 1)];
        }

        void ClearThornFx()
        {
            foreach (var b in bramblesShown) Object.Destroy(b.go);
            foreach (var w in thornWalls) Object.Destroy(w.go);
            foreach (var m in healMotes) Object.Destroy(m.go);
            bramblesShown.Clear(); thornWalls.Clear(); healMotes.Clear();
        }
    }
}
