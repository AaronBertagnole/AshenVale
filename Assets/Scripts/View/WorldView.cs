using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AshenVale
{
    // Draws the map the local player is on: scenery, heroes, monsters, drops, arrows, portals and effects.
    // It only reads the World (plus its event list, which it clears); it never changes game state.
    public sealed partial class WorldView
    {
        sealed partial class HeroView { public GameObject go; public SpriteRenderer sr; public string anim = ""; public float clock, lastHp = -1; public int attackSerial = -1; public float climbOffset, climbXOffset; public Vector3 climbScale; public bool fittedClimb; public readonly PronePosePlayer prone = new PronePosePlayer(); }
        sealed class SlashFx { public GameObject obj; public SpriteRenderer sr; public float life, duration, x, y; public int direction; public bool ring; public int dragon; }

        // A number that pops up where something was hurt or healed and drifts upward (GameHud draws them).
        public sealed class FloatingNumber
        {
            public float x, y, age, life;
            public string text;
            public Color color;
            public bool big;
        }
        public readonly List<FloatingNumber> numbers = new List<FloatingNumber>();
        public static readonly Color DamageColor = new Color(1f, .96f, .8f), HurtColor = new Color(1f, .35f, .3f), HealColor = new Color(.55f, 1f, .55f);

        // A monster on screen: `go` stands at its feet and is mirrored to face its way; `body` is what squashes and bobs.
        sealed class MonsterView
        {
            public GameObject go;
            public Transform body;
            public SpriteRenderer art;          // set when the monster has art (Resources/Art/Monsters/<key>/)
            public SpriteRenderer[] parts;      // every renderer, for the hurt flash and the fade when it dies
            public Color[] colors;
            public MonsterDef def;
            public float clock, hurt, moveTime, lastHp, lastX, dying, lastStrike;
            public Transform crown;             // bosses: the thorn crown, which flares while it rears up
        }

        public const float ViewHalfHeight = 5.4f;   // camera orthographic size: the view is 10.8 units tall
        const float HeroScale = 1.65f;
        const float DieTime = .45f;
        public const int MonsterOrder = 7, DropOrder = 9, HeroOrder = 10, SlashOrder = 12, ArrowOrder = 13;

        readonly World world;
        readonly Camera camera;
        readonly Transform cam;
        readonly MapVisual[] visuals;
        int shownMap = -1;
        bool snapCamera = true;

        readonly Dictionary<int, HeroView> heroViews = new Dictionary<int, HeroView>();
        readonly Dictionary<int, MonsterView> monsterViews = new Dictionary<int, MonsterView>();
        readonly List<MonsterView> dying = new List<MonsterView>();
        readonly Dictionary<int, GameObject> dropViews = new Dictionary<int, GameObject>();
        // Drops pop out of the monster when they appear: each remembers when it was first seen and where it burst from.
        sealed class DropPop { public float start, fromX; public Vector3 rest; }
        readonly Dictionary<int, DropPop> dropPops = new Dictionary<int, DropPop>();
        bool dropsSynced;                   // false right after a map change, so what is already lying there doesn't pop
        const float PopTime = .5f, PopHeight = .9f;
        readonly Dictionary<int, GameObject> shotViews = new Dictionary<int, GameObject>();
        readonly Dictionary<int, GameObject> portalViews = new Dictionary<int, GameObject>();
        readonly List<SlashFx> slashes = new List<SlashFx>();
        readonly HashSet<int> seen = new HashSet<int>();
        readonly List<int> stale = new List<int>();

        public WorldView(World world)
        {
            this.world = world;
            camera = Camera.main;
            if (camera == null)
            {
                var c = new GameObject("Main Camera");
                camera = c.AddComponent<Camera>();
                c.tag = "MainCamera";
            }
            camera.orthographic = true;
            camera.orthographicSize = ViewHalfHeight;
            camera.backgroundColor = new Color(.07f, .09f, .16f);
            cam = camera.transform;
            cam.position = new Vector3(0, 0, -10);

            visuals = new MapVisual[Maps.All.Length];
            for (int i = 0; i < visuals.Length; i++)
            {
                visuals[i] = MapScenery.Build(Maps.All[i]);
                visuals[i].root.SetActive(false);
            }
        }

        // Where the camera centre may go on a map so the view never shows past its edges.
        // halfWidth is half the view's width (ViewHalfHeight * aspect).
        public static void CameraRange(MapDef def, float halfWidth, out float minX, out float maxX, out float minY, out float maxY)
        {
            minX = halfWidth; maxX = def.columns - halfWidth;
            if (minX > maxX) minX = maxX = def.columns * .5f;
            // A strip of ground under your feet at the lowest; painted scenes keep it thin to show more of the picture.
            minY = def.Bottom - (string.IsNullOrEmpty(def.scene) ? 1.3f : .9f) + ViewHalfHeight;
            maxY = def.Top + 1f - ViewHalfHeight;
            if (maxY < minY) maxY = minY;
        }

        // Removes every hero, monster, drop and effect (new run, back to menu).
        public void Reset()
        {
            ClearRimeFx();
            ClearLandingLeaves();
            foreach (var v in heroViews.Values) Object.Destroy(v.go);
            heroViews.Clear();
            ClearMonsters();
            DestroyAll(dropViews); DestroyAll(shotViews); DestroyAll(portalViews);
            foreach (var fx in slashes) Object.Destroy(fx.obj);
            slashes.Clear();
            snapCamera = true;
        }

        // Called once per frame. alpha (0..1) is how far we are between the last tick and the next one.
        public void Sync(int localId, float frameDt, float alpha)
        {
            var local = world.GetPlayer(localId);
            int map = local != null ? local.map : Maps.Emberfields;
            if (map != shownMap) ShowMap(map);

            ReadEvents(localId);
            SyncHeroes(localId, frameDt, alpha);
            SyncMonsters(frameDt, alpha);
            SyncDrops(localId);
            SyncShots(alpha);
            SyncPortals();
            UpdateSlashes(frameDt);
            UpdateLandingLeaves(frameDt);
            UpdateCamera(local, frameDt, alpha);
        }

        void ShowMap(int map)
        {
            ClearRimeFx();
            ClearLandingLeaves();
            dropsSynced = false;
            dropPops.Clear();
            for (int i = 0; i < visuals.Length; i++) visuals[i].root.SetActive(i == map);
            shownMap = map;
            ClearMonsters();
            foreach (var fx in slashes) Object.Destroy(fx.obj);
            slashes.Clear();
            snapCamera = true;
        }

        void ReadEvents(int localId)
        {
            foreach (var e in world.events)
            {
                if (e.map != shownMap) continue;
                if(e.kind==SimEventKind.RimeBurst || e.kind==SimEventKind.RimeStorm || e.kind==SimEventKind.RimeTeleport) {SpawnRimeFx(e);continue;}
                if (e.kind == SimEventKind.EmbermarksPickedUp) continue;   // the bottom-left pickup note covers it (owner: no floating text)
                if (e.kind == SimEventKind.Slash) SpawnSlash(e);
                else if (e.kind == SimEventKind.DragonThrust || e.kind == SimEventKind.DragonFang || e.kind == SimEventKind.DragonFissure || e.kind == SimEventKind.DragonSweep) SpawnDragonFx(e);
                else if (e.kind == SimEventKind.BossSlam) SpawnSlamRing(e);
            }
            world.events.Clear();
            for (int i = numbers.Count - 1; i >= 0; i--) { numbers[i].age += Time.deltaTime; if (numbers[i].age >= numbers[i].life) numbers.RemoveAt(i); }
        }

        // ---------------------------------------------------------------- heroes

        void SyncHeroes(int localId, float frameDt, float alpha)
        {
            seen.Clear();
            foreach (var p in world.players)
            {
                if (p.map != shownMap) continue;
                seen.Add(p.id);
                HeroView v;
                if (!heroViews.TryGetValue(p.id, out v))
                {
                    v = new HeroView { go = new GameObject("Hero " + p.id + " (" + p.Hero.name + ")") };
                    v.sr = v.go.AddComponent<SpriteRenderer>();
                    heroViews[p.id] = v;
                }
                v.sr.sortingOrder = p.id == localId ? HeroOrder + 1 : HeroOrder;
                if (v.lastHp >= 0 && p.hp != v.lastHp && !p.dead)
                    Number(p.x, p.y + 2.1f, p.hp < v.lastHp ? "-" + (v.lastHp - p.hp) : "+" + (p.hp - v.lastHp), p.hp < v.lastHp ? HurtColor : HealColor, false);
                v.lastHp = p.hp;
                TrackRimeLanding(p, v, localId, frameDt, alpha);
                Animate(p, v, frameDt, alpha);
                float x = Mathf.Lerp(p.px, p.x, alpha), y = Mathf.Lerp(p.py, p.y, alpha);
                v.go.transform.localScale = v.fittedClimb ? v.climbScale : new Vector3(HeroScale, HeroScale, 1f);
                float heroOffset = HeroPolishArt.Uses(p.Hero.folder) ? (114f/128f-.5f)*HeroScale : .48f; // Prone art shares the idle ground-contact baseline.
                if (!p.climbing && !p.dead && PaintedMapFraming.UsesOwnGround(Maps.All[p.map]))
                    heroOffset = PaintedSpriteContact.HeroOffset(p.Hero.folder, v.sr.sprite, v.go.transform.localScale.y, heroOffset);
                Vector3 basePosition = new Vector3(x + v.climbXOffset, y + heroOffset + v.climbOffset, 0);
                Quaternion rotation = Quaternion.identity;
                if (RangerIsFlipping(p))
                {
                    float progress = Mathf.Clamp01(1f - p.rangerFlipTime / PlayerState.RangerFlipDuration);
                    rotation = Quaternion.Euler(0, 0, -360f * p.rangerFlipFacing * progress);
                }
                v.go.transform.rotation = rotation; // reset every frame, including interruptions/map changes
                // Rotate about the sprite's visual center, even if its imported pivot is at its feet.
                Vector3 center = v.sr.sprite != null ? v.sr.sprite.bounds.center : Vector3.zero;
                center = Vector3.Scale(center, v.go.transform.localScale);
                v.go.transform.position = basePosition + center - rotation * center;
            }
            stale.Clear();
            foreach (var kv in heroViews) if (!seen.Contains(kv.Key)) stale.Add(kv.Key);
            foreach (var id in stale) { Object.Destroy(heroViews[id].go); heroViews.Remove(id); }
        }

        readonly Dictionary<int, SpriteRenderer[]> climbLadders = new Dictionary<int, SpriteRenderer[]>();
        // Palm centers in exported128px frames, measured from top-left: high then low hand.
        static readonly Vector2[] KnightClimbHands = { new Vector2(38.72f, 23.93f), new Vector2(88.99f, 57.03f), new Vector2(89.23f, 23.93f), new Vector2(39.97f, 57.03f) };
        static readonly Vector2[] RangerClimbHands = { new Vector2(38.65f, 20.52f), new Vector2(90.49f, 52.61f), new Vector2(89.21f, 20.52f), new Vector2(38.39f, 52.61f) };

        int FitClimbContacts(PlayerState p, HeroView v, Sprite[] frames, float y)
        {
            // Locate the drawn ladder, not a guessed ground-relative phase.
            SpriteRenderer[] renderers;
            if (!climbLadders.TryGetValue(p.map, out renderers))
            {
                renderers = visuals[p.map].root.GetComponentsInChildren<SpriteRenderer>(true);
                climbLadders[p.map] = renderers;
            }
            SpriteRenderer ladder = null;
            foreach (var sr in renderers)
                if (sr != null && sr.gameObject.name == "Ladder" && sr.sprite != null &&
                    Mathf.Abs(sr.bounds.center.x - p.x) < .8f &&
                    y >= sr.bounds.min.y - .6f && y <= sr.bounds.max.y + .6f)
                { ladder = sr; break; }
            float origin = Tuning.Ground + .24f, spacing = .38f, left = p.x - .27f, right = p.x + .27f;
            if (ladder != null)
            {
                // Supplied72x84tile: rail centers at8/64px, rung centers22/64px from bottom.
                float unitX = ladder.bounds.size.x / 72f;
                float unitY = ladder.transform.lossyScale.y / ladder.sprite.pixelsPerUnit * ladder.sprite.rect.height / 84f;
                left = ladder.bounds.min.x + 8f * unitX;
                right = ladder.bounds.min.x + 64f * unitX;
                spacing = 42f * unitY;
                origin = ladder.bounds.min.y + 22f * unitY;
            }
            else
            {
                foreach (var l in world.maps[p.map].def.ladders)
                    if (Mathf.Abs(l.x - p.x) < .8f)
                    { origin = l.bottom + .24f; left = l.x - .27f; right = l.x + .27f; break; }
            }
            spacing = Mathf.Max(.01f, spacing);
            int step = Mathf.FloorToInt((y - origin) / spacing);
            int pose = (step % 2 + 2) % 2;
            int index = Mathf.Min(pose, frames.Length - 1);
            Sprite sprite = frames[index];
            var hands = p.Hero.dragonKnight ? KnightClimbHands : RangerClimbHands;
            Vector2 high = hands[pose * 2], low = hands[pose * 2 + 1];
            // Contacts are expressed in128px exports; account for the actual Sprite rect/pivot/PPU.
            high = new Vector2(high.x / 128f * sprite.rect.width, (1f - high.y / 128f) * sprite.rect.height);
            low = new Vector2(low.x / 128f * sprite.rect.width, (1f - low.y / 128f) * sprite.rect.height);
            Vector2 localHigh = (high - sprite.pivot) / sprite.pixelsPerUnit;
            Vector2 localLow = (low - sprite.pivot) / sprite.pixelsPerUnit;
            float sx = (right - left) / Mathf.Max(.01f, Mathf.Abs(localHigh.x - localLow.x));
            float sy = spacing / Mathf.Max(.01f, Mathf.Abs(localHigh.y - localLow.y));
            float targetX = pose == 0 ? left : right;
            float naturalHandY = y + .48f + localHigh.y * sy;
            float targetY = origin + Mathf.Round((naturalHandY - origin) / spacing) * spacing;
            sx = sy;   // one scale both ways: a fitted width squashed the hero ~13%; the hands still close on the rails
            v.climbScale = new Vector3(sx, sy, 1f);
            v.climbXOffset = targetX - localHigh.x * sx - p.x;
            v.climbOffset = targetY - localHigh.y * sy - y - .48f;
            v.fittedClimb = true;
            return index;
        }

        static bool RangerIsFlipping(PlayerState p)
        {
            return p.Hero.folder == "ElvenRanger" && p.rangerFlipTime > 0 &&
                !p.grounded && !p.climbing && !p.dead && p.hurtVisual <= 0 && p.attackVisual <= 0;
        }

        static string ChooseAnimation(PlayerState p)
        {
            var h = p.Hero;
            if (p.dead) return "death";
            // A swing in progress keeps playing when you are hit: the hurt pose waits until it ends.
            if (p.attackVisual > 0 && !p.climbing)
            {
                // comboStep is bumped before the swing, so the knight's first basic attack (step 1) is the thrust.
                bool useA = h.dragonKnight ? p.comboStep % 2 == 1 : p.comboStep % 2 == 0;
                return p.lastWasSkill ? h.skillAnim : (useA ? h.basicAnimA : h.basicAnimB);
            }
            if (p.hurtVisual > 0) return "hurt";
            if (p.climbing) return "climb";
            if (RangerIsFlipping(p)) return "jump";
            if (h.dragonKnight && p.leapActive) return "leap";
            if (h.dragonKnight && p.landVisual > 0 && p.grounded && Mathf.Abs(p.vx) < .2f) return "land";

            if (p.crouching) return RimecallerRules.Is(p.Hero)?"prone":"idle";
            if (!p.grounded) return p.vy > 0 ? "jump" : "fall";
            return Mathf.Abs(p.vx) > .2f ? "run" : "idle";
        }

        void Animate(PlayerState p, HeroView v, float frameDt, float alpha)
        {
            var h = p.Hero;
            string clip = ChooseAnimation(p);
            if (v.landingTime > 0f && (clip == "idle" || clip == "run") &&
                Sprites.Hero(h.folder + "/land").Length > 0)
                clip = "land";
            if (v.prone.TryDraw(p, clip, v.sr, frameDt))
            {
                v.anim = "prone"; v.clock = 0f;
                v.climbOffset = v.climbXOffset = 0f; v.fittedClimb = false;
                return;
            }
            if ((p.Hero.dragonKnight || RimecallerRules.Is(p.Hero)) && p.attackVisual > 0 && p.attackSerial != v.attackSerial)
            { v.attackSerial = p.attackSerial; v.clock = 0; }
            bool enteredClip = clip != v.anim;
            float climbY = Mathf.Lerp(p.py, p.y, alpha);
            if (enteredClip) { v.anim = clip; v.clock = 0; }
            v.clock += frameDt;
            v.climbOffset = v.climbXOffset = 0; v.fittedClimb = false;
            Sprite[] frames = Sprites.Hero(h.folder + "/" + clip);
            if (enteredClip && clip == "climb" && frames.Length < 2)
                Debug.LogWarning("Climb needs multiple loaded frames at Resources/Heroes/" + h.folder +
                    "/climb/00.png, 01.png, 02.png, 03.png. Loaded: " + frames.Length);
            float fps = clip == "idle" ? 1.5f : clip == "run" ? (HeroPolishArt.Uses(h.folder)?6:11) : clip == "death" ? 7 : 12;
            if (frames.Length > 0)
            {
                int index = Mathf.FloorToInt(v.clock * fps);
                if (clip == "climb")
                    index = HeroPolishArt.Uses(h.folder) ? FitPolishedClimb(p,v,frames,climbY) : FitClimbContacts(p, v, frames, climbY);
                if (h.dragonKnight && clip != "climb" && p.attackVisual > 0 && p.attackDuration > 0)
                {
                    // Reserve the first half of the clip for windup; contact at the windup boundary.
                    float elapsed = p.attackDuration - p.attackVisual;
                    float windup = p.lastWasSkill ? h.skillWindup : h.basicWindup;
                    float phase = elapsed < windup ? .5f * elapsed / windup :
                        .5f + .5f * (elapsed - windup) / Mathf.Max(.01f, p.attackDuration - windup);
                    index = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(phase * frames.Length));
                }
                if (h.dragonKnight && clip == "leap")
                {
                    // Six poses: ascent 0..2, descent 3..5. Hold the final descent on long falls.
                    float upSpeed = Mathf.Max(.01f, h.leapJumpSpeed);
                    index = p.vy > 0 ? Mathf.Clamp(Mathf.FloorToInt((1f - p.vy / upSpeed) * 3f), 0, 2)
                        : 3 + Mathf.Clamp(Mathf.FloorToInt(-p.vy / upSpeed * 3f), 0, 2);
                }
                if (h.dragonKnight && clip == "land")
                    index = Mathf.FloorToInt(Mathf.Clamp01(1f - p.landVisual / .24f) * frames.Length);
                if (clip == "land" && RimecallerRules.Is(h))
                    index = Mathf.FloorToInt(Mathf.Clamp01(1f - v.landingTime / RimeLandingSeconds) * frames.Length);
                // Fit every Ranger attack pose into the existing visual duration.
                // Simulation still releases arrows immediately, as before; this changes only drawing.
                if ((h.style == AttackStyle.Arrows || h.style == AttackStyle.Magic) && p.attackVisual > 0 && p.attackDuration > 0 &&
                    (clip == h.basicAnimA || clip == h.basicAnimB || clip == h.skillAnim))
                    index = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(
                        Mathf.Clamp01(1f - p.attackVisual / p.attackDuration) * frames.Length));
                // Hold the tucked airborne pose while the renderer performs the somersault.
                if (RangerIsFlipping(p)) index = frames.Length - 1;
                bool once = clip == "leap" || clip == "land" || clip == "death" || clip == "hurt" || clip == h.basicAnimA || clip == h.basicAnimB || clip == h.skillAnim;
                index = once ? Mathf.Min(index, frames.Length - 1) : (index % frames.Length + frames.Length) % frames.Length;
                v.sr.sprite = frames[index];
            }
            v.sr.flipX = clip != "climb" && p.facing < 0;
            v.sr.color = p.immunity > 0 && Mathf.FloorToInt(p.immunity * 16) % 2 == 0 ? new Color(1, .55f, .55f) : Color.white;
        }

        // ---------------------------------------------------------------- monsters, drops, arrows, portals

        void SyncMonsters(float frameDt, float alpha)
        {
            seen.Clear();
            foreach (var m in world.maps[shownMap].monsters)
            {
                seen.Add(m.id);
                MonsterView v;
                if (!monsterViews.TryGetValue(m.id, out v))
                {
                    v = BuildMonster(m.Def);
                    v.lastHp = m.hp;
                    v.lastX = m.x;
                    v.clock = (m.id % 17) * .37f;
                    monsterViews[m.id] = v;
                }
                var d = v.def;
                float x = Mathf.Lerp(m.px, m.x, alpha), y = Mathf.Lerp(m.py, m.y, alpha);
                v.clock += frameDt;
                if (Mathf.Abs(x - v.lastX) > .0005f) v.moveTime = .15f; else v.moveTime = Mathf.Max(0, v.moveTime - frameDt);
                v.lastX = x;
                if (m.hp < v.lastHp - .01f)
                {
                    v.hurt = .25f;
                    int dealt = Mathf.Max(1, Mathf.RoundToInt(v.lastHp - m.hp));
                    Number(x, y + d.hover + d.height + .2f, dealt.ToString(), DamageColor, dealt >= 60);
                }
                else v.hurt = Mathf.Max(0, v.hurt - frameDt);
                v.lastHp = m.hp;

                float bob = d.hover > 0 ? Mathf.Sin(v.clock * 3f) * .08f : 0f;
                v.go.transform.position = new Vector3(x, y + d.hover + bob, 0);
                v.go.transform.localScale = new Vector3(m.facing >= 0 ? 1f : -1f, 1f, 1f);
                bool moving = v.moveTime > 0;
                if (v.art != null)
                {
                    var frames = MonsterFrames(d, v.hurt > 0 ? "hurt" : moving ? "move" : "idle");
                    if (frames.Length > 0) v.art.sprite = frames[Mathf.FloorToInt(v.clock * 8f) % frames.Length];
                }
                else if (d.boss)
                {
                    // Rearing up: stretches tall and leans back; the strike: squats hard. Otherwise a slow heave.
                    float stretch = m.windup > 0 ? .18f : m.strike > 0 ? -.22f : Mathf.Sin(v.clock * 1.6f) * .03f;
                    v.body.localScale = new Vector3(1f - stretch * .6f, 1f + stretch, 1f);
                    v.body.localRotation = Quaternion.Euler(0, 0, m.windup > 0 ? -9f : 0f);
                    if (v.crown != null) v.crown.localScale = Vector3.one * (m.windup > 0 ? 1.25f + Mathf.Sin(v.clock * 20f) * .08f : 1f);
                }
                else
                {
                    float squash = moving ? Mathf.Sin(v.clock * 12f) * .06f : Mathf.Sin(v.clock * 2.5f) * .02f;
                    v.body.localScale = new Vector3(1f - squash, 1f + squash, 1f);
                }
                float flash = v.hurt / .25f;
                Color tint = new Color(1f, .45f, .4f, 1f);
                if (d.boss && m.windup > 0) { flash = Mathf.Max(flash, .55f + Mathf.Sin(v.clock * 18f) * .2f); tint = new Color(1f, .3f, .15f, 1f); }   // the tell
                for (int i = 0; i < v.parts.Length; i++)
                    v.parts[i].color = Color.Lerp(v.colors[i], new Color(tint.r, tint.g, tint.b, v.colors[i].a), flash * .7f);
            }

            stale.Clear();
            foreach (var kv in monsterViews) if (!seen.Contains(kv.Key)) stale.Add(kv.Key);
            foreach (var id in stale) { dying.Add(monsterViews[id]); monsterViews.Remove(id); }   // killed, or a chest moving on: fade out

            for (int i = dying.Count - 1; i >= 0; i--)
            {
                var v = dying[i];
                v.dying += frameDt;
                float t = Mathf.Clamp01(v.dying / DieTime);
                if (t >= 1f) { Object.Destroy(v.go); dying.RemoveAt(i); continue; }
                if (v.art != null)
                {
                    var frames = MonsterFrames(v.def, "die");
                    if (frames.Length > 0) v.art.sprite = frames[Mathf.Min(frames.Length - 1, Mathf.FloorToInt(t * frames.Length))];
                }
                else v.body.localScale = new Vector3(1f + t * .3f, 1f - t * .6f, 1f);
                for (int j = 0; j < v.parts.Length; j++)
                {
                    var c = v.colors[j];
                    v.parts[j].color = new Color(c.r, c.g, c.b, c.a * (1f - t));
                }
            }
        }

        static Sprite[] MonsterFrames(MonsterDef d, string anim) { return Sprites.Frames("Art/Monsters/" + d.key + "/" + anim); }

        // Real art if Resources/Art/Monsters/<key>/idle exists; otherwise a soft placeholder creature
        // (or a little chest for monsters that never move). Drawn facing right; the view mirrors it.
        static MonsterView BuildMonster(MonsterDef d)
        {
            var v = new MonsterView { def = d, go = new GameObject(d.name) };
            var root = v.go.transform;
            v.go.AddComponent<SortingGroup>().sortingOrder = MonsterOrder;   // its pieces draw together, in front of the scenery
            var idle = MonsterFrames(d, "idle");
            if (idle.Length > 0)
            {
                // The body pivot sits at the feet (squash and the hurt lean happen around it); the picture hangs
                // under it with its bottom edge on the floor, whatever pivot the importer gave it.
                v.body = new GameObject("Body").transform;
                v.body.SetParent(root, false);
                var art = Sprites.Picture("Art", idle[0], 0, v.body, 0, 0);
                art.transform.localPosition = new Vector3(0, -idle[0].bounds.min.y, 0);
                v.art = art.GetComponent<SpriteRenderer>();
            }
            else
            {
                v.body = new GameObject("Body").transform;
                v.body.SetParent(root, false);
                float w = d.width, h = d.height;
                Color dark = Color.Lerp(d.color, Color.black, .35f);
                if (d.speed <= 0f)
                {
                    Part("Chest outline", Sprites.Block("Chest outline", Color.Lerp(d.color, Color.black, .65f), w + .08f, h * .7f + .08f, 0), v.body, 0, h * .35f);
                    Part("Chest", Sprites.Block("Chest", d.color, w, h * .7f, 0), v.body, 0, h * .35f);
                    Part("Lid", Sprites.Block("Lid", dark, w * 1.06f, h * .32f, 1), v.body, 0, h * .78f);
                    Part("Latch", Sprites.Block("Latch", new Color(1f, .82f, .3f), w * .16f, h * .24f, 2), v.body, 0, h * .6f);
                }
                else if (d.boss)
                {
                    // A bramble giant: a knotted trunk, a thorn crown, root feet and two baleful eyes.
                    Color bark = d.color, darkBark = Color.Lerp(d.color, Color.black, .45f), leaf = new Color(.36f, .58f, .24f), thorn = new Color(.62f, .55f, .38f);
                    Part("Shadow", Sprites.Blob("Shadow", new Color(0, 0, 0, .3f), w * 1.1f, .25f, 0, null, 0, 0), root, 0, .04f);
                    for (int i = -1; i <= 1; i++) Part("Root", Sprites.Blob("Root", darkBark, w * .38f, .5f, 1, null, 0, 0), v.body, i * w * .3f, .2f);
                    Part("Trunk outline", Sprites.Blob("Trunk outline", Color.Lerp(d.color, Color.black, .7f), w * .72f + .12f, h * .78f + .12f, 1, null, 0, 0), v.body, 0, h * .42f);
                    Part("Trunk", Sprites.Blob("Trunk", bark, w * .72f, h * .78f, 2, null, 0, 0), v.body, 0, h * .42f);
                    Part("Knot", Sprites.Blob("Knot", darkBark, w * .22f, h * .12f, 3, null, 0, 0), v.body, -w * .12f, h * .3f);
                    Part("Knot", Sprites.Blob("Knot", darkBark, w * .16f, h * .09f, 3, null, 0, 0), v.body, w * .18f, h * .5f);
                    v.crown = new GameObject("Crown").transform;
                    v.crown.SetParent(v.body, false);
                    v.crown.localPosition = new Vector3(0, h * .8f, 0);
                    Part("Canopy", Sprites.Blob("Canopy", leaf, w * 1.15f, h * .36f, 2, null, 0, 0), v.crown, 0, 0);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = (i - 2) * .5f;
                        Part("Thorn", Sprites.Block("Thorn", thorn, .14f, .9f, 3), v.crown, Mathf.Sin(a) * w * .45f, Mathf.Cos(a) * h * .22f);
                        v.crown.GetChild(v.crown.childCount - 1).localRotation = Quaternion.Euler(0, 0, -a * 57f);
                    }
                    for (int i = 0; i < 2; i++)
                    {
                        float ex = w * (.12f + i * .18f), ey = h * .62f;
                        Part("Eye glow", Sprites.Blob("Eye glow", new Color(1f, .75f, .2f, .6f), .34f, .3f, 3, null, 0, 0), v.body, ex, ey);
                        Part("Eye", Sprites.Blob("Eye", new Color(1f, .9f, .4f), .16f, .14f, 4, null, 0, 0), v.body, ex, ey);
                    }
                }
                else
                {
                    Part("Shadow", Sprites.Blob("Shadow", new Color(0, 0, 0, .25f), w * 1.05f, .18f, 0, null, 0, 0), root, 0, -d.hover + .04f);
                    // A dark rim keeps placeholder monsters readable against busy painted backgrounds.
                    Part("Outline", Sprites.Blob("Outline", Color.Lerp(d.color, Color.black, .65f), w + .1f, h + .1f, 1, null, 0, 0), v.body, 0, h * .5f);
                    Part("Body", Sprites.Blob("Body", d.color, w, h, 2, null, 0, 0), v.body, 0, h * .5f);
                    Part("Shine", Sprites.Blob("Shine", new Color(1, 1, 1, .22f), w * .45f, h * .3f, 3, null, 0, 0), v.body, -w * .15f, h * .72f);
                    for (int i = 0; i < 2; i++)
                    {
                        float ex = w * (.12f + i * .2f), ey = h * .6f;
                        Part("Eye", Sprites.Blob("Eye", Color.white, .17f, .21f, 3, null, 0, 0), v.body, ex, ey);
                        Part("Pupil", Sprites.Blob("Pupil", new Color(.12f, .1f, .14f), .09f, .12f, 4, null, 0, 0), v.body, ex + .03f, ey - .01f);
                    }
                }
            }
            v.parts = v.go.GetComponentsInChildren<SpriteRenderer>();
            v.colors = new Color[v.parts.Length];
            for (int i = 0; i < v.parts.Length; i++) v.colors[i] = v.parts[i].color;
            return v;
        }

        static void Part(string name, GameObject piece, Transform parent, float x, float y)
        {
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = new Vector3(x, y, 0);
        }

        void ClearMonsters()
        {
            foreach (var v in monsterViews.Values) Object.Destroy(v.go);
            monsterViews.Clear();
            foreach (var v in dying) Object.Destroy(v.go);
            dying.Clear();
        }

        void SyncDrops(int localId)
        {
            seen.Clear();
            // New drops this frame burst from their shared centre (a monster's loot lands around where it fell).
            float burstX = 0; int fresh = 0;
            foreach (var d in world.maps[shownMap].drops)
                if (!dropViews.ContainsKey(d.id)) { burstX += d.x; fresh++; }
            if (fresh > 0) burstX /= fresh;
            foreach (var d in world.maps[shownMap].drops)
            {
                seen.Add(d.id);
                GameObject go;
                if (!dropViews.TryGetValue(d.id, out go))
                {
                    bool burst = dropsSynced;
                    bool part = d.kind == DropKind.Part && d.value >= 0 && d.value < Parts.All.Length;
                    string name = d.kind == DropKind.HpPotion ? "HP Potion" : d.kind == DropKind.MpPotion ? "MP Potion" : d.kind == DropKind.Item ? d.item.name
                                : part ? Parts.All[d.value].name : "Embermarks";
                    Color color = d.kind == DropKind.HpPotion ? new Color(1, .22f, .32f) : d.kind == DropKind.MpPotion ? new Color(.22f, .48f, 1)
                                : d.kind == DropKind.Item ? Loot.RarityColor(d.item.rarity) : part ? Parts.All[d.value].color : new Color(1, .81f, .18f);
                    // Parts with a painted icon show it (half a unit tall); other parts are small round blobs and
                    // everything else a little block, until they get icons too.
                    var icon = part ? Sprites.PartIcon(d.value)
                             : d.kind == DropKind.HpPotion ? Sprites.Art("Art/Items/hp_potion")
                             : d.kind == DropKind.MpPotion ? Sprites.Art("Art/Items/mp_potion")
                             : d.kind == DropKind.Gold ? Sprites.Art("Art/Currency/embermark") : null;
                    if (icon != null)
                    {
                        go = Sprites.Picture(name, icon, DropOrder, null, 0, 0);
                        float k = .5f / Mathf.Max(.01f, icon.bounds.size.y);
                        go.transform.localScale = new Vector3(k, k, 1f);
                    }
                    else go = part ? Sprites.Blob(name, color, .3f, .3f, DropOrder, null, 0, 0) : Sprites.Block(name, color, .28f, .34f, DropOrder);
                    dropViews[d.id] = go;
                    if (burst) dropPops[d.id] = new DropPop { start = Time.time, fromX = burstX, rest = go.transform.localScale };
                }
                float x = d.x, y = d.y + Mathf.Sin(Time.time * 4 + d.x) * .1f;
                DropPop pop;
                if (dropPops.TryGetValue(d.id, out pop))
                {
                    float t = (Time.time - pop.start) / PopTime;
                    if (t >= 1f) { dropPops.Remove(d.id); go.transform.localScale = pop.rest; }
                    else
                    {
                        // An arc from the burst point to its resting spot, landing with a little squash-and-settle.
                        float ease = 1f - (1f - t) * (1f - t);
                        x = Mathf.Lerp(pop.fromX, d.x, ease);
                        y = d.y + Mathf.Sin(t * Mathf.PI) * PopHeight * (.6f + .4f * Mathf.Abs(d.x - pop.fromX));
                        float grow = t < .25f ? .3f + .7f * (t / .25f) : 1f + .15f * Mathf.Sin((t - .25f) / .75f * Mathf.PI);
                        go.transform.localScale = new Vector3(pop.rest.x * grow, pop.rest.y * grow, 1f);
                    }
                }
                go.transform.position = new Vector3(x, y, 0);
                // Someone else's for now: drawn faint (the host decides; a client is told in the snapshot).
                int keeper = world.ReservedFor(d);
                bool reserved = d.reservedForOther || (keeper != 0 && keeper != localId);
                var sr = go.GetComponent<SpriteRenderer>();
                var c = sr.color; c.a = reserved ? .35f : 1f; sr.color = c;
            }
            RemoveUnseen(dropViews);
            dropsSynced = true;
        }

        void SyncShots(float alpha)
        {
            seen.Clear();
            foreach (var s in world.maps[shownMap].shots)
            {
                seen.Add(s.id);
                GameObject go;
                if (!shotViews.TryGetValue(s.id, out go))
                {
                    bool rime=world.GetPlayer(s.owner)!=null && RimecallerRules.Is(world.GetPlayer(s.owner).Hero);
                    var art = Sprites.Art(rime?"Art/Rimecaller/orb":"Art/Projectiles/ranger_arrow");   // painted arrow (64x16, flies to the right)
                    go = art != null ? Sprites.Picture("Arrow", art, ArrowOrder, null, s.x, s.y)
                                     : Sprites.Block("Arrow", new Color(.72f, 1, .43f), .52f, .11f, ArrowOrder);
                    if(rime && art!=null) go.transform.localScale=Vector3.one*(.65f/art.bounds.size.x);
                    shotViews[s.id] = go;
                }
                go.transform.position = new Vector3(Mathf.Lerp(s.px, s.x, alpha), Mathf.Lerp(s.py, s.y, alpha), 0);
                go.GetComponent<SpriteRenderer>().flipX = s.direction < 0;
                { var shotOwner = world.GetPlayer(s.owner); if (shotOwner != null && RimecallerRules.Is(shotOwner.Hero)) AimRimeShot(go, s); }
            }
            RemoveUnseen(shotViews);
        }

        void SyncPortals()
        {
            seen.Clear();
            foreach (var p in world.players)
            {
                if (!p.portalOpen || p.portalMap != shownMap) continue;
                seen.Add(p.id);
                GameObject go;
                if (!portalViews.TryGetValue(p.id, out go))
                {
                    go = new GameObject("Town Portal (Up)");
                    MapScenery.Portal(go.transform, 0, 0, "Art/Props/town_portal", MapScenery.TownPortalColor, MapScenery.PortalOrder);
                    portalViews[p.id] = go;
                }
                go.transform.position = new Vector3(p.portalX, p.portalY, 0);
            }
            RemoveUnseen(portalViews);
        }

        void RemoveUnseen(Dictionary<int, GameObject> views)
        {
            stale.Clear();
            foreach (var kv in views) if (!seen.Contains(kv.Key)) stale.Add(kv.Key);
            foreach (var id in stale) { Object.Destroy(views[id]); views.Remove(id); }
        }

        static void DestroyAll(Dictionary<int, GameObject> views)
        {
            foreach (var go in views.Values) Object.Destroy(go);
            views.Clear();
        }

        // ---------------------------------------------------------------- effects and camera

        // Temporary procedural effects: no external art required for the combat playtest.
        void SpawnDragonFx(SimEvent e)
        {
            bool fissure = e.kind == SimEventKind.DragonFissure;
            bool fang = e.kind == SimEventKind.DragonFang;
            bool sweep = e.kind == SimEventKind.DragonSweep;
            var root = new GameObject(fissure ? "Wyrm Leap Fissure" : fang ? "Dragon Fang" : "Spear Thrust");
            root.transform.position = new Vector3(e.x, e.y + (fissure ? .04f : 0), 0);
            var sr = root.AddComponent<SpriteRenderer>();
            // Share the same sprite material as existing effects (works with the current render pipeline).
            var sample = Sprites.Block("FX material", Color.white, .01f, .01f, SlashOrder);
            sr.sharedMaterial = sample.GetComponent<SpriteRenderer>().sharedMaterial;
            Object.Destroy(sample);
            if (fissure)
            {
                var points = new Vector3[15];
                for (int i = 0; i < points.Length; i++)
                    points[i] = new Vector3(-1.65f + i * 3.3f / 14f, i % 2 == 0 ? 0 : .07f, 0);
                DragonLine(root, sr.sharedMaterial, points, .055f);
                for (int i = -2; i <= 2; i++)
                    DragonLine(root, sr.sharedMaterial, new [] { new Vector3(i * .55f, 0, 0), new Vector3(i * .55f + .15f, .24f, 0), new Vector3(i * .55f + .08f, .38f, 0) }, .025f);
            }
            else if (sweep)
            {
                // Flattened crescent at torso height, following the polearm's lateral arc.
                var arc = new Vector3[17];
                for (int i = 0; i < arc.Length; i++)
                {
                    float angle = Mathf.Lerp(-1.35f, 1.35f, i / 16f);
                    arc[i] = new Vector3(Mathf.Cos(angle) * 1.5f - .65f,
                        Mathf.Sin(angle) * .48f, 0);
                }
                DragonLine(root, sr.sharedMaterial, arc, .075f);
                var strokes = root.GetComponentsInChildren<LineRenderer>();
                foreach (var line in strokes)
                {
                    line.startWidth = .015f; line.endWidth = .075f;
                    line.numCapVertices = 2;
                }
            }
            else
            {
                float length = fang ? 2.8f : 1.6f;
                DragonLine(root, sr.sharedMaterial, new [] { new Vector3(-length * .5f, 0, 0), new Vector3(length * .5f, 0, 0) }, fang ? .07f : .035f);
                DragonLine(root, sr.sharedMaterial, new [] { new Vector3(length * .5f - .3f, .17f, 0), new Vector3(length * .5f, 0, 0), new Vector3(length * .5f - .3f, -.17f, 0) }, .045f);
                if (fang)
                {
                    // Angular spectral jaws opening around the piercing spear.
                    for (int side = -1; side <= 1; side += 2)
                        DragonLine(root, sr.sharedMaterial, new [] { new Vector3(-1.1f, side * .15f, 0), new Vector3(-.5f, side * .48f, 0), new Vector3(.3f, side * .35f, 0), new Vector3(.65f, side * .12f, 0) }, .07f);
                }
            }
            float duration = fissure ? .55f : fang ? .32f : sweep ? .16f : .2f;
            slashes.Add(new SlashFx { obj = root, sr = sr, life = duration, duration = duration,
                direction = e.facing, x = e.x, y = e.y, dragon = fissure ? 3 : fang ? 2 : sweep ? 4 : 1 });
        }

        static void DragonLine(GameObject parent, Material material, Vector3[] points, float width)
        {
            var child = new GameObject("Ember stroke"); child.transform.SetParent(parent.transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = material; line.useWorldSpace = false;
            line.positionCount = points.Length; line.SetPositions(points);
            line.startWidth = line.endWidth = width; line.sortingOrder = SlashOrder;
            line.startColor = line.endColor = new Color(1f, .58f, .18f, .9f);
        }

        void SpawnSlash(SimEvent e)
        {
            var obj = Sprites.Block(e.heavy ? "Dragon Strike VFX" : "Spear Slash VFX", new Color(1, .1f, .17f, .8f), e.heavy ? 2.8f : 1.35f, e.heavy ? .18f : .09f, SlashOrder);
            float life = e.heavy ? .38f : .18f;
            slashes.Add(new SlashFx { obj = obj, sr = obj.GetComponent<SpriteRenderer>(), life = life, duration = life, direction = e.facing, x = e.x, y = e.y });
        }

        // The root slam: a shockwave ring spreading along the ground from the boss's feet.
        void SpawnSlamRing(SimEvent e)
        {
            var obj = Sprites.Blob("Slam VFX", new Color(.75f, .6f, .3f, .7f), 1f, .35f, SlashOrder, null, e.x, e.y + .15f);
            slashes.Add(new SlashFx { obj = obj, sr = obj.GetComponent<SpriteRenderer>(), life = .45f, duration = .45f, direction = 0, x = e.x, y = e.y + .15f, ring = true });
        }

        public void Number(float x, float y, string text, Color color, bool big)
        {
            numbers.Add(new FloatingNumber { x = x + Random.Range(-.25f, .25f), y = y, text = text, color = color, big = big, life = big ? 1.1f : .85f });
        }

        void UpdateSlashes(float dt)
        {
            for (int i = slashes.Count - 1; i >= 0; i--)
            {
                var fx = slashes[i];
                fx.life -= dt;
                if (fx.life <= 0) { Object.Destroy(fx.obj); slashes.RemoveAt(i); continue; }
                float t = fx.life / fx.duration;
                if (fx.dragon != 0)
                {
                    float age = 1f - t;
                    if (fx.dragon == 4)
                        fx.obj.transform.rotation = Quaternion.Euler(0, 0, fx.direction * Mathf.Lerp(-15f, 15f, age));
                    float grow = Mathf.Min(1f, age * 5f);
                    fx.obj.transform.localScale = new Vector3((fx.dragon == 3 ? 1 : fx.direction) * (.25f + .75f * grow), 1, 1);
                    foreach (var line in fx.obj.GetComponentsInChildren<LineRenderer>())
                        line.startColor = line.endColor = new Color(1f, .58f, .18f, t * .95f);
                    continue;
                }
                if (fx.ring)
                {
                    float grow = 1f - t;
                    fx.obj.transform.localScale = new Vector3(1f + grow * 2f * Tuning.BossSlamRange, .35f + grow * .25f, 1f);
                    fx.sr.color = new Color(.75f, .6f, .3f, t * .7f);
                    continue;
                }
                fx.sr.color = new Color(1, .12f, .18f, t * .8f);
                fx.obj.transform.position = new Vector3(fx.x, fx.y, 0);
                fx.obj.transform.rotation = Quaternion.Euler(0, 0, fx.direction * (1 - t) * 22);
            }
        }

        // Follows the local player in both directions, never showing past the map's edges, and moves the
        // backdrop layers by their parallax factors.
        void UpdateCamera(PlayerState local, float frameDt, float alpha)
        {
            var def = Maps.All[shownMap];
            float minX, maxX, minY, maxY;
            CameraRange(def, ViewHalfHeight * camera.aspect, out minX, out maxX, out minY, out maxY);
            float x = local != null ? Mathf.Lerp(local.px, local.x, alpha) : def.startX;
            float y = (local != null ? Mathf.Lerp(local.py, local.y, alpha) : def.startY) + 1.2f;
            var target = new Vector3(Mathf.Clamp(x, minX, maxX), Mathf.Clamp(y, minY, maxY), -10);
            if (snapCamera || local == null) { cam.position = target; snapCamera = local == null; }
            else
            {
                var pos = cam.position;
                pos.x = Mathf.Lerp(pos.x, target.x, Mathf.Min(1f, frameDt * 5f));

                // Keep ordinary hops inside a stable view. Give falling less slack so the
                // landing area comes into frame early. Ladders and landings use normal follow.
                const float riseDeadZone = 2.3f;
                const float fallDeadZone = .75f;
                if (!local.grounded && !local.climbing && !local.dead)
                {
                    target.y = Mathf.Clamp(pos.y, y - riseDeadZone, y + fallDeadZone);
                    target.y = Mathf.Clamp(target.y, minY, maxY);
                }

                float verticalRate = target.y < pos.y ? 6.5f : 4.5f;
                pos.y = Mathf.Lerp(pos.y, target.y, 1f - Mathf.Exp(-verticalRate * frameDt));
                // Keep some space below the hero during a long fall, within the map's limits.
                pos.y = Mathf.Clamp(Mathf.Min(pos.y, y + 2.8f), minY, maxY);
                cam.position = pos;
            }
            var visual = visuals[shownMap];
            Vector3 c = cam.position;
            float halfWidth = ViewHalfHeight * camera.aspect;
            foreach (var layer in visual.layers)
            {
                if (!layer.panorama)
                {
                    layer.transform.position = new Vector3(c.x * layer.fx, (c.y - visual.cameraLowY) * layer.fy, 0);
                    continue;
                }
                // Across: the picture's left edge shows at the map's left end and its right edge at the right end
                // (travel 1); a distant picture slides through less of its spare width, centred, so it seems further away.
                // Up: at the lowest camera its painted path sits on the ground; at the highest its top meets the view's top.
                float tx = maxX > minX ? Mathf.InverseLerp(minX, maxX, c.x) : .5f;
                float ty = maxY > minY ? Mathf.InverseLerp(minY, maxY, c.y) : 0f;
                float spare = layer.width - 2f * halfWidth;
                float left = spare > 0f ? c.x - halfWidth - (.5f + (tx - .5f) * layer.travel) * spare : c.x - layer.width * .5f;
                float low = Tuning.Ground - layer.floor * layer.height;
                float high = layer.floorAnchored ? low : Mathf.Max(low, maxY + ViewHalfHeight - layer.height);
                layer.transform.position = new Vector3(left, Mathf.Lerp(low, high, ty), 0);
            }
            visual.Animate(Time.time, c, halfWidth, ViewHalfHeight);     // motes, leaves, sparks, smoke
        }
    }
}
