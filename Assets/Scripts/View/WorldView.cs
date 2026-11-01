using System.Collections.Generic;
using UnityEngine;

namespace AshenVale
{
    // Draws the map the local player is on: scenery, heroes, monsters, drops, arrows, portals and effects.
    // It only reads the World (plus its event list, which it clears); it never changes game state.
    public sealed class WorldView
    {
        sealed class HeroView { public GameObject go; public SpriteRenderer sr; public string anim = ""; public float clock; }
        sealed class SlashFx { public GameObject obj; public SpriteRenderer sr; public float life, duration, x, y; public int direction; }

        const float HeroScale = 1.65f;

        readonly World world;
        readonly Transform cam;
        readonly GameObject[] mapRoots;
        int shownMap = -1;
        bool snapCamera = true;

        readonly Dictionary<int, HeroView> heroViews = new Dictionary<int, HeroView>();
        readonly Dictionary<int, GameObject> monsterViews = new Dictionary<int, GameObject>();
        readonly Dictionary<int, GameObject> dropViews = new Dictionary<int, GameObject>();
        readonly Dictionary<int, GameObject> shotViews = new Dictionary<int, GameObject>();
        readonly Dictionary<int, GameObject> portalViews = new Dictionary<int, GameObject>();
        readonly List<SlashFx> slashes = new List<SlashFx>();
        readonly HashSet<int> seen = new HashSet<int>();
        readonly List<int> stale = new List<int>();

        public WorldView(World world)
        {
            this.world = world;
            var camera = Camera.main;
            if (camera == null)
            {
                var c = new GameObject("Main Camera");
                camera = c.AddComponent<Camera>();
                c.tag = "MainCamera";
            }
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.backgroundColor = new Color(.07f, .09f, .16f);
            cam = camera.transform;
            cam.position = new Vector3(0, 0, -10);

            mapRoots = new GameObject[Maps.All.Length];
            for (int i = 0; i < mapRoots.Length; i++)
            {
                mapRoots[i] = MapScenery.Build(Maps.All[i]);
                mapRoots[i].SetActive(false);
            }
        }

        // Removes every hero, monster, drop and effect (new run, back to menu).
        public void Reset()
        {
            foreach (var v in heroViews.Values) Object.Destroy(v.go);
            heroViews.Clear();
            DestroyAll(monsterViews); DestroyAll(dropViews); DestroyAll(shotViews); DestroyAll(portalViews);
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
            SyncMonsters(alpha);
            SyncDrops();
            SyncShots(alpha);
            SyncPortals();
            UpdateSlashes(frameDt);
            UpdateCamera(local, frameDt, alpha);
        }

        void ShowMap(int map)
        {
            for (int i = 0; i < mapRoots.Length; i++) mapRoots[i].SetActive(i == map);
            shownMap = map;
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
                v.sr.sortingOrder = p.id == localId ? 10 : 9;
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

        void SyncMonsters(float alpha)
        {
            seen.Clear();
            foreach (var m in world.maps[shownMap].monsters)
            {
                seen.Add(m.id);
                GameObject go;
                if (!monsterViews.TryGetValue(m.id, out go))
                {
                    var d = m.Def;
                    go = Sprites.Block(d.name, d.color, d.width, d.height, 6);
                    monsterViews[m.id] = go;
                }
                go.transform.position = new Vector3(Mathf.Lerp(m.px, m.x, alpha), Mathf.Lerp(m.py, m.y, alpha) + .6f, 0);
            }
            RemoveUnseen(monsterViews);
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
                    string name = d.kind == DropKind.HpPotion ? "HP Potion" : d.kind == DropKind.MpPotion ? "MP Potion" : d.kind == DropKind.Item ? d.item.name : "Gold drop";
                    Color color = d.kind == DropKind.HpPotion ? new Color(1, .22f, .32f) : d.kind == DropKind.MpPotion ? new Color(.22f, .48f, 1)
                                : d.kind == DropKind.Item ? Loot.RarityColor(d.item.rarity) : new Color(1, .81f, .18f);
                    go = Sprites.Block(name, color, .28f, .34f, 7);
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
                    go = Sprites.Block("Arrow", new Color(.72f, 1, .43f), .52f, .11f, 13);
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
                    go = Sprites.Block("Town Portal (Up)", new Color(.3f, .35f, 1, .75f), .85f, 1.7f, 8);
                    portalViews[p.id] = go;
                }
                go.transform.position = new Vector3(p.portalX, p.portalY + .8f, 0);
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
            var obj = Sprites.Block(e.heavy ? "Dragon Strike VFX" : "Spear Slash VFX", new Color(1, .1f, .17f, .8f), e.heavy ? 2.8f : 1.35f, e.heavy ? .18f : .09f, 12);
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

        void UpdateCamera(PlayerState local, float frameDt, float alpha)
        {
            if (local == null) return;
            var def = Maps.All[local.map];
            float x = Mathf.Lerp(local.px, local.x, alpha);
            var target = new Vector3(Mathf.Clamp(x, -def.cameraClamp, def.cameraClamp), 0, -10);
            if (snapCamera) { cam.position = target; snapCamera = false; }
            else cam.position = Vector3.Lerp(cam.position, target, frameDt * (def.kind == MapKind.Town ? 6 : 4));
        }
    }
}
