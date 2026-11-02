using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AshenVale
{
    // Small, code-owned nine-slice surfaces: no baked labels, screen-sized HUD texture, or extra art dependency.
    public sealed class HudTheme
    {
        public static readonly Color Ink = new Color(.035f, .13f, .15f, .96f);
        public static readonly Color Copper = new Color(.78f, .58f, .32f);
        public static readonly Color Ivory = new Color(1f, .94f, .79f);
        public static readonly Color Muted = new Color(.68f, .79f, .77f);
        public static readonly Color Teal = new Color(.26f, .91f, .85f);
        public readonly Font font;
        public readonly Sprite panel, slot, track, parchment, white;
        readonly List<Object> owned = new List<Object>();

        public HudTheme()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            panel = Surface(new Color(.075f, .24f, .25f, .96f), Ink, Copper, 13);
            slot = Surface(new Color(.08f, .23f, .24f), new Color(.02f, .08f, .10f), Copper, 11);
            track = Surface(new Color(.025f, .10f, .12f), new Color(.015f, .055f, .065f), new Color(.20f, .34f, .32f), 8);
            parchment = Surface(new Color(.96f, .86f, .65f), new Color(.86f, .73f, .51f), Copper, 10);
            white = Surface(Color.white, new Color(.83f, .88f, .88f), new Color(1, 1, 1, .3f), 8);
        }

        Sprite Surface(Color top, Color bottom, Color rim, int radius)
        {
            const int n = 64;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "HUD surface", hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - (n - 1) * .5f) - ((n - 1) * .5f - radius), 0);
                float dy = Mathf.Max(Mathf.Abs(y - (n - 1) * .5f) - ((n - 1) * .5f - radius), 0);
                float distance = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                float alpha = Mathf.Clamp01(.5f - distance);
                Color c = Color.Lerp(bottom, top, y / (float)(n - 1));
                if (distance > -1.7f) c = rim;
                else if (distance > -2.7f) c = Color.Lerp(c, Color.white, .12f);
                c.a *= alpha;
                pixels[y * n + x] = c;
            }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
            sprite.name = "HUD nine-slice"; sprite.hideFlags = HideFlags.DontSave;
            owned.Add(sprite); owned.Add(texture); return sprite;
        }

        public void Dispose() { foreach (var item in owned) if (item) Object.Destroy(item); owned.Clear(); }
    }
}
