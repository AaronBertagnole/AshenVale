using UnityEngine;

namespace AshenVale
{
    // Small atlas seals. Difficulty is an accent, rather than the entire location icon.
    public static class AtlasMarkerArt
    {
        static readonly Texture2D[] icons = new Texture2D[6];
        static readonly string[] names = { "town", "hunting", "boss", "unknown", "current", "difficulty" };

        static Texture2D Icon(int index)
        {
            if (icons[index] == null)
                icons[index] = Resources.Load<Texture2D>("Art/UI/MapMarkers/" + names[index]);
            return icons[index];
        }

        static void Stamp(Vector2 center, float size, int index, Color tint)
        {
            var texture = Icon(index);
            if (texture == null) return;
            Color previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(new Rect(center.x - size / 2f, center.y - size / 2f, size, size), texture, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
        }

        // The place you are on flashes (seal and compass ring together) instead of carrying a "you are here" label.
        public static void Draw(Vector2 at, float scale, bool seen, bool town, bool boss,
                                bool current, Color difficulty, bool hovered)
        {
            float flash = current ? .55f + .45f * Mathf.Sin(Time.unscaledTime * 5f) : 1f;    // 0.1 .. 1, about twice a second
            if (current || hovered)
                Stamp(at, 21f * scale, 4, new Color(1f, 1f, 1f, current ? flash : .65f));
            int kind = !seen ? 3 : town ? 0 : boss ? 2 : 1;
            float size = !seen ? 12f : boss ? 16f : 15f;
            Stamp(at, size * scale, kind, new Color(1f, 1f, 1f, current ? .4f + .6f * flash : 1f));
            if (seen && !town)
                Stamp(at + new Vector2(5f, 5f) * scale, 6f * scale, 5, new Color(difficulty.r, difficulty.g, difficulty.b, current ? .4f + .6f * flash : 1f));
        }
    }
}
