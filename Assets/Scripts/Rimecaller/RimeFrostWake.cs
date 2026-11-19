using UnityEngine;

namespace AshenVale
{
    internal static class RimeEffectArt
    {
        internal static Texture2D Load(string name)
        {
            var data = Resources.Load<TextAsset>("RimePolish/" + name);
            if (!data) return null;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(data.bytes, name != "Frost"))
            {
                Object.Destroy(texture);
                return null;
            }
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }
    }

    public sealed class RimeFrostWake : MonoBehaviour
    {
        static Sprite art;
        SpriteRenderer core;
        TrailRenderer wake;
        float nextSliver;
        float age;

        public bool HasArt => core != null;

        void Awake()
        {
            if (!art)
            {
                var texture = RimeEffectArt.Load("Frost");
                if (texture)
                {
                    const int size = 96;
                    const int centerX = 359;
                    const int centerY = 116;
                    var pixels = texture.GetPixels(centerX - size / 2, centerY - size / 2, size, size);
                    Object.Destroy(texture);
                    for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float radius = new Vector2(x + .5f - size / 2, y + .5f - size / 2).magnitude;
                        float edge = Mathf.InverseLerp(31f, 46f, radius);
                        pixels[y * size + x].a *= 1f - Mathf.SmoothStep(0f, 1f, edge);
                    }
                    var coreTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    coreTexture.wrapMode = TextureWrapMode.Clamp;
                    coreTexture.SetPixels(pixels);
                    coreTexture.Apply(false, true);
                    art = Sprite.Create(coreTexture, new Rect(0, 0, size, size),
                        new Vector2(.5f, .5f), 300f, 0, SpriteMeshType.FullRect);
                }
            }
            if (!art) return;

            var original = GetComponent<SpriteRenderer>();
            transform.localScale = Vector3.one;
            var crystal = new GameObject("Spinning frost crystal");
            crystal.transform.SetParent(transform, false);
            core = crystal.AddComponent<SpriteRenderer>();
            core.sprite = art;
            core.sharedMaterial = original.sharedMaterial;
            core.sortingLayerID = original.sortingLayerID;
            core.sortingOrder = original.sortingOrder;
            original.enabled = false;

            wake = GetComponent<TrailRenderer>();
            if (wake)
            {
                wake.time = .125f;
                wake.startWidth = .14f;
                wake.startColor = new Color(.52f, .9f, 1f, .75f);
                wake.endColor = new Color(.25f, .63f, 1f, 0f);
            }
        }

        void LateUpdate()
        {
            if (!core) return;
            age += Time.deltaTime;
            float pulse = Mathf.Sin(age * 24f);
            float launch = Mathf.Exp(-age * 15f);
            core.transform.localRotation = Quaternion.Euler(0f, 0f, 8f * Mathf.Sin(age * 17f));
            core.transform.localScale = new Vector3(1f + .28f * launch, 1f - .14f * launch + .05f * pulse, 1f);
            core.color = Color.Lerp(new Color(.62f, .87f, 1f), Color.white, .5f + .5f * pulse);
            if (wake) wake.startWidth = .14f + .025f * Mathf.Sin(age * 32f);
        }

        public bool ShedSliver()
        {
            if (Time.time < nextSliver) return false;
            nextSliver = Time.time + .04f;
            return true;
        }
    }
}
