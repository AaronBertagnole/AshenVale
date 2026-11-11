using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AshenVale
{
    // Small, code-owned nine-slice surfaces: no baked labels, screen-sized HUD texture, or extra art dependency.
    public sealed class HudTheme
    {
        // The scheme: light grey (#cccccc) panels with dark text, neutral charcoal for the things that hold
        // glowing icons, bars and light text (action slots, meter tracks), and copper for accents.
        public static readonly Color Ink = new Color(.74f, .74f, .74f, .97f);       // panel, bottom of its gradient
        public static readonly Color Copper = new Color(.79f, .37f, .23f);
        public static readonly Color CopperText = new Color(.56f, .21f, .09f);      // copper that reads on a light panel
        public static readonly Color Text = new Color(.10f, .10f, .11f);            // text on light panels
        public static readonly Color TextMuted = new Color(.30f, .30f, .31f);
        public static readonly Color Ivory = new Color(.96f, .94f, .90f);           // text over the game world or on dark slots
        public static readonly Color Muted = new Color(.80f, .79f, .77f);
        public static readonly Color Teal = new Color(.64f, .77f, .60f);
        public static readonly Color Line = new Color(.32f, .32f, .33f);
        public readonly Font font;
        public readonly Sprite panel, slot, track, parchment, white, button, buttonHover, buttonPressed;

        // A colour meant for dark backgrounds, made readable on a light panel.
        public static Color OnLight(Color c)
        {
            float light = c.r * .3f + c.g * .59f + c.b * .11f;
            return light > .5f ? new Color(c.r * .42f, c.g * .42f, c.b * .42f, c.a) : c;
        }
        readonly List<Object> owned = new List<Object>();

        public HudTheme()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            panel = Surface(new Color(.83f, .83f, .83f, .97f), Ink, Line, 13);
            slot = Surface(new Color(.27f, .27f, .28f), new Color(.15f, .15f, .16f), Copper, 11);
            track = Surface(new Color(.11f, .11f, .12f), new Color(.06f, .06f, .065f), new Color(.34f, .34f, .35f), 8);
            button = Surface(new Color(.93f, .93f, .93f), new Color(.84f, .84f, .84f), Line, 11);
            buttonHover = Surface(new Color(1f, 1f, 1f), new Color(.93f, .93f, .93f), Copper, 11);
            buttonPressed = Surface(new Color(.72f, .72f, .72f), new Color(.80f, .80f, .80f), Copper, 11);
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
