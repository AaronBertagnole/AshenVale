using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AshenVale
{
    // Draws the map the local player is on: scenery, heroes, monsters, drops, arrows, portals and effects.
    // It only reads the World (plus its event list, which it clears); it never changes game state.
    public sealed class WorldView
    {
        sealed class HeroView { public GameObject go; public SpriteRenderer sr; public string anim = ""; public float clock; }
        sealed class SlashFx { public GameObject obj; public SpriteRenderer sr; public float life, duration, x, y; public int direction; }

        // A monster on screen: `go` stands at its feet and is mirrored to face its way; `body` is what squashes and bobs.
        sealed class MonsterView
        {
            public GameObject go;
            public Transform body;
            public SpriteRenderer art;          // set when the monster has art (Resources/Art/Monsters/<key>/)
            public SpriteRenderer[] parts;      // every renderer, for the hurt flash and the fade when it dies
            public Color[] colors;
            public MonsterDef def;
            public float clock, hurt, moveTime, lastHp, lastX, dying;
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

            ReadEvents();
            SyncHeroes(localId, frameDt, alpha);
            SyncMonsters(frameDt, alpha);
            SyncDrops();
            SyncShots(alpha);
            SyncPortals();
            UpdateSlashes(frameDt);
            UpdateCamera(local, frameDt, alpha);
        }

        void ShowMap(int map)
        {
            for (int i = 0; i < visuals.Length; i++) visuals[i].root.SetActive(i == map);
            shownMap = map;
            ClearMonsters();
            foreach (var fx in slashes) Object.Destroy(fx.obj);
            slashes.Clear();
            snapCamera = true;
        }

        void ReadEvents()
        {
            foreach (var e in world.events)
                if (e.kind == SimEventKind.Slash && e.map == shownMap) SpawnSlash(e);
            world.events.Clear();
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
                Animate(p, v, frameDt);
                float x = Mathf.Lerp(p.px, p.x, alpha), y = Mathf.Lerp(p.py, p.y, alpha);
                v.go.transform.localScale = new Vector3(HeroScale, p.crouching ? 1.10f : HeroScale, 1f);
                v.go.transform.position = new Vector3(x, y + (p.crouching ? .33f : .48f), 0);
            }
            stale.Clear();
            foreach (var kv in heroViews) if (!seen.Contains(kv.Key)) stale.Add(kv.Key);
            foreach (var id in stale) { Object.Destroy(heroViews[id].go); heroViews.Remove(id); }
        }

        static string ChooseAnimation(PlayerState p)
        {
            var h = p.Hero;
            if (p.dead) return "death";
            if (p.hurtVisual > 0) return "hurt";
            if (p.attackVisual > 0) return p.lastWasSkill ? h.skillAnim : (p.comboStep % 2 == 0 ? h.basicAnimA : h.basicAnimB);
            if (p.climbing) return "run";
            if (p.crouching) return "idle";
            if (!p.grounded) return p.vy > 0 ? "jump" : "fall";
            return Mathf.Abs(p.vx) > .2f ? "run" : "idle";
        }

        static void Animate(PlayerState p, HeroView v, float frameDt)
        {
            var h = p.Hero;
            string clip = ChooseAnimation(p);
            if (clip != v.anim) { v.anim = clip; v.clock = 0; }
            v.clock += frameDt;
            Sprite[] frames = Sprites.Hero(h.folder + "/" + clip);
            float fps = clip == "idle" ? 5 : clip == "run" ? 11 : clip == "death" ? 7 : 12;
            if (frames.Length > 0)
            {
                int index = Mathf.FloorToInt(v.clock * fps);
                bool once = clip == "death" || clip == "hurt" || clip == h.basicAnimA || clip == h.basicAnimB || clip == h.skillAnim;
                index = once ? Mathf.Min(index, frames.Length - 1) : index % frames.Length;
                v.sr.sprite = frames[index];
            }
            v.sr.flipX = p.facing < 0;
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
                if (m.hp < v.lastHp - .01f) v.hurt = .25f; else v.hurt = Mathf.Max(0, v.hurt - frameDt);
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
                else
                {
                    float squash = moving ? Mathf.Sin(v.clock * 12f) * .06f : Mathf.Sin(v.clock * 2.5f) * .02f;
                    v.body.localScale = new Vector3(1f - squash, 1f + squash, 1f);
                }
                float flash = v.hurt / .25f;
                for (int i = 0; i < v.parts.Length; i++)
                    v.parts[i].color = Color.Lerp(v.colors[i], new Color(1f, .45f, .4f, v.colors[i].a), flash * .7f);
            }

            stale.Clear();
            foreach (var kv in monsterViews) if (!seen.Contains(kv.Key)) stale.Add(kv.Key);
            foreach (var id in stale) { dying.Add(monsterViews[id]); monsterViews.Remove(id); }   // monsters only leave by dying

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
                var art = Sprites.Picture("Art", idle[0], 0, root, 0, 0);
                v.art = art.GetComponent<SpriteRenderer>();
                v.body = art.transform;
            }
            else
            {
                v.body = new GameObject("Body").transform;
                v.body.SetParent(root, false);
                float w = d.width, h = d.height;
                Color dark = Color.Lerp(d.color, Color.black, .35f);
                if (d.speed <= 0f)
                {
                    Part("Chest", Sprites.Block("Chest", d.color, w, h * .7f, 0), v.body, 0, h * .35f);
                    Part("Lid", Sprites.Block("Lid", dark, w * 1.06f, h * .32f, 1), v.body, 0, h * .78f);
                    Part("Latch", Sprites.Block("Latch", new Color(1f, .82f, .3f), w * .16f, h * .24f, 2), v.body, 0, h * .6f);
                }
                else
                {
                    Part("Shadow", Sprites.Blob("Shadow", new Color(0, 0, 0, .25f), w * 1.05f, .18f, 0, null, 0, 0), root, 0, -d.hover + .04f);
                    Part("Body", Sprites.Blob("Body", d.color, w, h, 1, null, 0, 0), v.body, 0, h * .5f);
                    Part("Shine", Sprites.Blob("Shine", new Color(1, 1, 1, .22f), w * .45f, h * .3f, 2, null, 0, 0), v.body, -w * .15f, h * .72f);
                    for (int i = 0; i < 2; i++)
                    {
                        float ex = w * (.12f + i * .2f), ey = h * .6f;
                        Part("Eye", Sprites.Blob("Eye", Color.white, .17f, .21f, 2, null, 0, 0), v.body, ex, ey);
                        Part("Pupil", Sprites.Blob("Pupil", new Color(.12f, .1f, .14f), .09f, .12f, 3, null, 0, 0), v.body, ex + .03f, ey - .01f);
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

        void SyncDrops()
        {
            seen.Clear();
            foreach (var d in world.maps[shownMap].drops)
            {
                seen.Add(d.id);
                GameObject go;
                if (!dropViews.TryGetValue(d.id, out go))
                {
                    bool part = d.kind == DropKind.Part && d.value >= 0 && d.value < Parts.All.Length;
                    string name = d.kind == DropKind.HpPotion ? "HP Potion" : d.kind == DropKind.MpPotion ? "MP Potion" : d.kind == DropKind.Item ? d.item.name
                                : part ? Parts.All[d.value].name : "Gold drop";
                    Color color = d.kind == DropKind.HpPotion ? new Color(1, .22f, .32f) : d.kind == DropKind.MpPotion ? new Color(.22f, .48f, 1)
                                : d.kind == DropKind.Item ? Loot.RarityColor(d.item.rarity) : part ? Parts.All[d.value].color : new Color(1, .81f, .18f);
                    // Parts are small round blobs, everything else a little block (until drops get icons).
                    go = part ? Sprites.Blob(name, color, .3f, .3f, DropOrder, null, 0, 0) : Sprites.Block(name, color, .28f, .34f, DropOrder);
                    dropViews[d.id] = go;
                }
                go.transform.position = new Vector3(d.x, d.y + Mathf.Sin(Time.time * 4 + d.x) * .1f, 0);
            }
            RemoveUnseen(dropViews);
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
                    go = Sprites.Block("Arrow", new Color(.72f, 1, .43f), .52f, .11f, ArrowOrder);
                    shotViews[s.id] = go;
                }
                go.transform.position = new Vector3(Mathf.Lerp(s.px, s.x, alpha), Mathf.Lerp(s.py, s.y, alpha), 0);
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
                    Sprites.Blob("Glow", new Color(.45f, .4f, 1f, .45f), 1f, 1.9f, MapScenery.PortalOrder, go.transform, 0, .95f);
                    Sprites.Blob("Core", new Color(.8f, .78f, 1f, .85f), .45f, 1.4f, MapScenery.PortalOrder + 1, go.transform, 0, .95f);
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

        void SpawnSlash(SimEvent e)
        {
            var obj = Sprites.Block(e.heavy ? "Dragon Strike VFX" : "Spear Slash VFX", new Color(1, .1f, .17f, .8f), e.heavy ? 2.8f : 1.35f, e.heavy ? .18f : .09f, SlashOrder);
            float life = e.heavy ? .38f : .18f;
            slashes.Add(new SlashFx { obj = obj, sr = obj.GetComponent<SpriteRenderer>(), life = life, duration = life, direction = e.facing, x = e.x, y = e.y });
        }

        void UpdateSlashes(float dt)
        {
            for (int i = slashes.Count - 1; i >= 0; i--)
            {
                var fx = slashes[i];
                fx.life -= dt;
                if (fx.life <= 0) { Object.Destroy(fx.obj); slashes.RemoveAt(i); continue; }
                float t = fx.life / fx.duration;
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
                pos.y = Mathf.Lerp(pos.y, target.y, Mathf.Min(1f, frameDt * 3.5f));
                cam.position = pos;
            }
            var visual = visuals[shownMap];
            foreach (var layer in visual.layers)
                layer.transform.position = new Vector3(cam.position.x * layer.fx, (cam.position.y - visual.cameraLowY) * layer.fy, 0);
        }
    }
}
