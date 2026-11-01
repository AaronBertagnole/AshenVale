using UnityEngine;

namespace AshenVale
{
    public enum Drift { Float, Fall, Rise }

    // An animated light drawn over part of a map's picture with the MapEffect shader.
    public struct EffectDef
    {
        public string name;
        public float x, y, width, height;   // centre and size in pixels of the picture (x from the left, y from the top)
        public Color color;
        public int mode;                    // 0 portal ripples, 1 flowing water, 2 drifting mist, 3 fire flicker
        public bool distant;                // on the distant (_back) picture, sliding with it, instead of the front one
        public float strength;              // 1 = normal (0 reads as 1); raise it where the paint underneath is bright
    }

    // A group of drifting specks: motes, leaves, pollen, ash, sparks, smoke.
    public struct AmbientDef
    {
        public string name;
        public Drift drift;
        // aroundView: fills the screen wherever the camera goes; sizes, sway and speed in world units.
        // Otherwise it lives at one spot of the front picture (x, y, width, height), with sizes, sway and speed in
        // that picture's pixels, so it lines up with the painting (a chimney, a forge).
        public bool aroundView;
        public float x, y, width, height;
        public int count;
        public Color tint;
        public float sizeMin, sizeMax;
        public float speed;                 // rise / fall speed per second
        public float sway;                  // how far each speck sways from side to side
        public float rate;                  // how fast it sways (radians per second)
        public bool leaf;                   // a tumbling leaf instead of a glowing speck
        public int order;                   // sorting order (heroes are 10-11)
    }

    // The living extras of a map's look: how its distant picture slides, its animated lights and drifting specks.
    // Pictures: a painted map can come as two layers, the front (Art/Scenes/<key>.png or Art/Backdrops/<key>.png,
    // see through where the distance shows) and the distance behind it (the same name + "_back"), which slides
    // more slowly for depth. Effect positions came from the owner's "All Maps" pack, converted to picture pixels
    // and checked against the pictures (the Mosswood stream and Emberfields falls are on the distant layer).
    public sealed class MapArtDef
    {
        // How far the distant picture slides compared with the front one: 1 = as much (a picture as wide as the
        // camera's travel then looks painted on the world), 0 = it stays with the camera.
        public float backTravel = .5f;
        public EffectDef[] effects = new EffectDef[0];
        public AmbientDef[] ambient = new AmbientDef[0];
    }

    public static class MapArt
    {
        static MapArtDef[] all;

        public static MapArtDef For(int map)
        {
            if (all == null) all = Build();
            return map >= 0 && map < all.Length && all[map] != null ? all[map] : new MapArtDef { ambient = new[] { Motes() } };
        }

        static MapArtDef[] Build()
        {
            var a = new MapArtDef[Maps.All.Length];
            var autumn = new MapArtDef
            {
                effects = new[]
                {
                    Fx("Upper falls", 1515, 360, 40, 90, new Color(.65f, .9f, 1f), 1, true),
                    Fx("Great falls", 1665, 442, 34, 100, new Color(.65f, .9f, 1f), 1, true),
                    Fx("Side falls", 1745, 420, 26, 64, new Color(.65f, .9f, 1f), 1, true),
                },
                ambient = new[] { Motes(), Leaves(55, new Color(.95f, .3f, .05f, 1f)) },
            };
            a[Maps.Emberfields] = autumn;
            a[Maps.AmberleafTrail] = autumn;        // shows the Emberfields pictures (mirrored) for now
            a[Maps.Emberhaven] = new MapArtDef
            {
                backTravel = .55f,
                effects = new[]
                {
                    Fx("Waystone glow", 1060, 425, 165, 155, new Color(1f, .7f, .28f), 0, false, 1.3f),
                    Fx("Waystone flame", 1081, 426, 74, 143, new Color(1f, .6f, .1f), 3),
                    Fx("Forge glow", 1855, 478, 95, 106, new Color(1f, .4f, .08f), 3),
                },
                ambient = new[] { Motes(), Leaves(30, new Color(.95f, .3f, .05f, 1f)), Smoke(503, 235), Smoke(1913, 256) },
            };
            a[Maps.MossgateHollow] = new MapArtDef
            {
                effects = new[]
                {
                    Fx("Gate glow", 1059, 404, 191, 296, new Color(.6f, 1f, .35f), 0, false, 1.4f),
                    Fx("Waterfall", 1637, 325, 74, 212, new Color(.6f, .9f, 1f), 1, true),
                    Fx("Mist", 1086, 521, 1803, 116, new Color(.6f, .85f, .8f), 2),
                },
                ambient = new[] { Motes() },
            };
            a[Maps.Mosswood] = new MapArtDef
            {
                effects = new[]
                {
                    Fx("Stream", 1405, 495, 70, 150, new Color(.6f, .95f, 1f), 1, true),     // the cascade is in the distance
                    Fx("Mist", 1086, 510, 1803, 148, new Color(.65f, .85f, .8f), 2),
                },
                ambient = new[] { Motes() },
            };
            a[Maps.Kingsroad] = new MapArtDef
            {
                effects = new[] { Fx("River shimmer", 1765, 436, 636, 58, new Color(.6f, .9f, 1f), 1, true) },
                ambient = new[] { Motes(), Pollen() },
            };
            // Cinderford's front is two pictures joined (Tools/stitch_pictures.py): the gate, shops and smithy, then
            // the house, waystone and east gate from x 1971. One distant castle hill slides behind the whole street.
            a[Maps.Cinderford] = new MapArtDef
            {
                backTravel = 1f,
                effects = new[]
                {
                    Fx("Waystone glow", 3057, 429, 156, 206, new Color(.1f, 1f, .8f), 0),
                    Fx("Forge glow", 1964, 489, 79, 85, new Color(1f, .45f, .08f), 3),
                },
                ambient = new[]
                {
                    Motes(),
                    Sparks("Forge sparks", 1964, 452, 85, 138, 22, new Color(1f, .5f, .1f, .8f)),
                    Smoke(2006, 128),
                    Sparks("Waystone sparks", 3056, 425, 159, 212, 24, new Color(.2f, 1f, .85f, .75f)),
                },
            };
            a[Maps.AshenMarsh] = new MapArtDef { ambient = new[] { Ash() } };
            a[Maps.ShaleCliffs] = new MapArtDef { ambient = new[] { Snow() } };
            return a;
        }

        static EffectDef Fx(string name, float x, float y, float width, float height, Color color, int mode, bool distant = false, float strength = 1f)
        {
            return new EffectDef { name = name, x = x, y = y, width = width, height = height, color = color, mode = mode, distant = distant, strength = strength };
        }

        // Specks that fill the screen (world units).
        static AmbientDef Motes()
        {
            return new AmbientDef { name = "Motes", drift = Drift.Float, aroundView = true, count = 40, tint = new Color(1f, .9f, .55f, .9f),
                                    sizeMin = .07f, sizeMax = .18f, sway = .35f, rate = .35f, order = 14 };
        }

        static AmbientDef Pollen()
        {
            return new AmbientDef { name = "Pollen", drift = Drift.Float, aroundView = true, count = 30, tint = new Color(1f, .95f, .6f, .6f),
                                    sizeMin = .05f, sizeMax = .12f, sway = .35f, rate = .3f, order = 14 };
        }

        static AmbientDef Leaves(int count, Color tint)
        {
            return new AmbientDef { name = "Falling leaves", drift = Drift.Fall, aroundView = true, count = count, tint = tint,
                                    sizeMin = .14f, sizeMax = .26f, speed = .7f, sway = .4f, rate = .35f, leaf = true, order = 14 };
        }

        static AmbientDef Snow()
        {
            return new AmbientDef { name = "Snow", drift = Drift.Fall, aroundView = true, count = 45, tint = new Color(1f, 1f, 1f, .8f),
                                    sizeMin = .05f, sizeMax = .1f, speed = .4f, sway = .3f, rate = .3f, order = 14 };
        }

        static AmbientDef Ash()
        {
            return new AmbientDef { name = "Ash", drift = Drift.Fall, aroundView = true, count = 40, tint = new Color(.78f, .76f, .72f, .55f),
                                    sizeMin = .05f, sizeMax = .11f, speed = .25f, sway = .3f, rate = .25f, order = 14 };
        }

        // Specks at one spot of the front picture (picture pixels).
        static AmbientDef Smoke(float x, float y)
        {
            return new AmbientDef { name = "Chimney smoke", drift = Drift.Rise, x = x, y = y, width = 53, height = 170, count = 10,
                                    tint = new Color(.8f, .83f, .85f, .13f), sizeMin = 41, sizeMax = 81, speed = 14, sway = 23, rate = .13f, order = 4 };
        }

        static AmbientDef Sparks(string name, float x, float y, float width, float height, int count, Color tint)
        {
            return new AmbientDef { name = name, drift = Drift.Rise, x = x, y = y, width = width, height = height, count = count, tint = tint,
                                    sizeMin = 4, sizeMax = 10, speed = 37, sway = 23, rate = .35f, order = 14 };
        }
    }
}
