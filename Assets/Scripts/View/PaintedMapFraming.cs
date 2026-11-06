using System;
using UnityEngine;

namespace AshenVale
{
    // Recovery shim: retain call-site compatibility, disable V1/V2/V3 overrides.
    // Restart Play mode after replacing this file so old generated objects are rebuilt.
    public static class PaintedMapFraming
    {
        // Kept so the previously installed V3 editor script still compiles.
        [Serializable] public sealed class Entry
        {
            public string key;
            public bool completeGround = true;
            public bool extendSky;
            public float widthMultiplier = .8f;
            public bool overrideFloor;
            public float floorFromBottom;
        }
        [Serializable] public sealed class Settings
        {
            public float referenceHeight = 724f;
            public float referenceWidth = 2172f;
            public float referenceWorldWidth = 38f;
            public float newBackdropWidthMultiplier = .8f;
            public Entry[] maps = new Entry[0];
        }

        public static bool UsesOwnGround(MapDef def) { return false; }
        public static bool LocksFloor(MapDef def) { return false; }
        public static bool ShowPlaceholderProps(MapDef def, bool paintedScene, bool hasPicture)
        {
            return !paintedScene;
        }
        public static void AddSkyFill(MapDef def, Transform root) { }
        public static void Apply(MapDef def, Sprite sprite, ref float width, ref float floor) { }
    }
}
