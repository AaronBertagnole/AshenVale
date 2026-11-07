using System.Collections.Generic;
using UnityEngine;

namespace AshenVale
{
    // Inspect a copy, so imported art does not need Read/Write enabled or new import settings.
    public static class PaintedSpriteContact
    {
        static readonly Dictionary<string, float> feet = new Dictionary<string, float>();
        static readonly Dictionary<Sprite, Color> skies = new Dictionary<Sprite, Color>();

        static Texture2D Copy(Texture2D source)
        {
            var previous = RenderTexture.active;
            var target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Texture2D result = null;
            try
            {
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                result.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                result.Apply();
                return result;
            }
            catch { if (result != null) Object.Destroy(result); throw; }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); }
        }

        public static Color Sky(Sprite sprite)
        {
            if (sprite == null) return new Color(.36f, .26f, .56f);
            Color color;
            if (skies.TryGetValue(sprite, out color)) return color;
            Texture2D copy = null;
            try
            {
                copy = Copy(sprite.texture);
                Rect rect = sprite.textureRect;
                color = copy.GetPixel(Mathf.Clamp(Mathf.RoundToInt(rect.center.x), 0, copy.width - 1),
                                      Mathf.Clamp(Mathf.RoundToInt(rect.yMax - 2f), 0, copy.height - 1));
                color.a = 1f;
            }
            catch { color = new Color(.36f, .26f, .56f); }
            finally { if (copy != null) Object.Destroy(copy); }
            skies[sprite] = color;
            return color;
        }

        // Where the boots end in a frame, as a fraction of its height measured from the bottom: the lowest row with a
        // few opaque pixels in the central band of the frame. A thin spear tip at the edge, or a low spear head, does not
        // count (it is narrower than a boot, or outside the band). -1 when the frame cannot be read.
        static readonly Dictionary<Sprite, float> bootRows = new Dictionary<Sprite, float>();
        static float BootFraction(Sprite sprite)
        {
            if (sprite == null) return -1f;
            float result;
            if (bootRows.TryGetValue(sprite, out result)) return result;
            result = -1f;
            Texture2D copy = null;
            try
            {
                copy = Copy(sprite.texture);
                Rect rect = sprite.textureRect;
                int left = Mathf.RoundToInt(rect.xMin + rect.width * .30f), right = Mathf.RoundToInt(rect.xMin + rect.width * .62f);
                int bottom = Mathf.RoundToInt(rect.yMin), top = Mathf.RoundToInt(rect.yMax);
                for (int y = bottom; y < top && result < 0f; y++)
                {
                    int run = 0;
                    for (int x = left; x < right; x++) if (copy.GetPixel(x, y).a >= .5f) run++;
                    if (run >= 5) result = (y - rect.yMin + .5f) / rect.height;
                }
            }
            catch { result = -1f; }
            finally { if (copy != null) Object.Destroy(copy); }
            bootRows[sprite] = result;
            return result;
        }

        // How many sprite pixels this frame's boots sit above the idle pose's boots. A windup frame drawn mid-leap
        // reads as a hop when the body is placed by the idle contact row; lowering it by this much keeps the boots on the floor.
        public static float FootLiftPixels(string folder, Sprite current)
        {
            var idleFrames = Sprites.Hero(folder + "/idle");
            if (current == null || idleFrames.Length == 0) return 0f;
            float idle = BootFraction(idleFrames[0]), now = BootFraction(current);
            if (idle < 0f || now < 0f) return 0f;
            return Mathf.Clamp((now - idle) * current.rect.height, 0f, 48f);
        }

        public static float HeroOffset(string folder, Sprite current, float scaleY, float fallback)
        {
            if (current == null) return fallback;
            float fraction;
            if (!feet.TryGetValue(folder, out fraction))
            {
                fraction = -1f;
                var frames = Sprites.Hero(folder + "/idle");
                Texture2D copy = null;
                try
                {
                    if (frames.Length > 0)
                    {
                        var reference = frames[0];
                        copy = Copy(reference.texture);
                        Rect rect = reference.textureRect;
                        // Central body/boot region avoids a low spear tip at the frame edge.
                        int left = Mathf.RoundToInt(rect.xMin + rect.width * .25f);
                        int right = Mathf.RoundToInt(rect.xMin + rect.width * .75f);
                        int bottom = Mathf.RoundToInt(rect.yMin), top = Mathf.RoundToInt(rect.yMax);
                        for (int y = bottom; y < top && fraction < 0f; y++)
                            for (int x = left; x < right; x++)
                                if (copy.GetPixel(x, y).a >= .5f)
                                { fraction = (y - rect.yMin + .5f) / rect.height; break; }
                    }
                }
                catch { fraction = -1f; }
                finally { if (copy != null) Object.Destroy(copy); }
                feet[folder] = fraction;
            }
            if (fraction < 0f) return fallback;
            // One contact row per hero, not per animation pose: no foot-row jitter while walking.
            return -(fraction * current.rect.height - current.pivot.y) / current.pixelsPerUnit * scaleY;
        }
    }
}
