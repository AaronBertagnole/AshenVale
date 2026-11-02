using UnityEngine;
using UnityEngine.UI;

namespace AshenVale
{
    // Actual terrain and entities, including the same discovery rules as the world atlas.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudMinimap : MaskableGraphic
    {
        public World world;
        public PlayerState player;
        MapDef map;
        Rect plot;
        float scale;
        public void Refresh(World state, PlayerState me) { world = state; player = me; SetVerticesDirty(); }
        Vector2 At(float x, float y) { return new Vector2(plot.xMin + Mathf.Clamp(x, 0, map.columns) * scale, plot.yMin + Mathf.Clamp(y - map.Bottom, 0, map.rows) * scale); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (world == null || player == null || player.map < 0 || player.map >= world.maps.Length) return;
            map = world.maps[player.map].def;
            Rect r = rectTransform.rect;
            scale = Mathf.Min((r.width - 12) / Mathf.Max(1, map.columns), (r.height - 12) / Mathf.Max(1, map.rows));
            plot = new Rect(r.center.x - map.columns * scale / 2, r.center.y - map.rows * scale / 2, map.columns * scale, map.rows * scale);
            Color stone = new Color(.82f, .77f, .58f, .9f);
            Box(vh, At(0, map.Bottom), new Vector2(map.columns * scale, 2), stone);
            foreach (var p in map.platforms) Box(vh, At(p.left, p.top), new Vector2((p.right - p.left) * scale, 2), stone);
            foreach (var l in map.ladders) Box(vh, At(l.x, l.bottom), new Vector2(1.4f, (l.top - l.bottom) * scale), HudTheme.Copper);
            foreach (var p in map.portals)
                if (!WorldAtlas.IsSecret(p.targetMap) || player.ch.HasSeen(p.targetMap)) Mark(vh, At(p.x, p.y + .8f), 4, HudTheme.Teal);
            if (map.kind == MapKind.Town)
            {
                Mark(vh, At(map.returnX, map.returnY + .8f), 3, new Color(.78f, .7f, 1));
                Mark(vh, At(map.waystoneX, map.waystoneY + .6f), 3, HudTheme.Teal);
                foreach (var p in map.merchants) Mark(vh, At(p.x, p.y + .6f), 3, HudTheme.Copper);
            }
            if (map.hasSmithy) Mark(vh, At(map.smithyX, map.smithyY + .6f), 3, new Color(1, .5f, .2f));
            foreach (var m in world.maps[player.map].monsters) Mark(vh, At(m.x, m.y + .5f), m.Def.IsChest ? 3 : 2, m.Def.IsChest ? Color.yellow : new Color(.96f, .33f, .32f));
            foreach (var p in world.players) if (p != player && p.map == player.map && !p.dead) Mark(vh, At(p.x, p.y + .6f), 3, new Color(.5f, 1, .55f));
            var cam = Camera.main;
            if (cam && cam.orthographic)
            {
                var c = cam.transform.position; float h = cam.orthographicSize, w = h * cam.aspect;
                var a = At(c.x - w, c.y - h); var b = At(c.x + w, c.y + h); Color edge = new Color(1, 1, 1, .2f);
                Box(vh, a, new Vector2(b.x - a.x, 1), edge); Box(vh, new Vector2(a.x, b.y), new Vector2(b.x - a.x, 1), edge);
                Box(vh, a, new Vector2(1, b.y - a.y), edge); Box(vh, new Vector2(b.x, a.y), new Vector2(1, b.y - a.y), edge);
            }
            Mark(vh, At(player.x, player.y + .6f), 4.5f, Color.white);
            Mark(vh, At(player.x, player.y + .6f), 3, HudTheme.Teal);
        }
        static void Box(VertexHelper vh, Vector2 a, Vector2 size, Color c)
        { int n = vh.currentVertCount; vh.AddVert(a, c, Vector2.zero); vh.AddVert(a + new Vector2(0, size.y), c, Vector2.zero); vh.AddVert(a + size, c, Vector2.zero); vh.AddVert(a + new Vector2(size.x, 0), c, Vector2.zero); vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3); }
        static void Mark(VertexHelper vh, Vector2 p, float radius, Color c)
        { int n = vh.currentVertCount; vh.AddVert(p + new Vector2(-radius, 0), c, Vector2.zero); vh.AddVert(p + new Vector2(0, radius), c, Vector2.zero); vh.AddVert(p + new Vector2(radius, 0), c, Vector2.zero); vh.AddVert(p + new Vector2(0, -radius), c, Vector2.zero); vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3); }
    }
}

