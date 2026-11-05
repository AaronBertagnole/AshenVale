using System;
using UnityEngine;

namespace AshenVale
{
    // Visual framing only: gameplay coordinates, collision and camera size do not change.
    public static class PaintedMapFraming
    {
        [Serializable] public sealed class Entry
        {
            public string key;
            public bool completeGround = true;
            public float widthMultiplier = 0.8f;
            public bool overrideFloor;
            public float floorFromBottom;
        }
        [Serializable] public sealed class Settings
        {
            public float newBackdropWidthMultiplier = 0.8f;
            public Entry[] maps = new Entry[0];
        }
        static Settings settings;
        static Settings Config
        {
            get
            {
                if (settings != null) return settings;
                var text = Resources.Load<TextAsset>("Art/painted_map_framing");
                settings = text != null ? JsonUtility.FromJson<Settings>(text.text) : new Settings();
                if (settings == null) settings = new Settings();
                return settings;
            }
        }

        static Entry Find(MapDef def)
        {
            if (Config.maps != null)
                foreach (var e in Config.maps)
                    if (e != null && string.Equals(e.key, def.key, StringComparison.OrdinalIgnoreCase)) return e;
            return null;
        }

        public static bool UsesOwnGround(MapDef def)
        {
            // Original scene composition is a known-good reference. Never change it here.
            if (def.id == Maps.Emberhaven || !string.IsNullOrEmpty(def.scene)) return false;
            var entry = Find(def);
            if (entry != null) return entry.completeGround;
            // These original dynamic forests used separately authored ground strips.
            return def.key != "emberfields" && def.key != "mosswood" && def.key != "kingsroad";
        }

        public static void Apply(MapDef def, Sprite sprite, ref float width, ref float floor)
        {
            if (!UsesOwnGround(def)) return;
            var entry = Find(def);
            float multiplier = entry != null ? entry.widthMultiplier : Config.newBackdropWidthMultiplier;
            width *= Mathf.Clamp(multiplier, 0.25f, 1.5f);
            if (entry != null && entry.overrideFloor) floor = Mathf.Clamp01(entry.floorFromBottom);
            // Preserve coverage above and below the walking line at the lowest camera.
            // Shrinking a panorama without this guard can reveal blank bands at the top.
            float minX, maxX, minY, maxY;
            WorldView.CameraRange(def, 0f, out minX, out maxX, out minY, out maxY);
            float above = Mathf.Max(0f, minY + WorldView.ViewHalfHeight - Tuning.Ground);
            float below = Mathf.Max(0f, Tuning.Ground - (minY - WorldView.ViewHalfHeight));
            float aspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            float requiredHeight = Mathf.Max(above / Mathf.Max(0.01f, 1f - floor), below / Mathf.Max(0.01f, floor));
            width = Mathf.Max(width, (requiredHeight + 0.1f) * aspect);
            // Preserve coverage at the current Game-view aspect ratio; no horizontal stretching.
            var camera = Camera.main;
            if (camera != null)
                width = Mathf.Max(width, 2f * WorldView.ViewHalfHeight * camera.aspect + 0.2f);
        }
    }
}
