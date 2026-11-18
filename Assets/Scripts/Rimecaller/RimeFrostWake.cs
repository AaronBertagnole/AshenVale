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
        float nextSliver;
        public bool HasArt => art != null;

        void Awake()
        {
            if (!art)
            {
                var tex = RimeEffectArt.Load("Frost");
                if (tex) art = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(.74f,.46f), 240f, 0, SpriteMeshType.FullRect);
            }
            if (!art) return;
            GetComponent<SpriteRenderer>().sprite = art;
            transform.localScale = Vector3.one;
            var trail = GetComponent<TrailRenderer>();
            if (trail)
            {
                trail.time = .09f;
                trail.startWidth = .045f;
            }
        }

        public bool ShedSliver()
        {
            if (Time.time < nextSliver) return false;
            nextSliver = Time.time + .065f;
            return true;
        }
    }
}
