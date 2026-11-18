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
            if (!texture.LoadImage(data.bytes, true))
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
                    // Isolate the crystal head from the painted wake.
                    art = Sprite.Create(texture, new Rect(312, 64, 95, 95),
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
            float pulse = Mathf.Sin(age * 31f);
            core.transform.localRotation = Quaternion.Euler(0f, 0f, age * 610f);
            core.transform.localScale = Vector3.one * (1f + .13f * pulse);
            core.color = Color.Lerp(new Color(.62f, .87f, 1f), Color.white, .5f + .5f * pulse);
            if (wake) wake.startWidth = .14f + .035f * Mathf.Sin(age * 43f);
        }

        public bool ShedSliver()
        {
            if (Time.time < nextSliver) return false;
            nextSliver = Time.time + .04f;
            return true;
        }
    }
}
