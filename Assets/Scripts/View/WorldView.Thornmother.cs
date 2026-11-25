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
        // A boss's own effect art (Art/Effects/<boss key>/{warning,eruption,wall,motes}) if the map's boss has any,
        // otherwise Thornmother's: the fights share their mechanics, so the frames mean the same things.
        const string ThornmotherFx = "Art/Effects/thornmother/";
        string bossFx = ThornmotherFx; int bossFxMap = -2;
        string ThornFx
        {
            get
            {
                if (bossFxMap == shownMap) return bossFx;
                bossFxMap = shownMap; bossFx = ThornmotherFx;
                if (shownMap >= 0)
                    foreach (var point in Maps.All[shownMap].spawns)
                    {
                        var d = Monsters.All[point.monster];
                        if (d.boss && Sprites.Frames("Art/Effects/" + d.key + "/warning").Length > 0) { bossFx = "Art/Effects/" + d.key + "/"; break; }
                    }
                return bossFx;
            }
        }
        void PlayThornSound(string cue, float volume = 1f, float cooldown = 0f)
        {
            if (!CaptainEffects) GameSfx.Play(cue, volume, cooldown);
        }

        bool CaptainEffects => ThornFx == "Art/Effects/captain_marrow/";

        const int ThornGroundOrder = MonsterOrder + 1, ThornFrontOrder = HeroOrder + 1;
        const float EruptShow = .55f, WallGrow = .9f, WallFall = .45f, WallScale = 1.5f, MoteFlight = .7f;

        // The frame for a monster's special pose, or null when it is just standing or walking.
        Sprite PoseFrame(MonsterView v, MonsterState m, float dt)
        {
            if (m.pose != v.pose)
            {
                v.pose = m.pose; v.poseClock = 0f;
                // Her calls and her rooting are heard as the pose begins (the slam, marks and walls sound from their events).
                if (m.pose == 3 || m.pose == 4) PlayThornSound("thorn_summon", .8f);
                else if (m.pose == 5) PlayThornSound("thorn_root");
            }
            else v.poseClock += dt;
            string anim; float span = 0f; int from = 0, count = 4; float loopRate = 0f;
            switch (m.pose)
            {
                case 1: anim = "slam"; count = 2; span = Tuning.BossSlamWindup * (World.Enraged(m) ? Tuning.BossEnrageWindup : 1f); break;              // rearing up
                case 2: anim = "slam"; from = 2; count = 2; span = Tuning.BossStrikeVisual; break;  // the blow, then recovering
                case 3: anim = "summon"; span = Tuning.BossSummonWindup; break;
                case 4: anim = "summon"; span = Tuning.BossEruptWindup * (World.Enraged(m) ? Tuning.BossEnrageWindup : 1f); break;
                case 5: anim = "root"; span = Tuning.BossRootTime; break;
                case 6: anim = "rooted"; loopRate = 5f; break;
                case 7: anim = "break"; span = Tuning.BossRootTime; break;
                case 8: anim = "heal"; loopRate = 8f; break;
                case 9: anim = "attack"; span = Tuning.MonsterStrikeShow; break;      // any monster with attack frames
                case 10: anim = "attack"; count = 2; span = Tuning.SpitWindup; break;            // a spitter gulping
                case 11: anim = "attack"; from = 2; count = 2; span = Tuning.SpitRecover; break;  // mouth open, then settling
                case 12: anim = "idle"; count = 1; span = 1f; break;                             // frozen: held on one frame
                default: return null;
            }
            var frames = MonsterFrames(v, anim);
            if (frames.Length == 0) return null;
            // A slam clip is the wind-up then the blow, half and half, however many frames it has (4 or 8).
            if (m.pose == 1) { from = 0; count = Mathf.Max(1, frames.Length / 2); }
            else if (m.pose == 2) { from = frames.Length / 2; count = Mathf.Max(1, frames.Length - from); }
            else if (from == 0 && count == 4) count = frames.Length;      // whole clips play all their frames
            int index = loopRate > 0f ? Mathf.FloorToInt(v.poseClock * loopRate) % frames.Length
                                      : from + Mathf.Min(count - 1, Mathf.FloorToInt(v.poseClock / Mathf.Max(.01f, span) * count));
            return frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }

        sealed class Bramble { public GameObject go; public SpriteRenderer sr; public float x, y, age; public bool erupted; }
        // A wall is a thicket: its leading clump in front of the heroes, and darker clumps behind them filling the
        // ground it has already taken, out to where it started.
        sealed class ThornWall
        {
            public GameObject go; public SpriteRenderer sr; public float x, x0, y, age, falling = -1f; public int side;
            public readonly List<SpriteRenderer> fill = new List<SpriteRenderer>(); public readonly List<float> fillAge = new List<float>();
        }
        const float WallStep = 1.7f; const int WallFillMax = 14;
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
            PlayThornSound("thorn_warn", .8f);
        }

        void SpawnThornWalls(SimEvent e)
        {
            EndThornWalls();
            PlayThornSound("thorn_walls");
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject go;
                var sr = ThornSprite("Thorn Wall", ThornFrontOrder, out go);
                sr.flipX = side > 0;
                float at = side < 0 ? e.x : e.amount / 100f;
                thornWalls.Add(new ThornWall { go = go, sr = sr, side = side, x = at, x0 = at, y = e.y });
            }
        }

        void EndThornWalls()
        {
            bool any = false;
            foreach (var wall in thornWalls) if (wall.falling < 0f) { wall.falling = 0f; any = true; }
            if (any) PlayThornSound("thorn_break");
        }

        void SpawnHealMote(SimEvent e)
        {
            GameObject go;
            var sr = ThornSprite("Healing Mote", SlashOrder, out go);
            healMotes.Add(new HealMote { go = go, sr = sr, x = e.x, y = e.y + .8f, target = e.amount });
            PlayThornSound("sprout_heal", .5f, .25f);
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
                    if (!b.erupted) { b.erupted = true; PlayThornSound("thorn_erupt"); }
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
                    if (wall.falling >= WallFall || wallFrames.Length == 0) { DestroyWall(wall); thornWalls.RemoveAt(i); continue; }
                }
                else if (mother != null)
                {
                    // The same creep as the sim's (World.UpdateRooted): inward until BossWallGap apart around her.
                    float stop = mother.x + wall.side * Tuning.BossWallGap * .5f;
                    wall.x = wall.side < 0 ? Mathf.Min(wall.x + Tuning.BossWallSpeed * dt, Mathf.Max(wall.x, stop))
                                           : Mathf.Max(wall.x - Tuning.BossWallSpeed * dt, Mathf.Min(wall.x, stop));
                }
                if (wallFrames.Length == 0) continue;
                // The full-grown clump pushes up out of the ground (no frame-by-frame popping); the last frame is it crumbling.
                Sprite frame = wall.falling >= 0f ? wallFrames[wallFrames.Length - 1] : wallFrames[Mathf.Max(0, wallFrames.Length - 2)];
                float half = frame.bounds.size.x * WallScale * .5f;
                float lead = wall.x + wall.side * (half - .4f);     // its inner face is where the thorns begin
                float gone = wall.falling >= 0f ? 1f - wall.falling / WallFall : 1f;
                Rise(wall.sr, frame, lead, wall.y, WallScale, wall.age, Color.white, gone);
                // Clumps behind it, one more each WallStep it has crept, the newest sprouting at the far end.
                int wanted = Mathf.Min(WallFillMax, 1 + Mathf.FloorToInt(Mathf.Abs(wall.x - wall.x0) / WallStep));
                while (wall.falling < 0f && wall.fill.Count < wanted)
                {
                    GameObject go;
                    var sr = ThornSprite("Thorn Thicket", ThornGroundOrder, out go);
                    sr.flipX = (wall.fill.Count % 2 == 0) != wall.sr.flipX;
                    wall.fill.Add(sr); wall.fillAge.Add(0f);
                }
                for (int k = 0; k < wall.fill.Count; k++)
                {
                    wall.fillAge[k] += dt;
                    float vary = .84f + .16f * ((k * 37 + 11) % 5) / 4f, shade = .58f + .1f * ((k * 13) % 3) / 2f;
                    Rise(wall.fill[k], frame, lead + wall.side * (k + 1) * WallStep, wall.y, WallScale * vary, wall.fillAge[k], new Color(shade, shade * 1.04f, shade * .95f), gone);
                }
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

        // Stands a clump at (x, floor), pushing up out of the ground over WallGrow: thin and low at first, a small
        // overshoot as it reaches full height, its roots a little under the surface so it never floats.
        static void Rise(SpriteRenderer sr, Sprite sprite, float x, float floor, float scale, float age, Color tint, float alpha)
        {
            float t = Mathf.Clamp01(age / WallGrow), ease = 1f - (1f - t) * (1f - t) * (1f - t);
            float sy = scale * (ease + .12f * Mathf.Sin(t * Mathf.PI) * t), sx = scale * Mathf.Lerp(.72f, 1f, ease);
            sr.sprite = sprite;
            sr.transform.localScale = new Vector3(sx, Mathf.Max(.02f, sy), 1f);
            sr.transform.position = new Vector3(x - sprite.bounds.center.x * sx * (sr.flipX ? -1f : 1f), floor - .1f - sprite.bounds.min.y * sy, 0f);
            sr.color = new Color(tint.r, tint.g, tint.b, alpha * Mathf.Clamp01(age / .18f));
        }

        static void DestroyWall(ThornWall wall)
        {
            Object.Destroy(wall.go);
            foreach (var sr in wall.fill) Object.Destroy(sr.gameObject);
            wall.fill.Clear(); wall.fillAge.Clear();
        }

        void ClearThornFx()
        {
            foreach (var b in bramblesShown) Object.Destroy(b.go);
            foreach (var w in thornWalls) DestroyWall(w);
            foreach (var m in healMotes) Object.Destroy(m.go);
            bramblesShown.Clear(); thornWalls.Clear(); healMotes.Clear();
            foreach (var s in spitsShown) Object.Destroy(s.go);
            spitsShown.Clear();
        }

        // A spitter's poison (SimEventKind.PoisonSpit): the glob arcs from its mouth to the landing spot, then a puddle
        // steams there until it dries. Timed from Tuning, like the sim.
        const string SpitFx = "Art/Effects/marsh_toad/";
        const float SpitMouth = .55f, SpitArc = 1.3f, SpitScale = 1.5f, PuddleFade = .6f;
        sealed class Spit { public GameObject go; public SpriteRenderer sr; public float x0, x1, y, age; }
        readonly List<Spit> spitsShown = new List<Spit>();

        void SpawnSpit(SimEvent e)
        {
            GameObject go;
            var sr = ThornSprite("Poison Spit", SlashOrder, out go);
            spitsShown.Add(new Spit { go = go, sr = sr, x0 = e.x, x1 = e.amount / 100f, y = e.y });
        }

        void UpdateSpitFx(float dt)
        {
            var glob = Sprites.Art(SpitFx + "spit"); var puddle = Sprites.Art(SpitFx + "puddle");
            for (int i = spitsShown.Count - 1; i >= 0; i--)
            {
                var s = spitsShown[i];
                s.age += dt;
                float pooled = s.age - Tuning.SpitFlight;
                if (pooled >= Tuning.PuddleTime) { Object.Destroy(s.go); spitsShown.RemoveAt(i); continue; }
                if (pooled < 0f)
                {
                    float t = s.age / Tuning.SpitFlight;
                    float x = Mathf.Lerp(s.x0, s.x1, t), y = s.y + SpitMouth * (1f - t) + SpitArc * 4f * t * (1f - t);
                    s.sr.sprite = glob;
                    s.sr.flipX = s.x1 < s.x0;
                    s.sr.color = Color.white;
                    if (glob == null) continue;
                    // Nose along its path: up off the lip, down into the mud.
                    float rise = (SpitArc * 4f * (1f - 2f * t) - SpitMouth) / Mathf.Max(.5f, Mathf.Abs(s.x1 - s.x0));
                    s.sr.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan(rise) * Mathf.Rad2Deg * (s.sr.flipX ? -1f : 1f));
                    s.sr.transform.localScale = new Vector3(SpitScale, SpitScale, 1f);
                    s.sr.transform.position = new Vector3(x, y, 0f) - s.sr.transform.rotation * (new Vector3(glob.bounds.center.x * (s.sr.flipX ? -1f : 1f), glob.bounds.center.y, 0f) * SpitScale);
                }
                else
                {
                    s.sr.flipX = false;
                    s.sr.transform.rotation = Quaternion.identity;
                    s.sr.sortingOrder = ThornGroundOrder;
                    // Splashes out wide, then steams; thins away as it dries.
                    float grow = Mathf.Clamp01(pooled / .15f);
                    float scale = puddle != null ? Tuning.PuddleRadius * 2f / puddle.bounds.size.x * Mathf.Lerp(.4f, 1f, grow) : 1f;
                    StandOn(s.sr, puddle, s.x1, s.y - .06f, scale);
                    float fade = Mathf.Clamp01((Tuning.PuddleTime - pooled) / PuddleFade);
                    s.sr.color = new Color(1f, 1f, 1f, fade * (.88f + .12f * Mathf.Sin(pooled * 7f)));
                }
            }
        }
    }
}
