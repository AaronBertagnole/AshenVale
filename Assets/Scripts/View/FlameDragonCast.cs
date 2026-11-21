using UnityEngine;

namespace AshenVale
{
    public sealed class FlameDragonCast : MonoBehaviour
    {
        static Sprite art;
        static bool loaded;
        public static Sprite Art
        {
            get
            {
                if (loaded) return art;
                loaded = true;
                var data = Resources.Load<TextAsset>("DragonfireVfx/FlameDragon");
                if (!data) return null;
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(data.bytes, true))
                {
                    Destroy(texture);
                    return null;
                }
                texture.wrapMode = TextureWrapMode.Clamp;
                art = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(.37f, .42f), 100f, 0, SpriteMeshType.FullRect);
                return art;
            }
        }

        SpriteRenderer visual;
        Vector3 origin;
        float age, facing, size;

        public void Begin(Sprite sprite, float casterX, float floor, int direction, int order)
        {
            facing = direction < 0 ? -1f : 1f;
            origin = new Vector3(casterX + facing * 1.45f, floor + 1.15f, 0f);
            size = Mathf.Min(2.25f / sprite.bounds.size.y,
                (Abilities.DragonfireReach - 2.25f) / sprite.bounds.max.x);
            visual = gameObject.AddComponent<SpriteRenderer>();
            visual.sprite = sprite;
            visual.sortingOrder = order;
            Draw();
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= 1.2f) Destroy(gameObject);
            else Draw();
        }

        void Draw()
        {
            float launch = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / .18f));
            float surge = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / .5f));
            float scale = size * Mathf.Lerp(.64f, 1f, launch);
            transform.localScale = new Vector3(facing * scale, scale, 1f);
            transform.position = origin + new Vector3(facing * .8f * surge, .12f * surge, 0f);
            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.43f, 1.2f, age));
            visual.color = new Color(1f, 1f, 1f, fade * Mathf.Lerp(.45f, 1f, launch));
        }
    }
}
