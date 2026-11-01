using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshenVale
{
    // Loads and caches sprites, and makes the placeholder colored squares.
    public static class Sprites
    {
        static readonly Dictionary<string, Sprite[]> frames = new Dictionary<string, Sprite[]>();
        static Sprite square;

        // Frames of Resources/Heroes/<path>, sorted by file name (Resources.LoadAll promises no order).
        public static Sprite[] Hero(string path)
        {
            Sprite[] result;
            if (!frames.TryGetValue(path, out result))
            {
                result = Resources.LoadAll<Sprite>("Heroes/" + path);
                Array.Sort(result, (a, b) => string.CompareOrdinal(a.name, b.name));
                frames[path] = result;
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

        // A tinted, scaled square: all world art is made of these until real sprites arrive.
        public static GameObject Block(string name, Color color, float width, float height, int order, Transform parent = null)
        {
            var o = new GameObject(name);
            if (parent != null) o.transform.SetParent(parent, false);
            var s = o.AddComponent<SpriteRenderer>();
            s.sprite = Square;
            s.color = color;
            s.sortingOrder = order;
            o.transform.localScale = new Vector3(width, height, 1);
            return o;
        }

        public static GameObject Block(string name, Color color, float width, float height, int order, Transform parent, float x, float y)
        {
            var o = Block(name, color, width, height, order, parent);
            o.transform.position = new Vector3(x, y, 0);
            return o;
        }
    }
}
