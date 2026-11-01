using System.Collections.Generic;
using UnityEngine;

namespace AshenVale
{
    // A backdrop layer that follows the camera by (fx, fy): 1 = fixed to the screen (sky), 0 = fixed to the world.
    // A panorama layer is one painted picture (width x height units, origin at its bottom-left) that WorldView
    // slides so it always fills the view: its left edge shows at the map's left end, its right edge at the right end.
    public sealed class ParallaxLayer
    {
        public Transform transform;
        public float fx, fy;
        public bool panorama;
        public float width, height, floor;
    }

    // Everything drawn for one map: a root (switched on while the local player is on the map) and its backdrop layers.
    public sealed class MapVisual
    {
        public GameObject root;
        public readonly List<ParallaxLayer> layers = new List<ParallaxLayer>();
        public float cameraLowY;    // backdrop layers are laid out as seen with the camera at its lowest point
    }

    // Builds the look of a map from its MapDef and theme. Every piece first looks for real art under
    // Resources/Art (see ART.md) and falls back to placeholder shapes, so art can be dropped in one file at a time.
    //
    //   Art/Scenes/<map>          a whole painted scene for one map (MapDef.scene), replacing the backdrop and ground
    //   Art/Backdrops/<map>       a painted backdrop for a wider map (MapDef.backdrop), replacing sky / far / mid
    //   Art/Themes/<theme>/sky, far, mid, ground, ground_back, platform_left, platform_mid, platform_right, ladder
    //   Art/Props/portal, return_portal, waystone, merchant, smithy
    public static class MapScenery
    {
        // Sorting orders, back to front (ladders stand behind platforms). Monsters 7, drops 9, heroes 10-11,
        // slashes 12, arrows 13 (WorldView).
        public const int SkyOrder = -40, FarOrder = -30, MidOrder = -20, BackDecorOrder = -10, GroundOrder = 0,
                         PlatformOrder = 2, LadderOrder = 0, PropOrder = 5, PortalOrder = 6;

        sealed class Theme
        {
            public Color skyBottom, skyTop, far, farDark, mid, midDark, groundBody, groundTop, platformBody, platformTop, accent;
            public string farStyle, midStyle;
            public bool outdoors = true;
            public Color ladderRail = new Color(.55f, .35f, .19f), ladderRung = new Color(.72f, .51f, .28f);
            public Color hanging = Color.clear;     // what hangs under platforms (clear: a darker shade of the platform)
            public Color[] flecks;                  // little leaves scattered on platform tops (null: none)
        }

        static Theme ThemeFor(string name)
        {
            switch (name)
            {
                case "town": return new Theme
                {
                    skyBottom = new Color(.99f, .80f, .58f), skyTop = new Color(.42f, .56f, .82f),
                    far = new Color(.55f, .62f, .78f), farDark = new Color(.47f, .54f, .70f),
                    mid = new Color(.66f, .52f, .44f), midDark = new Color(.72f, .34f, .27f),
                    groundBody = new Color(.40f, .33f, .28f), groundTop = new Color(.76f, .64f, .46f),
                    platformBody = new Color(.52f, .36f, .23f), platformTop = new Color(.80f, .60f, .36f),
                    accent = new Color(1f, .78f, .42f), farStyle = "hills", midStyle = "houses",
                };
                case "autumn": return new Theme     // Emberfields: colours taken from its painted backdrop
                {
                    skyBottom = new Color(.97f, .88f, .70f), skyTop = new Color(.45f, .66f, .90f),
                    far = new Color(.80f, .42f, .22f), farDark = new Color(.68f, .30f, .16f),
                    mid = new Color(.75f, .23f, .11f), midDark = new Color(.37f, .28f, .18f),
                    groundBody = new Color(.25f, .24f, .15f), groundTop = new Color(.90f, .61f, .33f),
                    platformBody = new Color(.33f, .32f, .21f), platformTop = new Color(.90f, .61f, .33f),
                    accent = new Color(.98f, .80f, .51f), farStyle = "hills", midStyle = "roundtrees",
                    ladderRail = new Color(.37f, .28f, .18f), ladderRung = new Color(.58f, .42f, .26f),
                    hanging = new Color(.36f, .46f, .18f),
                    flecks = new[] { new Color(.75f, .23f, .11f), new Color(.93f, .43f, .14f), new Color(.98f, .80f, .51f), new Color(.71f, .36f, .18f) },
                };
                case "forest": return new Theme
                {
                    skyBottom = new Color(.62f, .84f, .74f), skyTop = new Color(.22f, .46f, .50f),
                    far = new Color(.27f, .48f, .43f), farDark = new Color(.22f, .41f, .37f),
                    mid = new Color(.17f, .36f, .28f), midDark = new Color(.30f, .22f, .16f),
                    groundBody = new Color(.27f, .21f, .16f), groundTop = new Color(.33f, .62f, .34f),
                    platformBody = new Color(.40f, .28f, .18f), platformTop = new Color(.40f, .70f, .38f),
                    accent = new Color(.95f, .92f, .55f), farStyle = "pines", midStyle = "bigtrees",
                };
                case "cave": return new Theme
                {
                    skyBottom = new Color(.10f, .10f, .18f), skyTop = new Color(.04f, .04f, .09f),
                    far = new Color(.17f, .17f, .27f), farDark = new Color(.13f, .13f, .21f),
                    mid = new Color(.24f, .22f, .33f), midDark = new Color(.19f, .17f, .27f),
                    groundBody = new Color(.18f, .16f, .23f), groundTop = new Color(.40f, .36f, .50f),
                    platformBody = new Color(.29f, .27f, .38f), platformTop = new Color(.52f, .48f, .64f),
                    accent = new Color(.55f, .88f, 1f), farStyle = "stalactites", midStyle = "crystals", outdoors = false,
                };
                case "road": return new Theme
                {
                    skyBottom = new Color(.99f, .88f, .64f), skyTop = new Color(.50f, .68f, .90f),
                    far = new Color(.64f, .64f, .78f), farDark = new Color(.56f, .56f, .70f),
                    mid = new Color(.66f, .72f, .36f), midDark = new Color(.55f, .40f, .26f),
                    groundBody = new Color(.48f, .37f, .26f), groundTop = new Color(.80f, .68f, .45f),
                    platformBody = new Color(.50f, .40f, .28f), platformTop = new Color(.62f, .76f, .36f),
                    accent = new Color(1f, .93f, .62f), farStyle = "mountains", midStyle = "fields",
                };
                case "rivertown": return new Theme
                {
                    skyBottom = new Color(.88f, .84f, .94f), skyTop = new Color(.42f, .52f, .78f),
                    far = new Color(.52f, .58f, .74f), farDark = new Color(.45f, .51f, .66f),
                    mid = new Color(.58f, .55f, .60f), midDark = new Color(.36f, .44f, .62f),
                    groundBody = new Color(.36f, .34f, .38f), groundTop = new Color(.64f, .60f, .58f),
                    platformBody = new Color(.48f, .34f, .24f), platformTop = new Color(.74f, .58f, .38f),
                    accent = new Color(.60f, .80f, 1f), farStyle = "hills", midStyle = "houses",
                };
                case "marsh": return new Theme
                {
                    skyBottom = new Color(.66f, .70f, .60f), skyTop = new Color(.32f, .38f, .36f),
                    far = new Color(.40f, .44f, .40f), farDark = new Color(.34f, .38f, .34f),
                    mid = new Color(.32f, .40f, .29f), midDark = new Color(.27f, .24f, .20f),
                    groundBody = new Color(.24f, .22f, .18f), groundTop = new Color(.42f, .44f, .29f),
                    platformBody = new Color(.35f, .30f, .24f), platformTop = new Color(.48f, .54f, .33f),
                    accent = new Color(.80f, .86f, .70f), farStyle = "deadtrees", midStyle = "reeds",
                };
                default: return new Theme   // "meadow"
                {
                    skyBottom = new Color(.74f, .90f, 1f), skyTop = new Color(.36f, .62f, .93f),
                    far = new Color(.58f, .78f, .64f), farDark = new Color(.50f, .70f, .57f),
                    mid = new Color(.34f, .60f, .39f), midDark = new Color(.45f, .32f, .22f),
                    groundBody = new Color(.45f, .32f, .22f), groundTop = new Color(.48f, .76f, .34f),
                    platformBody = new Color(.53f, .38f, .26f), platformTop = new Color(.56f, .82f, .37f),
                    accent = new Color(1f, .95f, .70f), farStyle = "hills", midStyle = "roundtrees",
                };
            }
        }

        public static MapVisual Build(MapDef def)
        {
            var visual = new MapVisual { root = new GameObject("Map: " + def.name) };
            var root = visual.root.transform;
            var theme = ThemeFor(def.theme);
            var rng = new System.Random(1000 + def.id * 37);
            float minX, maxX, minY, maxY;
            WorldView.CameraRange(def, 0f, out minX, out maxX, out minY, out maxY);
            visual.cameraLowY = minY;
            string art = "Art/Themes/" + def.theme + "/";

            // A painted scene already shows the sky, ground, houses, waystone and shops: draw only what it lacks.
            // A painted backdrop replaces the sky and distant layers; the ground and platforms are still drawn.
            bool painted = BuildScene(root, def);
            if (!painted)
            {
                if (!BuildBackdrop(visual, def))
                {
                    Sky(visual, def, theme, art);
                    Far(visual, def, theme, art, rng);
                    Mid(visual, def, theme, art, rng);
                }
                Ground(root, def, theme, art, rng);
            }
            foreach (var plat in def.platforms) BuildPlatform(root, plat, theme, art, rng);
            foreach (var ladder in def.ladders) BuildLadder(root, ladder, theme, art);
            foreach (var portal in def.portals) BuildPortal(root, portal.x, portal.y, false);
            if (def.kind == MapKind.Town)
            {
                BuildPortal(root, def.returnX, def.returnY, true);
                BuildWaystone(root, def.waystoneX, def.waystoneY, !painted);
                BuildMerchant(root, def.merchantX, def.merchantY, !painted);
            }
            if (def.hasSmithy) BuildSmithy(root, def.smithyX, def.smithyY, !painted);
            return visual;
        }

        // The map's painted scene, stretched to the map's width with its walking line on the ground. False if the map
        // has none or its picture isn't in the project.
        static bool BuildScene(Transform root, MapDef def)
        {
            if (string.IsNullOrEmpty(def.scene)) return false;
            var sprite = Sprites.Art(def.scene);
            if (sprite == null) return false;
            var size = sprite.bounds.size;
            float scale = def.columns / size.x, bottom = Tuning.Ground - def.sceneFloor * size.y * scale;
            // Placed from the sprite's own bounds, so it lines up whatever pivot it was imported with.
            var o = Sprites.Picture("Scene", sprite, SkyOrder, root,
                                    def.columns * .5f - sprite.bounds.center.x * scale, bottom - sprite.bounds.min.y * scale);
            o.transform.localScale = new Vector3(scale, scale, 1);
            return true;
        }

        // The map's painted backdrop as a panorama layer (WorldView moves it). False if none or not in the project.
        static bool BuildBackdrop(MapVisual visual, MapDef def)
        {
            if (string.IsNullOrEmpty(def.backdrop)) return false;
            var sprite = Sprites.Art(def.backdrop);
            if (sprite == null) return false;
            var t = new GameObject("Backdrop").transform;
            t.SetParent(visual.root.transform, false);
            var size = sprite.bounds.size;
            float scale = def.backdropWidth / size.x;
            var o = Sprites.Picture("Backdrop art", sprite, SkyOrder, t, 0, 0);
            o.transform.localScale = new Vector3(scale, scale, 1);
            o.transform.localPosition = new Vector3(-sprite.bounds.min.x * scale, -sprite.bounds.min.y * scale, 0);   // layer origin = picture's bottom-left
            visual.layers.Add(new ParallaxLayer { transform = t, panorama = true, width = def.backdropWidth, height = size.y * scale, floor = def.backdropFloor });
            return true;
        }

        static Transform Layer(MapVisual visual, string name, float fx, float fy)
        {
            var t = new GameObject(name).transform;
            t.SetParent(visual.root.transform, false);
            visual.layers.Add(new ParallaxLayer { transform = t, fx = fx, fy = fy });
            return t;
        }

        // ---------------------------------------------------------------- backdrop

        static void Sky(MapVisual visual, MapDef def, Theme theme, string art)
        {
            var layer = Layer(visual, "Sky", 1f, 1f);
            float h = WorldView.ViewHalfHeight * 2f + 2f, w = 50f, y = visual.cameraLowY;
            var sprite = Sprites.Art(art + "sky");
            if (sprite != null)
            {
                var o = Sprites.Picture("Sky art", sprite, SkyOrder, layer, 0, y);
                float scale = Mathf.Max(w / sprite.bounds.size.x, h / sprite.bounds.size.y);   // cover the screen
                o.transform.localScale = new Vector3(scale, scale, 1);
                return;
            }
            Sprites.Band("Sky", theme.skyBottom, theme.skyTop, w, h, SkyOrder, layer, 0, y);
            if (theme.outdoors)
            {
                Sprites.Blob("Sun glow", new Color(theme.accent.r, theme.accent.g, theme.accent.b, .25f), 4.2f, 4.2f, SkyOrder + 1, layer, 5.5f, y + 2.6f);
                Sprites.Blob("Sun", new Color(theme.accent.r, theme.accent.g, theme.accent.b, .9f), 1.7f, 1.7f, SkyOrder + 2, layer, 5.5f, y + 2.6f);
            }
        }

        static void Far(MapVisual visual, MapDef def, Theme theme, string art, System.Random rng)
        {
            const float fx = .85f, fy = .9f;
            var layer = Layer(visual, "Far", fx, fy);
            float x0 = -18f, x1 = def.columns * (1f - fx) + 18f, G = Tuning.Ground;
            var sprite = Sprites.Art(art + "far");
            if (sprite != null) { Sprites.Tiled("Far art", sprite, x1 - x0, sprite.bounds.size.y, FarOrder, layer, (x0 + x1) * .5f, G - 1f); return; }

            Color cloud = new Color(1, 1, 1, .75f);
            if (theme.outdoors)
                for (float x = x0; x < x1; x += 7f + (float)rng.NextDouble() * 6f)
                {
                    float cy = G + 7f + (float)rng.NextDouble() * 4f;
                    for (int i = 0; i < 3; i++)
                        Sprites.Blob("Cloud", cloud, 2.2f + i * .4f, 1.1f + (i == 1 ? .5f : 0), FarOrder - 2, layer, x + i * 1.1f, cy + (i == 1 ? .25f : 0));
                }

            switch (theme.farStyle)
            {
                case "pines":
                    for (float x = x0; x < x1; x += 1.1f + (float)rng.NextDouble() * 1.2f)
                    {
                        float h = 5f + (float)rng.NextDouble() * 5f;
                        Sprites.Blob("Far pine", rng.Next(2) == 0 ? theme.far : theme.farDark, 1.6f + (float)rng.NextDouble(), h, FarOrder, layer, x, G + h * .3f);
                    }
                    break;
                case "stalactites":
                    for (float x = x0; x < x1; x += 1.2f + (float)rng.NextDouble() * 2f)
                    {
                        float h = 2f + (float)rng.NextDouble() * 4f;
                        Sprites.Blob("Stalactite", theme.far, .8f + (float)rng.NextDouble() * .8f, h * 2f, FarOrder, layer, x, G + 11.5f);
                        Sprites.Blob("Cave rock", theme.farDark, 3f + (float)rng.NextDouble() * 3f, 3f + (float)rng.NextDouble() * 2f, FarOrder, layer, x, G);
                        if (rng.Next(3) == 0)
                            Sprites.Blob("Glow", new Color(theme.accent.r, theme.accent.g, theme.accent.b, .5f), .25f, .25f, FarOrder + 1, layer,
                                         x + .5f, G + 2f + (float)rng.NextDouble() * 6f);
                    }
                    break;
                case "mountains":
                    for (float x = x0; x < x1; x += 5f + (float)rng.NextDouble() * 4f)
                    {
                        float h = 8f + (float)rng.NextDouble() * 6f;
                        Sprites.Blob("Mountain", rng.Next(2) == 0 ? theme.far : theme.farDark, 7f + (float)rng.NextDouble() * 4f, h, FarOrder, layer, x, G + h * .1f);
                        Sprites.Blob("Snow cap", new Color(.95f, .95f, 1f, .85f), 1.8f, 1.2f, FarOrder + 1, layer, x, G + h * .55f);
                    }
                    break;
                case "deadtrees":
                    Sprites.Blob("Far bank", theme.farDark, (x1 - x0) + 10f, 4f, FarOrder, layer, (x0 + x1) * .5f, G - .5f);
                    for (float x = x0; x < x1; x += 2f + (float)rng.NextDouble() * 3f)
                    {
                        float h = 3f + (float)rng.NextDouble() * 3f;
                        Sprites.Block("Dead trunk", theme.far, .22f, h, FarOrder + 1, layer, x, G + h * .5f);
                        var branch = Sprites.Block("Dead branch", theme.far, .14f, h * .4f, FarOrder + 1, layer, x + .35f, G + h * .75f);
                        branch.transform.rotation = Quaternion.Euler(0, 0, -35f);
                    }
                    Sprites.Band("Mist", new Color(theme.accent.r, theme.accent.g, theme.accent.b, .45f), new Color(theme.accent.r, theme.accent.g, theme.accent.b, 0f),
                                 (x1 - x0) + 10f, 2.5f, FarOrder + 2, layer, (x0 + x1) * .5f, G + 1f);
                    break;
                default:   // hills
                    for (float x = x0; x < x1; x += 4f + (float)rng.NextDouble() * 4f)
                        Sprites.Blob("Hill", rng.Next(2) == 0 ? theme.far : theme.farDark, 9f + (float)rng.NextDouble() * 6f,
                                     5f + (float)rng.NextDouble() * 4f, FarOrder, layer, x, G + (float)rng.NextDouble());
                    break;
            }
        }

        static void Mid(MapVisual visual, MapDef def, Theme theme, string art, System.Random rng)
        {
            const float fx = .6f, fy = .7f;
            var layer = Layer(visual, "Mid", fx, fy);
            float x0 = -18f, x1 = def.columns * (1f - fx) + 18f, G = Tuning.Ground;
            var sprite = Sprites.Art(art + "mid");
            if (sprite != null) { Sprites.Tiled("Mid art", sprite, x1 - x0, sprite.bounds.size.y, MidOrder, layer, (x0 + x1) * .5f, G - .5f); return; }

            switch (theme.midStyle)
            {
                case "houses":
                    for (float x = x0; x < x1; x += 3.4f + (float)rng.NextDouble() * 1.5f)
                    {
                        float w = 2.4f + (float)rng.NextDouble() * .8f, h = 2f + (float)rng.NextDouble() * 1.2f;
                        Sprites.Block("House", theme.mid, w, h, MidOrder + 1, layer, x, G + h * .5f);
                        var roof = Sprites.Block("Roof", theme.midDark, w * .74f, w * .74f, MidOrder, layer, x, G + h);
                        roof.transform.rotation = Quaternion.Euler(0, 0, 45f);
                        Sprites.Block("Window", theme.accent, .45f, .55f, MidOrder + 2, layer, x - w * .22f, G + h * .55f);
                        Sprites.Block("Window", theme.accent, .45f, .55f, MidOrder + 2, layer, x + w * .22f, G + h * .55f);
                    }
                    break;
                case "bigtrees":
                    for (float x = x0; x < x1; x += 3f + (float)rng.NextDouble() * 3f)
                    {
                        float h = 4f + (float)rng.NextDouble() * 3f;
                        Sprites.Block("Trunk", theme.midDark, .6f, h, MidOrder, layer, x, G + h * .5f);
                        for (int i = 0; i < 3; i++)
                            Sprites.Blob("Canopy", theme.mid, 3f + (float)rng.NextDouble(), 2.4f, MidOrder + 1, layer, x - 1f + i, G + h + (i == 1 ? .6f : 0));
                    }
                    break;
                case "crystals":
                    for (float x = x0; x < x1; x += 2.5f + (float)rng.NextDouble() * 3f)
                    {
                        float h = 2f + (float)rng.NextDouble() * 3f;
                        Sprites.Blob("Rock pillar", theme.mid, 1.6f + (float)rng.NextDouble(), h * 2f, MidOrder, layer, x, G);
                        var crystal = Sprites.Block("Crystal", new Color(theme.accent.r, theme.accent.g, theme.accent.b, .8f), .5f, .5f, MidOrder + 1, layer, x + .4f, G + h * .6f);
                        crystal.transform.rotation = Quaternion.Euler(0, 0, 45f);
                        crystal.transform.localScale = new Vector3(.35f, .7f, 1f);
                    }
                    break;
                case "fields":
                    for (float x = x0; x < x1; x += 1.6f)
                        Sprites.Block("Fence post", theme.midDark, .14f, .9f, MidOrder + 1, layer, x, G + .45f);
                    Sprites.Block("Fence rail", theme.midDark, x1 - x0, .1f, MidOrder + 1, layer, (x0 + x1) * .5f, G + .7f);
                    for (float x = x0; x < x1; x += 4f + (float)rng.NextDouble() * 4f)
                        Sprites.Blob(rng.Next(2) == 0 ? "Haystack" : "Bush", rng.Next(2) == 0 ? new Color(.86f, .74f, .40f) : theme.mid,
                                     1.8f, 1.5f, MidOrder, layer, x, G + .5f);
                    break;
                case "reeds":
                    for (float x = x0; x < x1; x += .35f + (float)rng.NextDouble() * .5f)
                    {
                        float h = .8f + (float)rng.NextDouble() * 1.4f;
                        Sprites.Block("Reed", rng.Next(3) == 0 ? theme.midDark : theme.mid, .08f, h, MidOrder + 1, layer, x, G + h * .5f);
                    }
                    for (float x = x0; x < x1; x += 5f + (float)rng.NextDouble() * 5f)
                        Sprites.Blob("Pool", new Color(.45f, .55f, .55f, .8f), 3f, .5f, MidOrder, layer, x, G + .1f);
                    break;
                default:   // roundtrees
                    for (float x = x0; x < x1; x += 2.5f + (float)rng.NextDouble() * 3f)
                    {
                        float h = 1.5f + (float)rng.NextDouble() * 1.5f;
                        Sprites.Block("Trunk", theme.midDark, .35f, h, MidOrder, layer, x, G + h * .5f);
                        Sprites.Blob("Tree top", theme.mid, 2.4f, 2.2f, MidOrder + 1, layer, x, G + h + .6f);
                        Sprites.Blob("Tree top light", new Color(1, 1, 1, .12f), 1.2f, 1f, MidOrder + 2, layer, x - .4f, G + h + 1f);
                    }
                    break;
            }
        }

        // ---------------------------------------------------------------- ground, platforms, ladders

        static void Ground(Transform root, MapDef def, Theme theme, string art, System.Random rng)
        {
            float G = Tuning.Ground, w = def.columns + 40f, mid = def.columns * .5f;
            var sprite = Sprites.Art(art + "ground");
            float body = 0f;
            if (sprite != null)
            {
                Sprites.Tiled("Ground art", sprite, w, sprite.bounds.size.y, GroundOrder + 1, root, mid, G);
                body = sprite.bounds.size.y;
                var back = Sprites.Art(art + "ground_back");   // optional strip behind your feet that fades into the backdrop
                if (back != null) Sprites.Tiled("Ground back", back, w, back.bounds.size.y, GroundOrder + 1, root, mid, G);
            }
            else
            {
                Sprites.Block("Ground top", theme.groundTop, w, .32f, GroundOrder + 1, root, mid, G - .16f);
                Sprites.Block("Ground edge", Color.Lerp(theme.groundTop, Color.white, .25f), w, .07f, GroundOrder + 2, root, mid, G - .035f);
                for (float x = 0; x < def.columns; x += 1.5f + (float)rng.NextDouble() * 2.5f)
                    Sprites.Blob("Tuft", Color.Lerp(theme.groundTop, Color.black, .12f), .5f, .3f, BackDecorOrder, root, x, G + .05f);
                for (float x = 1; x < def.columns; x += 4f + (float)rng.NextDouble() * 6f)
                    Sprites.Blob("Pebble", Color.Lerp(theme.groundBody, Color.white, .25f), .35f, .18f, GroundOrder + 1, root, x, G - .55f);
                body = .32f;
            }
            Sprites.Block("Ground", theme.groundBody, w, 10f, GroundOrder, root, mid, G - body - 5f + .01f);
        }

        static void BuildPlatform(Transform root, Platform p, Theme theme, string art, System.Random rng)
        {
            float mid = (p.left + p.right) * .5f, width = p.right - p.left;
            var middle = Sprites.Art(art + "platform_mid");
            if (middle != null)
            {
                var left = Sprites.Art(art + "platform_left");
                var right = Sprites.Art(art + "platform_right");
                float lw = left != null ? left.bounds.size.x : 0f, rw = right != null ? right.bounds.size.x : 0f;
                if (lw + rw >= width) { lw = rw = 0f; left = right = null; }
                if (left != null) Sprites.Picture("Platform left", left, PlatformOrder, root, p.left + lw * .5f, p.top);
                if (right != null) Sprites.Picture("Platform right", right, PlatformOrder, root, p.right - rw * .5f, p.top);
                float inner = width - lw - rw;
                Sprites.Tiled("Platform", middle, inner, middle.bounds.size.y, PlatformOrder, root, p.left + lw + inner * .5f, p.top);
                return;
            }
            Sprites.Block("Platform earth", theme.platformBody, width, .5f, PlatformOrder, root, mid, p.top - .38f);
            Sprites.Blob("Platform end", theme.platformBody, .5f, .5f, PlatformOrder, root, p.left, p.top - .38f);
            Sprites.Blob("Platform end", theme.platformBody, .5f, .5f, PlatformOrder, root, p.right, p.top - .38f);
            Sprites.Block("Platform top", theme.platformTop, width, .22f, PlatformOrder + 1, root, mid, p.top - .11f);
            Sprites.Blob("Platform top end", theme.platformTop, .34f, .22f, PlatformOrder + 1, root, p.left, p.top - .11f);
            Sprites.Blob("Platform top end", theme.platformTop, .34f, .22f, PlatformOrder + 1, root, p.right, p.top - .11f);
            Sprites.Block("Platform shine", Color.Lerp(theme.platformTop, Color.white, .3f), width - .2f, .05f, PlatformOrder + 1, root, mid, p.top - .03f);
            Color hanging = theme.hanging.a > 0 ? theme.hanging : Color.Lerp(theme.platformBody, Color.black, .2f);
            for (float x = p.left + .7f; x < p.right - .4f; x += 1.2f + (float)rng.NextDouble() * 1.4f)
                Sprites.Blob("Hanging vine", hanging, .22f, .45f + (float)rng.NextDouble() * .3f, PlatformOrder - 1, root, x, p.top - .7f);
            if (theme.flecks == null) return;
            for (float x = p.left + .25f; x < p.right - .15f; x += .3f + (float)rng.NextDouble() * .45f)
            {
                var leaf = Sprites.Blob("Leaf", theme.flecks[rng.Next(theme.flecks.Length)], .2f, .1f, PlatformOrder + 1, root, x, p.top - .06f - (float)rng.NextDouble() * .1f);
                leaf.transform.rotation = Quaternion.Euler(0, 0, (float)rng.NextDouble() * 60f - 30f);
            }
        }

        static void BuildLadder(Transform root, Ladder l, Theme theme, string art)
        {
            float h = l.top - l.bottom;
            var sprite = Sprites.Art(art + "ladder");
            if (sprite != null) { Sprites.Tiled("Ladder", sprite, sprite.bounds.size.x, h, LadderOrder, root, l.x, l.bottom); return; }
            for (int side = -1; side <= 1; side += 2)
                Sprites.Block("Ladder rail", theme.ladderRail, .11f, h, LadderOrder, root, l.x + side * .27f, (l.bottom + l.top) * .5f);
            for (float y = l.bottom + .24f; y < l.top; y += .38f)
                Sprites.Block("Ladder rung", theme.ladderRung, .65f, .09f, LadderOrder + 1, root, l.x, y);
        }

        // ---------------------------------------------------------------- portals and town props

        static void BuildPortal(Transform root, float x, float y, bool townReturn)
        {
            var sprite = Sprites.Art(townReturn ? "Art/Props/return_portal" : "Art/Props/portal");
            if (sprite != null) { Sprites.Picture(townReturn ? "Return portal" : "Portal", sprite, PortalOrder, root, x, y); return; }
            Color glow = townReturn ? new Color(1f, .78f, .35f, .45f) : new Color(.35f, .65f, 1f, .45f);
            Color core = townReturn ? new Color(1f, .93f, .70f, .85f) : new Color(.78f, .92f, 1f, .85f);
            Sprites.Blob("Portal glow", glow, 1.1f, 2.1f, PortalOrder, root, x, y + 1f);
            Sprites.Blob("Portal core", core, .5f, 1.5f, PortalOrder + 1, root, x, y + 1f);
            Sprites.Blob("Portal base", glow, 1.4f, .3f, PortalOrder, root, x, y + .05f);
        }

        // placeholder: draw stand-in shapes when there is no art (false on painted scenes, which show their own).
        static void BuildWaystone(Transform root, float x, float y, bool placeholder)
        {
            var sprite = Sprites.Art("Art/Props/waystone");
            if (sprite != null) { Sprites.Picture("Waystone", sprite, PropOrder, root, x, y); return; }
            if (!placeholder) return;
            Sprites.Block("Waystone", new Color(.55f, .58f, .64f), .7f, 1.7f, PropOrder, root, x, y + .85f);
            Sprites.Blob("Waystone top", new Color(.55f, .58f, .64f), .7f, .5f, PropOrder, root, x, y + 1.7f);
            Sprites.Blob("Waystone rune glow", new Color(.45f, .85f, 1f, .45f), .9f, .9f, PropOrder, root, x, y + 1.15f);
            Sprites.Blob("Waystone rune", new Color(.75f, .95f, 1f, .95f), .3f, .3f, PropOrder + 1, root, x, y + 1.15f);
        }

        static void BuildMerchant(Transform root, float x, float y, bool placeholder)
        {
            var sprite = Sprites.Art("Art/Props/merchant");
            if (sprite != null) { Sprites.Picture("Merchant", sprite, PropOrder, root, x, y); return; }
            if (!placeholder) return;
            Sprites.Block("Merchant stall", new Color(.53f, .31f, .18f), 2.6f, 1.1f, PropOrder, root, x + .6f, y + .55f);
            Sprites.Block("Merchant awning", new Color(.86f, .45f, .22f), 3f, .4f, PropOrder, root, x + .6f, y + 2f);
            Sprites.Block("Awning post", new Color(.40f, .25f, .15f), .1f, 1.6f, PropOrder, root, x - .7f, y + 1.2f);
            Sprites.Block("Awning post", new Color(.40f, .25f, .15f), .1f, 1.6f, PropOrder, root, x + 1.9f, y + 1.2f);
            Sprites.Blob("Merchant body", new Color(.36f, .48f, .70f), .7f, .95f, PropOrder + 1, root, x - .3f, y + .5f);
            Sprites.Blob("Merchant head", new Color(.96f, .80f, .62f), .55f, .55f, PropOrder + 1, root, x - .3f, y + 1.2f);
            Sprites.Blob("Merchant hat", new Color(.70f, .30f, .25f), .65f, .3f, PropOrder + 2, root, x - .3f, y + 1.45f);
        }

        static void BuildSmithy(Transform root, float x, float y, bool placeholder)
        {
            var sprite = Sprites.Art("Art/Props/smithy");
            if (sprite != null) { Sprites.Picture("Smithy", sprite, PropOrder, root, x, y); return; }
            if (!placeholder) return;
            Sprites.Block("Forge", new Color(.50f, .47f, .45f), 1.8f, 1.6f, PropOrder, root, x + 1f, y + .8f);
            Sprites.Blob("Forge fire", new Color(1f, .55f, .15f, .9f), .9f, .7f, PropOrder + 1, root, x + 1f, y + .6f);
            Sprites.Block("Anvil", new Color(.25f, .26f, .30f), .9f, .3f, PropOrder + 1, root, x - .4f, y + .65f);
            Sprites.Block("Anvil foot", new Color(.25f, .26f, .30f), .4f, .5f, PropOrder + 1, root, x - .4f, y + .25f);
            Sprites.Blob("Smith body", new Color(.45f, .32f, .22f), .75f, 1f, PropOrder + 1, root, x - 1.3f, y + .5f);
            Sprites.Blob("Smith head", new Color(.96f, .80f, .62f), .55f, .55f, PropOrder + 2, root, x - 1.3f, y + 1.22f);
        }
    }
}
