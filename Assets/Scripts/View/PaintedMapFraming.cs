using System;
using UnityEngine;

namespace AshenVale
{
    // V3: ground is a world anchor, never a viewport-fitting constraint.
    public static class PaintedMapFraming
    {
        [Serializable] public sealed class Entry
        {
            public string key;
            public bool completeGround = true;
            public float floorFromBottom;
        }
        [Serializable] public sealed class Settings
        {
            public float referenceHeight = 724f;
            public float referenceWidth = 2172f;
            public float referenceWorldWidth = 38f;
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
            if (def.id == Maps.Emberhaven || !string.IsNullOrEmpty(def.scene)) return false;
            var entry = Find(def);
            // Unknown future images retain their authored handling until calibrated.
            return entry != null && entry.completeGround;
        }
        public static bool LocksFloor(MapDef def) { return UsesOwnGround(def); }
        public static bool ShowPlaceholderProps(MapDef def, bool paintedScene, bool hasPicture)
        {
            return !paintedScene && !(hasPicture && def.kind == MapKind.Town && UsesOwnGround(def));
        }
        public static void AddSkyFill(MapDef def, Transform root)
        {
            if (!LocksFloor(def)) return;
            string path = "Art/Backdrops/" + def.key;
            var sprite = Sprites.Art(path);
            if (sprite == null && !string.IsNullOrEmpty(def.backdrop))
            { path = def.backdrop; sprite = Sprites.Art(path); }
            var far = Sprites.Art(path + "_back");
            // Transparent forest foregrounds must sample their opaque distance layer.
            Sprites.Block("Painted sky extension", PaintedSpriteContact.Sky(far != null ? far : sprite),
                def.columns + 100f, def.rows + 100f, MapScenery.SkyOrder - 1, root,
                def.columns * .5f, Tuning.Ground + def.rows * .5f);
        }
        public static void Apply(MapDef def, Sprite sprite, ref float width, ref float floor)
        {
            if (!UsesOwnGround(def) || sprite == null) return;
            var entry = Find(def);
            floor = Mathf.Clamp(entry.floorFromBottom, .01f, .99f);
            // Equal source-height compositions get the same world height, even when
            // Unity's importer downsizes their texture. No vertical coverage enlargement.
            float height = Config.referenceWorldWidth * Config.referenceHeight / Mathf.Max(1f, Config.referenceWidth);
            width = height * sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            // Only unusually wide displays require enlargement to cover the screen.
            var camera = Camera.main;
            if (camera != null)
                width = Mathf.Max(width, 2f * WorldView.ViewHalfHeight * camera.aspect + .2f);
        }
    }
}
