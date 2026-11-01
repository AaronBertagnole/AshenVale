using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshenVale
{
    // Loads and caches sprites, and makes the placeholder shapes (squares, soft discs, gradients)
    // that stand in until real art is dropped into Resources/Art (see ART.md).
    public static class Sprites
    {
        static readonly Dictionary<string, Sprite[]> frames = new Dictionary<string, Sprite[]>();
        static readonly Dictionary<string, Sprite> singles = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Sprite> gradients = new Dictionary<string, Sprite>();
        static Sprite square, disc;

        // Frames of Resources/Heroes/<path>, sorted by file name.
        public static Sprite[] Hero(string path) { return Frames("Heroes/" + path); }

        // Every sprite in a Resources folder, sorted by file name (Resources.LoadAll promises no order).
        // Empty if the folder doesn't exist.
        public static Sprite[] Frames(string resourcePath)
        {
            Sprite[] result;
            if (!frames.TryGetValue(resourcePath, out result))
            {
                result = Resources.LoadAll<Sprite>(resourcePath);
                Array.Sort(result, (a, b) => string.CompareOrdinal(a.name, b.name));
                frames[resourcePath] = result;
            }
            return result;
        }

        // One optional sprite, e.g. "Art/Themes/meadow/sky". Null if that art doesn't exist yet.
        public static Sprite Art(string resourcePath)
        {
            Sprite result;
            if (!singles.TryGetValue(resourcePath, out result))
            {
                result = Resources.Load<Sprite>(resourcePath);
                singles[resourcePath] = result;
            }
            return result;
        }

        public static Sprite Square
        {
            get
            {
                if (square == null)
                {
                    var pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    pixel.filterMode = FilterMode.Point;
                    pixel.SetPixel(0, 0, Color.white);
                    pixel.Apply();
                    square = Sprite.Create(pixel, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
                }
                return square;
            }
        }

        // A white circle with a soft edge, 1 unit across. Overlapping discs make hills, bushes and tree tops.
        public static Sprite Disc
        {
            get
            {
                if (disc == null)
                {
                    const int size = 96;
                    var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    float r = size * .5f;
                    for (int y = 0; y < size; y++)
                        for (int x = 0; x < size; x++)
                        {
                            float d = Mathf.Sqrt((x + .5f - r) * (x + .5f - r) + (y + .5f - r) * (y + .5f - r));
                            tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d - .5f)));
                        }
                    tex.Apply();
                    disc = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
                }
                return disc;
            }
        }

        // A vertical gradient, 1 x 1 unit, from bottom color to top color.
        public static Sprite Gradient(Color bottom, Color top)
        {
            string key = ColorUtility.ToHtmlStringRGBA(bottom) + ColorUtility.ToHtmlStringRGBA(top);
            Sprite result;
            if (!gradients.TryGetValue(key, out result))
            {
                const int height = 64;
                var tex = new Texture2D(1, height, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < height; y++) tex.SetPixel(0, y, Color.Lerp(bottom, top, y / (height - 1f)));
                tex.Apply();
                result = Sprite.Create(tex, new Rect(0, 0, 1, height), new Vector2(.5f, .5f), height);
                gradients[key] = result;
            }
            return result;
        }

        // A tinted, scaled square: all world art is made of these until real sprites arrive.
        public static GameObject Block(string name, Color color, float width, float height, int order, Transform parent = null)
        {
            return Shape(name, Square, color, width, height, order, parent);
        }

        public static GameObject Block(string name, Color color, float width, float height, int order, Transform parent, float x, float y)
        {
            var o = Block(name, color, width, height, order, parent);
            o.transform.position = new Vector3(x, y, 0);
            return o;
        }

        // A soft ellipse width x height, centred on (x, y).
        public static GameObject Blob(string name, Color color, float width, float height, int order, Transform parent, float x, float y)
        {
            var o = Shape(name, Disc, color, width, height, order, parent);
            o.transform.position = new Vector3(x, y, 0);
            return o;
        }

        // A gradient band width x height, centred on (x, y).
        public static GameObject Band(string name, Color bottom, Color top, float width, float height, int order, Transform parent, float x, float y)
        {
            var o = Shape(name, Gradient(bottom, top), Color.white, width, height, order, parent);
            o.transform.position = new Vector3(x, y, 0);
            return o;
        }

        // A real sprite at its own size (scale 1), placed by its pivot.
        public static GameObject Picture(string name, Sprite sprite, int order, Transform parent, float x, float y)
        {
            var o = new GameObject(name);
            if (parent != null) o.transform.SetParent(parent, false);
            var s = o.AddComponent<SpriteRenderer>();
            s.sprite = sprite;
            s.sortingOrder = order;
            o.transform.position = new Vector3(x, y, 0);
            return o;
        }

        // A real sprite repeated to fill width x height (needs Full Rect mesh, set by the art importer).
        public static GameObject Tiled(string name, Sprite sprite, float width, float height, int order, Transform parent, float x, float y)
        {
            var o = Picture(name, sprite, order, parent, x, y);
            var s = o.GetComponent<SpriteRenderer>();
            s.drawMode = SpriteDrawMode.Tiled;
            s.size = new Vector2(width, height);
            return o;
        }

        static GameObject Shape(string name, Sprite sprite, Color color, float width, float height, int order, Transform parent)
        {
            var o = new GameObject(name);
            if (parent != null) o.transform.SetParent(parent, false);
            var s = o.AddComponent<SpriteRenderer>();
            s.sprite = sprite;
            s.color = color;
            s.sortingOrder = order;
            o.transform.localScale = new Vector3(width / sprite.bounds.size.x, height / sprite.bounds.size.y, 1);
            return o;
        }
    }
}
