using System.Collections.Generic;
using UnityEngine;

namespace AshenVale
{
    // A backdrop layer that follows the camera by (fx, fy): 1 = fixed to the screen (sky), 0 = fixed to the world.
    // A panorama layer is one painted picture (width x height units, origin at its bottom-left) that WorldView
    // slides so it always fills the view: with travel 1 its left edge shows at the map's left end and its right
    // edge at the right end; with less travel it slides through less of its spare width (it stays nearer the
    // camera), which makes it look further away. A map's distant picture uses that behind its front picture.
    public sealed class ParallaxLayer
    {
        public Transform transform;
        public float fx, fy;
        public bool panorama;
        public bool floorAnchored;
        public float width, height, floor;
        public float travel = 1f;
    }

    // Where a map picture's pixels are: picture pixel (x from the left, y from the top) -> a position under `parent`
    // (the map itself for a scene, the panorama layer for a backdrop), so effects and specks ride along with it.
    public struct PictureFrame
    {
        public Transform parent;
        public float left, bottom, perPixel;    // perPixel: world units per picture pixel
        public float width, height;             // the picture's size in pixels
        public bool flip;
        public bool Valid { get { return parent != null; } }
        public Vector3 At(float x, float y) { return new Vector3(left + (flip ? width - x : x) * perPixel, bottom + (height - y) * perPixel, 0); }
    }

    // One drifting speck: a mote, leaf, spark or puff of smoke.
    public sealed class Speck
    {
        public Transform transform;
        public SpriteRenderer sr;
        public Vector2 start;       // 0..1 across and up its group's box
        public float phase;
    }

    // A group of specks from MapArt (AmbientDef), with its sizes turned into its parent's units.
    public sealed class SpeckGroup
    {
        public AmbientDef def;
        public Vector2 centre;                          // spot groups: centre of their box under the parent
        public float width, height, sway, speed, fade;
        public readonly List<Speck> specks = new List<Speck>();
    }

    // Everything drawn for one map: a root (switched on while the local player is on the map), its backdrop layers
    // and its drifting specks.
    public sealed class MapVisual
    {
        public GameObject root;
        public readonly List<ParallaxLayer> layers = new List<ParallaxLayer>();
        public readonly List<SpeckGroup> ambient = new List<SpeckGroup>();
        public float cameraLowY;    // backdrop layers are laid out as seen with the camera at its lowest point

        // Called every frame for the map on screen, with the camera's centre and half the view's size.
        // Screen-filling groups live in a box around the camera and wrap at its edges (off screen), so each speck
        // keeps its place in the world as you walk; spot groups stay in their own box (a chimney, a forge).
        public void Animate(float time, Vector3 camera, float halfWidth, float halfHeight)
        {
            foreach (var g in ambient)
            {
                var d = g.def;
                bool around = d.aroundView;
                float w = around ? 2f * halfWidth + 2f : g.width, h = around ? 2f * halfHeight + 2f : g.height;
                float left = camera.x - w * .5f, bottom = camera.y - h * .5f;
                foreach (var s in g.specks)
                {
                    float a = time * d.rate + s.phase;
                    float x = s.start.x * w + Mathf.Sin(a) * g.sway, y = s.start.y * h, alpha;
                    if (d.drift == Drift.Float)
                    {
                        y += Mathf.Cos(a * .7f) * g.sway * .7f;
                        alpha = .4f + .6f * (Mathf.Sin(a * 2f) + 1f) * .5f;
                    }
                    else
                    {
                        y = Mathf.Repeat(y + (d.drift == Drift.Fall ? -g.speed : g.speed) * time, h);
                        alpha = around ? 1f : Mathf.Clamp01(Mathf.Min(y, h - y) / g.fade);    // fades in and out at the box's ends
                    }
                    s.transform.localPosition = around
                        ? new Vector3(left + Mathf.Repeat(x - left, w), bottom + Mathf.Repeat(y - bottom, h), 0)
                        : new Vector3(g.centre.x - w * .5f + x, g.centre.y - h * .5f + y, 0);
                    if (d.leaf) s.transform.localRotation = Quaternion.Euler(0, 0, a * 80f);
                    Color c = d.tint;
                    c.a *= alpha;
                    s.sr.color = c;
                }
            }
        }
    }

    // Builds the look of a map from its MapDef and theme. Every piece first looks for real art under
    // Resources/Art (see ART.md) and falls back to placeholder shapes, so art can be dropped in one file at a time.
    //
    //   Art/Scenes/<map>          a whole painted scene for one map (MapDef.scene), replacing the backdrop and ground
    //   Art/Backdrops/<map>       a painted backdrop for a wider map (MapDef.backdrop), replacing sky / far / mid
    //   ..._back                  either of those as a see-through front, with this distant picture sliding behind it
    //   MapArt                    animated lights (Resources/Shaders/MapEffect) and drifting specks per map
    //   Art/Themes/<theme>/sky, far, mid, ground, ground_back, platform_left, platform_mid, platform_right, ladder
    //   Art/Props/portal, return_portal, waystone, merchant, smithy
    public static class MapScenery
    {
        // Sorting orders, back to front (ladders stand behind platforms). Monsters 7, drops 9, heroes 10-11,
        // slashes 12, arrows 13 (WorldView).
        // A distant picture draws at SkyOrder and the front picture in front of it at FarOrder; effects on them at +5.
        public const int SkyOrder = -40, FarOrder = -30, MidOrder = -20, BackDecorOrder = -10, GroundOrder = -2,
                         PlatformOrder = 2, LadderOrder = 3, GlowOrder = 4, PropOrder = 5, PortalOrder = 6;
        // Ground fill -2, its painted top strip -1 (painted tiles carry picture above the path line, which used to
        // hide the foot of every ladder), platforms 2, ladders 3 so a ladder leans against the front of the ledge it
        // reaches (owner's call) and its foot stands on the path.
        const string EffectShader = "Shaders/MapEffect";

        sealed class Theme
        {
            public Color skyBottom, skyTop, far, farDark, mid, midDark, groundBody, groundTop, platformBody, platformTop, accent;
            public string farStyle, midStyle;
            public bool outdoors = true;
            public Color ladderRail = new Color(.55f, .35f, .19f), ladderRung = new Color(.72f, .51f, .28f);
            public Color hanging = Color.clear;     // what hangs under platforms (clear: a darker shade of the platform)
            public Color[] flecks;                  // little leaves scattered on platform tops (null: none)
            // Painted tiles cut from a picture show a strip of path in perspective above the ledge's edge, so the
            // walking line sits a little below the tile's top; the tiles are drawn raised by this much (units).
            public float groundSurface = 0f, platformSurface = 0f;
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
                    groundSurface = .16f, platformSurface = .10f,
                };
                case "mossgate": return new Theme   // Mossgate Hollow: autumn turning to moss on the way into Mosswood
                {
                    skyBottom = new Color(.82f, .90f, .78f), skyTop = new Color(.36f, .60f, .62f),
                    far = new Color(.40f, .58f, .45f), farDark = new Color(.33f, .50f, .40f),
                    mid = new Color(.30f, .52f, .32f), midDark = new Color(.36f, .27f, .18f),
                    groundBody = new Color(.22f, .21f, .15f), groundTop = new Color(.50f, .66f, .30f),
                    platformBody = new Color(.32f, .33f, .24f), platformTop = new Color(.52f, .68f, .32f),
                    accent = new Color(.95f, .85f, .55f), farStyle = "pines", midStyle = "bigtrees",
                    ladderRail = new Color(.37f, .28f, .18f), ladderRung = new Color(.58f, .42f, .26f),
                    hanging = new Color(.30f, .48f, .22f),
                    flecks = new[] { new Color(.55f, .72f, .30f), new Color(.38f, .60f, .28f), new Color(.93f, .50f, .18f), new Color(.75f, .30f, .12f) },
                    groundSurface = .16f, platformSurface = .10f,
                };
                case "thornglade": return new Theme     // Thornmother's Glade: colours from its haunted backdrop (the tiles are cut from it)
                {
                    skyBottom = new Color(.20f, .42f, .42f), skyTop = new Color(.05f, .12f, .14f),
                    far = new Color(.12f, .26f, .27f), farDark = new Color(.08f, .18f, .19f),
                    mid = new Color(.10f, .18f, .16f), midDark = new Color(.14f, .10f, .08f),
                    groundBody = new Color(.10f, .09f, .07f), groundTop = new Color(.26f, .30f, .16f),
                    platformBody = new Color(.13f, .11f, .08f), platformTop = new Color(.27f, .31f, .17f),
                    accent = new Color(.45f, .85f, .80f), farStyle = "pines", midStyle = "bigtrees",
                    ladderRail = new Color(.20f, .15f, .10f), ladderRung = new Color(.32f, .24f, .15f),
                    hanging = new Color(.20f, .26f, .12f),
                };
                case "forest": return new Theme
                {
                    skyBottom = new Color(.62f, .84f, .74f), skyTop = new Color(.22f, .46f, .50f),
                    far = new Color(.27f, .48f, .43f), farDark = new Color(.22f, .41f, .37f),
                    mid = new Color(.17f, .36f, .28f), midDark = new Color(.30f, .22f, .16f),
                    groundBody = new Color(.27f, .21f, .16f), groundTop = new Color(.33f, .62f, .34f),
                    platformBody = new Color(.40f, .28f, .18f), platformTop = new Color(.40f, .70f, .38f),
                    accent = new Color(.95f, .92f, .55f), farStyle = "pines", midStyle = "bigtrees",
                    groundSurface = .16f, platformSurface = .10f,
                };
                case "cave": case "hollowden": return new Theme
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
                    groundSurface = .16f, platformSurface = .10f,
                };
                case "cliffs": return new Theme     // Shale Cliffs: grey-blue rock, pines, snowy peaks far off
                {
                    skyBottom = new Color(.86f, .90f, .96f), skyTop = new Color(.38f, .56f, .86f),
                    far = new Color(.62f, .68f, .80f), farDark = new Color(.52f, .58f, .72f),
                    mid = new Color(.44f, .50f, .56f), midDark = new Color(.22f, .40f, .34f),
                    groundBody = new Color(.40f, .42f, .46f), groundTop = new Color(.62f, .64f, .62f),
                    platformBody = new Color(.46f, .48f, .52f), platformTop = new Color(.66f, .68f, .66f),
                    accent = new Color(.95f, .97f, 1f), farStyle = "mountains", midStyle = "bigtrees",
                    ladderRail = new Color(.42f, .32f, .22f), ladderRung = new Color(.6f, .46f, .3f),
                    hanging = new Color(.30f, .34f, .38f),
                    flecks = new[] { new Color(.78f, .80f, .78f), new Color(.35f, .55f, .40f) },
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
                // ---- Highspire: snow fields, the dwarven hall, the mines, the forge, the crystal hollow
                case "snow": return new Theme
                {
                    skyBottom = new Color(.93f, .95f, .98f), skyTop = new Color(.55f, .68f, .88f),
                    far = new Color(.72f, .78f, .88f), farDark = new Color(.60f, .66f, .78f),
                    mid = new Color(.86f, .92f, .97f), midDark = new Color(.30f, .36f, .42f),
                    groundBody = new Color(.44f, .48f, .56f), groundTop = new Color(.93f, .95f, .98f),
                    platformBody = new Color(.50f, .54f, .62f), platformTop = new Color(.95f, .97f, 1f),
                    accent = new Color(1f, 1f, 1f), farStyle = "mountains", midStyle = "bigtrees",
                    ladderRail = new Color(.38f, .30f, .22f), ladderRung = new Color(.58f, .46f, .32f),
                    hanging = new Color(.70f, .78f, .88f),
                    flecks = new[] { new Color(1f, 1f, 1f), new Color(.80f, .88f, .96f) },
                };
                case "hold": return new Theme     // Highspire: a stone hall cut into the mountain, warm windows
                {
                    skyBottom = new Color(.80f, .84f, .92f), skyTop = new Color(.40f, .50f, .72f),
                    far = new Color(.60f, .66f, .78f), farDark = new Color(.50f, .56f, .68f),
                    mid = new Color(.42f, .44f, .50f), midDark = new Color(.30f, .32f, .38f),
                    groundBody = new Color(.30f, .31f, .36f), groundTop = new Color(.58f, .60f, .66f),
                    platformBody = new Color(.36f, .37f, .42f), platformTop = new Color(.62f, .64f, .70f),
                    accent = new Color(1f, .72f, .35f), farStyle = "mountains", midStyle = "houses",
                };
                case "mine": return new Theme     // Ironvein: timbered shafts in brown rock
                {
                    skyBottom = new Color(.16f, .13f, .11f), skyTop = new Color(.07f, .06f, .05f),
                    far = new Color(.26f, .21f, .17f), farDark = new Color(.20f, .16f, .13f),
                    mid = new Color(.40f, .30f, .20f), midDark = new Color(.30f, .22f, .15f),
                    groundBody = new Color(.24f, .20f, .17f), groundTop = new Color(.46f, .40f, .34f),
                    platformBody = new Color(.36f, .26f, .17f), platformTop = new Color(.56f, .42f, .27f),
                    accent = new Color(1f, .78f, .45f), farStyle = "stalactites", midStyle = "timbers", outdoors = false,
                    ladderRail = new Color(.38f, .28f, .18f), ladderRung = new Color(.56f, .42f, .27f),
                };
                case "forge": return new Theme    // the Deep Forge and the vault: black stone lit from below by slag
                {
                    skyBottom = new Color(.30f, .12f, .06f), skyTop = new Color(.08f, .05f, .05f),
                    far = new Color(.24f, .16f, .14f), farDark = new Color(.17f, .11f, .10f),
                    mid = new Color(.34f, .24f, .20f), midDark = new Color(.24f, .16f, .14f),
                    groundBody = new Color(.20f, .17f, .17f), groundTop = new Color(.40f, .36f, .36f),
                    platformBody = new Color(.28f, .24f, .24f), platformTop = new Color(.48f, .42f, .40f),
                    accent = new Color(1f, .55f, .18f), farStyle = "stalactites", midStyle = "crystals", outdoors = false,
                    ladderRail = new Color(.30f, .30f, .34f), ladderRung = new Color(.48f, .48f, .52f),
                };
                case "crystal": return new Theme  // Crystal Hollow: the cave theme gone ice-blue
                {
                    skyBottom = new Color(.10f, .16f, .26f), skyTop = new Color(.04f, .06f, .12f),
                    far = new Color(.18f, .26f, .40f), farDark = new Color(.14f, .20f, .32f),
                    mid = new Color(.26f, .36f, .52f), midDark = new Color(.20f, .28f, .42f),
                    groundBody = new Color(.20f, .24f, .34f), groundTop = new Color(.44f, .52f, .68f),
                    platformBody = new Color(.30f, .36f, .50f), platformTop = new Color(.56f, .66f, .84f),
                    accent = new Color(.65f, .92f, 1f), farStyle = "stalactites", midStyle = "crystals", outdoors = false,
                };
                // ---- Saltmarrow: the beach, the port, the reef, sea caves, the galleon, the gallows cliffs
                case "beach": return new Theme
                {
                    skyBottom = new Color(1f, .92f, .78f), skyTop = new Color(.40f, .68f, .92f),
                    far = new Color(.30f, .62f, .80f), farDark = new Color(.22f, .50f, .70f),
                    mid = new Color(.36f, .66f, .36f), midDark = new Color(.52f, .38f, .24f),
                    groundBody = new Color(.72f, .60f, .40f), groundTop = new Color(.94f, .86f, .62f),
                    platformBody = new Color(.66f, .54f, .36f), platformTop = new Color(.92f, .84f, .60f),
                    accent = new Color(1f, .98f, .85f), farStyle = "sea", midStyle = "palms",
                    ladderRail = new Color(.50f, .36f, .22f), ladderRung = new Color(.70f, .54f, .34f),
                    hanging = new Color(.60f, .50f, .34f),
                };
                case "port": return new Theme     // Saltmarrow: timber quay, masts behind
                {
                    skyBottom = new Color(.98f, .84f, .66f), skyTop = new Color(.38f, .58f, .84f),
                    far = new Color(.32f, .58f, .76f), farDark = new Color(.24f, .48f, .66f),
                    mid = new Color(.58f, .44f, .30f), midDark = new Color(.40f, .28f, .18f),
                    groundBody = new Color(.42f, .30f, .20f), groundTop = new Color(.70f, .54f, .36f),
                    platformBody = new Color(.46f, .32f, .20f), platformTop = new Color(.74f, .56f, .36f),
                    accent = new Color(1f, .82f, .48f), farStyle = "sea", midStyle = "houses",
                };
                case "reef": return new Theme     // Wreckers' Reef: dark rock in the surf, wreck timbers
                {
                    skyBottom = new Color(.86f, .90f, .90f), skyTop = new Color(.36f, .56f, .76f),
                    far = new Color(.26f, .52f, .66f), farDark = new Color(.18f, .40f, .56f),
                    mid = new Color(.36f, .40f, .40f), midDark = new Color(.30f, .22f, .16f),
                    groundBody = new Color(.30f, .34f, .36f), groundTop = new Color(.52f, .58f, .56f),
                    platformBody = new Color(.40f, .30f, .20f), platformTop = new Color(.60f, .46f, .30f),
                    accent = new Color(.90f, .96f, 1f), farStyle = "sea", midStyle = "crystals",
                    hanging = new Color(.30f, .48f, .40f),
                };
                case "cove": return new Theme     // Smugglers' Cove and the grotto: sea caves lit teal from the water
                {
                    skyBottom = new Color(.08f, .20f, .22f), skyTop = new Color(.03f, .08f, .10f),
                    far = new Color(.14f, .26f, .28f), farDark = new Color(.10f, .20f, .22f),
                    mid = new Color(.24f, .34f, .34f), midDark = new Color(.18f, .26f, .26f),
                    groundBody = new Color(.20f, .24f, .24f), groundTop = new Color(.40f, .48f, .46f),
                    platformBody = new Color(.34f, .26f, .18f), platformTop = new Color(.54f, .42f, .28f),
                    accent = new Color(.45f, .95f, .85f), farStyle = "stalactites", midStyle = "timbers", outdoors = false,
                };
                case "galleon": return new Theme  // the Black Galleon and the hold: tarred timber, lantern light
                {
                    skyBottom = new Color(.30f, .22f, .20f), skyTop = new Color(.10f, .08f, .10f),
                    far = new Color(.22f, .18f, .16f), farDark = new Color(.16f, .12f, .11f),
                    mid = new Color(.34f, .24f, .16f), midDark = new Color(.22f, .15f, .10f),
                    groundBody = new Color(.26f, .18f, .12f), groundTop = new Color(.48f, .34f, .22f),
                    platformBody = new Color(.30f, .20f, .13f), platformTop = new Color(.52f, .36f, .22f),
                    accent = new Color(1f, .74f, .36f), farStyle = "stalactites", midStyle = "timbers", outdoors = false,
                };
                case "gallows": return new Theme  // Gallows Point: grey storm over the cliffs
                {
                    skyBottom = new Color(.62f, .64f, .66f), skyTop = new Color(.26f, .30f, .38f),
                    far = new Color(.30f, .40f, .48f), farDark = new Color(.22f, .30f, .38f),
                    mid = new Color(.38f, .40f, .42f), midDark = new Color(.26f, .26f, .28f),
                    groundBody = new Color(.32f, .34f, .36f), groundTop = new Color(.50f, .56f, .48f),
                    platformBody = new Color(.38f, .40f, .42f), platformTop = new Color(.54f, .58f, .52f),
                    accent = new Color(.80f, .84f, .88f), farStyle = "sea", midStyle = "deadtrees",
                };
                // ---- Eldermere: the deep forest, the magicians' village, the hollow library, the moonwell
                case "deepwood": return new Theme
                {
                    skyBottom = new Color(.30f, .48f, .40f), skyTop = new Color(.08f, .18f, .20f),
                    far = new Color(.12f, .28f, .26f), farDark = new Color(.08f, .20f, .20f),
                    mid = new Color(.14f, .36f, .26f), midDark = new Color(.20f, .14f, .10f),
                    groundBody = new Color(.18f, .15f, .12f), groundTop = new Color(.22f, .46f, .28f),
                    platformBody = new Color(.30f, .20f, .13f), platformTop = new Color(.26f, .52f, .30f),
                    accent = new Color(.75f, 1f, .55f), farStyle = "pines", midStyle = "bigtrees",
                    ladderRail = new Color(.28f, .20f, .14f), ladderRung = new Color(.46f, .34f, .22f),
                    hanging = new Color(.18f, .36f, .22f),
                    flecks = new[] { new Color(.60f, .90f, .45f), new Color(.85f, .75f, 1f) },
                };
                case "wizardtown": return new Theme   // Eldermere: timber houses in the trees, violet lantern light
                {
                    skyBottom = new Color(.52f, .46f, .66f), skyTop = new Color(.14f, .14f, .30f),
                    far = new Color(.18f, .30f, .32f), farDark = new Color(.12f, .22f, .26f),
                    mid = new Color(.46f, .36f, .30f), midDark = new Color(.30f, .22f, .20f),
                    groundBody = new Color(.26f, .22f, .20f), groundTop = new Color(.50f, .44f, .36f),
                    platformBody = new Color(.34f, .26f, .20f), platformTop = new Color(.56f, .46f, .34f),
                    accent = new Color(.80f, .62f, 1f), farStyle = "pines", midStyle = "houses",
                };
                case "library": return new Theme      // the hollow library and the sanctum: dark wood and old paper
                {
                    skyBottom = new Color(.22f, .16f, .20f), skyTop = new Color(.08f, .06f, .10f),
                    far = new Color(.26f, .20f, .18f), farDark = new Color(.18f, .14f, .13f),
                    mid = new Color(.44f, .30f, .22f), midDark = new Color(.30f, .20f, .15f),
                    groundBody = new Color(.24f, .18f, .14f), groundTop = new Color(.46f, .36f, .28f),
                    platformBody = new Color(.36f, .24f, .16f), platformTop = new Color(.58f, .44f, .30f),
                    accent = new Color(.85f, .65f, 1f), farStyle = "stalactites", midStyle = "shelves", outdoors = false,
                };
                case "moonwell": return new Theme     // the Moonwell: silver light under the boughs
                {
                    skyBottom = new Color(.20f, .30f, .40f), skyTop = new Color(.06f, .10f, .18f),
                    far = new Color(.14f, .24f, .30f), farDark = new Color(.10f, .18f, .24f),
                    mid = new Color(.20f, .36f, .40f), midDark = new Color(.16f, .14f, .12f),
                    groundBody = new Color(.18f, .20f, .24f), groundTop = new Color(.40f, .52f, .56f),
                    platformBody = new Color(.26f, .22f, .18f), platformTop = new Color(.44f, .58f, .60f),
                    accent = new Color(.80f, .95f, 1f), farStyle = "pines", midStyle = "crystals", outdoors = false,
                };
                // ---- Lastlight: the ruins, the camp, the catacombs, the ziggurat
                case "ruins": return new Theme
                {
                    skyBottom = new Color(.42f, .20f, .18f), skyTop = new Color(.10f, .06f, .10f),
                    far = new Color(.24f, .18f, .20f), farDark = new Color(.16f, .12f, .14f),
                    mid = new Color(.36f, .32f, .32f), midDark = new Color(.24f, .20f, .22f),
                    groundBody = new Color(.22f, .19f, .20f), groundTop = new Color(.42f, .38f, .36f),
                    platformBody = new Color(.30f, .26f, .27f), platformTop = new Color(.48f, .44f, .42f),
                    accent = new Color(1f, .45f, .25f), farStyle = "pillars", midStyle = "pillars",
                    ladderRail = new Color(.26f, .24f, .26f), ladderRung = new Color(.44f, .40f, .40f),
                    hanging = new Color(.20f, .24f, .18f),
                };
                case "ruintown": return new Theme     // Lastlight: tents and torches in a broken forum
                {
                    skyBottom = new Color(.50f, .28f, .22f), skyTop = new Color(.12f, .08f, .12f),
                    far = new Color(.26f, .20f, .22f), farDark = new Color(.18f, .14f, .16f),
                    mid = new Color(.50f, .40f, .30f), midDark = new Color(.34f, .26f, .22f),
                    groundBody = new Color(.24f, .20f, .20f), groundTop = new Color(.46f, .40f, .36f),
                    platformBody = new Color(.32f, .26f, .24f), platformTop = new Color(.50f, .44f, .40f),
                    accent = new Color(1f, .70f, .35f), farStyle = "pillars", midStyle = "houses",
                };
                case "catacomb": return new Theme     // the hall of idols, the catacombs, the oubliette: bone and old stone
                {
                    skyBottom = new Color(.14f, .12f, .12f), skyTop = new Color(.05f, .04f, .05f),
                    far = new Color(.22f, .19f, .18f), farDark = new Color(.15f, .13f, .12f),
                    mid = new Color(.46f, .42f, .36f), midDark = new Color(.30f, .27f, .24f),
                    groundBody = new Color(.20f, .18f, .17f), groundTop = new Color(.38f, .35f, .32f),
                    platformBody = new Color(.28f, .25f, .23f), platformTop = new Color(.44f, .40f, .36f),
                    accent = new Color(.55f, .95f, .65f), farStyle = "stalactites", midStyle = "pillars", outdoors = false,
                    ladderRail = new Color(.30f, .28f, .26f), ladderRung = new Color(.48f, .44f, .40f),
                };
                case "ziggurat": return new Theme     // the black ziggurat and the throne: black stone, violet fire
                {
                    skyBottom = new Color(.20f, .06f, .22f), skyTop = new Color(.04f, .02f, .06f),
                    far = new Color(.14f, .10f, .16f), farDark = new Color(.09f, .06f, .11f),
                    mid = new Color(.18f, .14f, .20f), midDark = new Color(.12f, .09f, .14f),
                    groundBody = new Color(.12f, .10f, .13f), groundTop = new Color(.28f, .24f, .30f),
                    platformBody = new Color(.16f, .13f, .18f), platformTop = new Color(.32f, .26f, .34f),
                    accent = new Color(.75f, .25f, 1f), farStyle = "pillars", midStyle = "pillars",
                    ladderRail = new Color(.20f, .18f, .22f), ladderRung = new Color(.36f, .30f, .38f),
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
            var look = MapArt.For(def.id);
            var rng = new System.Random(1000 + def.id * 37);
            float minX, maxX, minY, maxY;
            WorldView.CameraRange(def, 0f, out minX, out maxX, out minY, out maxY);
            visual.cameraLowY = minY;
            string art = "Art/Themes/" + def.theme + "/";
            // Tiles cut from a painted picture carry a strip of path above the ledge line (Tools/make_ground_tiles.py),
            // so they are drawn a little high; themes that set their own offsets keep them.
            if (theme.groundSurface == 0f && theme.platformSurface == 0f && Sprites.Art(art + "ground") != null)
            { theme.groundSurface = PaintedGroundSurface; theme.platformSurface = PaintedPlatformSurface; }

            // A painted scene already shows the sky, ground, houses, waystone and shops: draw only what it lacks.
            // A painted backdrop replaces the sky and distant layers; the ground and platforms are still drawn.
            PictureFrame front, back;
            bool painted = BuildScene(visual, def, look, out front, out back);
            if (!painted)
            {
                bool hasBackdrop = BuildBackdrop(visual, def, look, out front, out back);
                if (!hasBackdrop)
                {
                    Sky(visual, def, theme, art);
                    Far(visual, def, theme, art, rng);
                    Mid(visual, def, theme, art, rng);
                }
                // New complete panoramas already contain their street and retaining wall.
                // A second theme strip can create a competing visible floor above the actual feet.
                if (!hasBackdrop || !PaintedMapFraming.UsesOwnGround(def)) Ground(root, def, theme, art, rng);
            }
            PaintedMapFraming.AddSkyFill(def, root);
            BuildEffects(look, front, back, painted);
            BuildAmbient(visual, look, front);
            foreach (var plat in def.platforms) BuildPlatform(root, plat, theme, art, rng);
            foreach (var ladder in def.ladders) BuildLadder(root, ladder, theme, art);
            foreach (var portal in def.portals) BuildPortal(root, portal.x, portal.y, false);
            if (def.kind == MapKind.Town)
            {
                BuildPortal(root, def.returnX, def.returnY, true);
                BuildWaystone(root, def.waystoneX, def.waystoneY, PaintedMapFraming.ShowPlaceholderProps(def, painted, front.Valid));
                foreach (var stand in def.merchants) BuildMerchant(root, stand.x, stand.y, PaintedMapFraming.ShowPlaceholderProps(def, painted, front.Valid));
            }
            if (def.hasSmithy) BuildSmithy(root, def.smithyX, def.smithyY, PaintedMapFraming.ShowPlaceholderProps(def, painted, front.Valid));
            return visual;
        }

        // The map's painted scene, stretched to the map's width with its walking line on the ground, plus the
        // distant picture behind it if there is one (<scene>_back, at the same scale). False if the map has no scene
        // or its picture isn't in the project.
        static bool BuildScene(MapVisual visual, MapDef def, MapArtDef look, out PictureFrame front, out PictureFrame back)
        {
            front = back = new PictureFrame();
            if (string.IsNullOrEmpty(def.scene)) return false;
            var sprite = Sprites.Art(def.scene);
            if (sprite == null) return false;
            var root = visual.root.transform;
            var size = sprite.bounds.size;
            float scale = def.columns / size.x, bottom = Tuning.Ground - def.sceneFloor * size.y * scale;
            var far = Sprites.Art(def.scene + "_back");
            // Placed from the sprite's own bounds, so it lines up whatever pivot it was imported with.
            var o = Sprites.Picture("Scene", sprite, far != null ? FarOrder : SkyOrder, root,
                                    def.columns * .5f - sprite.bounds.center.x * scale, bottom - sprite.bounds.min.y * scale);
            o.transform.localScale = new Vector3(scale, scale, 1);
            float perPixel = def.columns / sprite.rect.width;
            front = new PictureFrame { parent = root, left = 0f, bottom = bottom, perPixel = perPixel, width = sprite.rect.width, height = sprite.rect.height };
            if (far != null)
            {
                // Same scale as the scene, bottoms lined up, sliding a little behind it (MapArt.backTravel).
                float width = far.rect.width * perPixel, height = far.rect.height * perPixel;
                back = Panorama(visual, "Distance", far, width, (Tuning.Ground - bottom) / height, false, look.backTravel, SkyOrder);
            }
            return true;
        }

        // Painted backdrops dropped in by map name use the same framing as the first ones: the path 116 px above the
        // bottom of a 724 px tall picture, shown 42 units wide.
        public const float DefaultBackdropFloor = 116f / 724f, DefaultBackdropWidth = 42f;
        public const float PaintedGroundSurface = .16f, PaintedPlatformSurface = .10f;

        // The map's painted backdrop as a panorama layer (WorldView moves it), plus the distant picture behind it if
        // there is one (<backdrop>_back), sliding less. A picture named after the map (Art/Backdrops/<key>.png) wins;
        // otherwise the one the map asks for. False if there is none.
        static bool BuildBackdrop(MapVisual visual, MapDef def, MapArtDef look, out PictureFrame front, out PictureFrame back)
        {
            front = back = new PictureFrame();
            if (HollowDenBackgroundTest.TryBuild(visual, def, out front, out back)) return true;
            string own = "Art/Backdrops/" + def.key, path = own;
            var sprite = Sprites.Art(own);
            float floor = def.backdropFloor, width = def.backdropWidth;
            bool flip = def.backdropFlip;
            if (sprite != null && def.backdrop != own) { floor = DefaultBackdropFloor; width = DefaultBackdropWidth; flip = false; }
            if (sprite == null && !string.IsNullOrEmpty(def.backdrop)) { path = def.backdrop; sprite = Sprites.Art(path); }
            if (sprite == null || width <= 0f) return false;
            PaintedMapFraming.Apply(def, sprite, ref width, ref floor);
            var far = Sprites.Art(path + "_back");
            if (far != null) back = Panorama(visual, "Distance", far, width, floor, flip, look.backTravel, SkyOrder, PaintedMapFraming.LocksFloor(def));
            front = Panorama(visual, "Backdrop", sprite, width, floor, flip, 1f, far != null ? FarOrder : SkyOrder, PaintedMapFraming.LocksFloor(def));
            return true;
        }

        // A picture as a panorama layer `width` units wide (see ParallaxLayer); its origin is its bottom-left corner
        // (mirrored pictures are flipped about their middle). Returns where its pixels are.
        static PictureFrame Panorama(MapVisual visual, string name, Sprite sprite, float width, float floor, bool flip, float travel, int order, bool floorAnchored = false)
        {
            var t = new GameObject(name).transform;
            t.SetParent(visual.root.transform, false);
            var size = sprite.bounds.size;
            float scale = width / size.x;
            var o = Sprites.Picture(name + " art", sprite, order, t, 0, 0);
            o.transform.localScale = new Vector3(flip ? -scale : scale, scale, 1);
            o.transform.localPosition = new Vector3(flip ? sprite.bounds.max.x * scale : -sprite.bounds.min.x * scale, -sprite.bounds.min.y * scale, 0);
            visual.layers.Add(new ParallaxLayer { transform = t, panorama = true, width = width, height = size.y * scale, floor = floor, travel = travel, floorAnchored = floorAnchored });
            return new PictureFrame { parent = t, left = 0f, bottom = 0f, perPixel = width / sprite.rect.width,
                                      width = sprite.rect.width, height = sprite.rect.height, flip = flip };
        }

        // MapArt effects: animated lights on the front picture, or on the distant one (sliding with it). On a scene the
        // front ones draw over the painting but behind heroes; on a backdrop, behind the ground and platforms.
        static void BuildEffects(MapArtDef look, PictureFrame front, PictureFrame back, bool scene)
        {
            if (look.effects.Length == 0) return;
            foreach (var fx in look.effects)
            {
                var frame = fx.distant ? back : front;
                if (!frame.Valid) continue;             // a distant effect needs the distant picture
                int order = fx.distant ? SkyOrder + 5 : scene ? GlowOrder : FarOrder + 5;
                var at = frame.At(fx.x, fx.y);
                Effect(fx.name, frame.parent, at.x, at.y, fx.width * frame.perPixel, fx.height * frame.perPixel, fx.color, fx.mode, fx.strength, order);
            }
        }

        static bool effectShaderWarned;

        // One animated light (see the shader): a width x height quad centred on (x, y) under `parent` (local units).
        // Water and mist are painted over the picture; glows and fire add light to it. Null if the shader is missing.
        public static GameObject Effect(string name, Transform parent, float x, float y, float width, float height, Color color, int mode, float strength, int order)
        {
            var shader = Resources.Load<Shader>(EffectShader);
            if (shader == null || !shader.isSupported)
            {
                if (!effectShaderWarned) Debug.LogWarning("Map effects are off: shader Resources/" + EffectShader + (shader == null ? " was not found" : " is not supported here"));
                effectShaderWarned = true;
                return null;
            }
            var material = new Material(shader);
            material.SetColor("_Color", color);
            material.SetFloat("_Mode", mode);
            material.SetFloat("_Active", strength <= 0f ? 1f : strength);
            bool over = mode == 1 || mode == 2;
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)(over ? UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : UnityEngine.Rendering.BlendMode.One));
            var o = Sprites.Picture(name, Sprites.FullQuad, order, parent, 0, 0);
            o.transform.localPosition = new Vector3(x, y, 0);
            o.transform.localScale = new Vector3(width, height, 1);
            o.GetComponent<SpriteRenderer>().sharedMaterial = material;
            return o;
        }

        // MapArt specks: screen-filling groups (motes, leaves, pollen, ash) under the map; spot groups (smoke, sparks)
        // on the front picture, in its pixels. MapVisual.Animate moves them.
        static void BuildAmbient(MapVisual visual, MapArtDef look, PictureFrame front)
        {
            int seed = 83;
            foreach (var d in look.ambient)
            {
                Transform parent;
                float unit;
                if (d.aroundView) { parent = visual.root.transform; unit = 1f; }
                else if (front.Valid) { parent = front.parent; unit = front.perPixel; }
                else continue;
                var holder = new GameObject(d.name).transform;
                holder.SetParent(parent, false);
                var group = new SpeckGroup { def = d, width = d.width * unit, height = d.height * unit, sway = d.sway * unit, speed = d.speed * unit };
                group.fade = Mathf.Max(.01f, group.height * .12f);
                if (!d.aroundView) { var c = front.At(d.x, d.y); group.centre = new Vector2(c.x, c.y); }
                var rng = new System.Random(seed++);
                for (int i = 0; i < d.count; i++)
                {
                    var o = new GameObject(d.leaf ? "Leaf" : "Speck");
                    o.transform.SetParent(holder, false);
                    o.transform.localScale = Vector3.one * Mathf.Lerp(d.sizeMin, d.sizeMax, (float)rng.NextDouble()) * unit;
                    var sr = o.AddComponent<SpriteRenderer>();
                    sr.sprite = d.leaf ? Sprites.Leaf : Sprites.Glow;
                    sr.sortingOrder = d.order;
                    sr.color = d.tint;
                    group.specks.Add(new Speck { transform = o.transform, sr = sr, phase = (float)rng.NextDouble() * 6.28f,
                                                 start = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble()) });
                }
                visual.ambient.Add(group);
            }
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
                case "pillars":
                    for (float x = x0; x < x1; x += 2.5f + (float)rng.NextDouble() * 3f)
                    {
                        float h = 3f + (float)rng.NextDouble() * 7f;
                        Sprites.Block("Far column", rng.Next(2) == 0 ? theme.far : theme.farDark, .9f + (float)rng.NextDouble() * .5f, h, FarOrder, layer, x, G + h * .5f);
                        if (rng.Next(3) == 0) Sprites.Block("Lintel", theme.farDark, 4f, .6f, FarOrder, layer, x + 1.5f, G + h);
                    }
                    break;
                case "sea":
                    Sprites.Band("Sea", theme.far, theme.farDark, (x1 - x0) + 10f, 5f, FarOrder, layer, (x0 + x1) * .5f, G + 2f);
                    Sprites.Band("Horizon", new Color(theme.accent.r, theme.accent.g, theme.accent.b, .5f), new Color(theme.accent.r, theme.accent.g, theme.accent.b, 0f),
                                 (x1 - x0) + 10f, .5f, FarOrder + 1, layer, (x0 + x1) * .5f, G + 4.6f);
                    for (float x = x0; x < x1; x += 9f + (float)rng.NextDouble() * 9f)
                    {
                        Sprites.Block("Hull", theme.farDark, 1.6f, .35f, FarOrder + 1, layer, x, G + 4.4f);
                        Sprites.Block("Mast", theme.farDark, .12f, 1.3f, FarOrder + 1, layer, x, G + 5.1f);
                        Sprites.Block("Sail", new Color(theme.accent.r, theme.accent.g, theme.accent.b, .8f), .8f, .9f, FarOrder + 2, layer, x + .3f, G + 5.2f);
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
                case "pillars":   // ruins: broken columns, rubble, a brazier here and there
                    for (float x = x0; x < x1; x += 3f + (float)rng.NextDouble() * 3.5f)
                    {
                        float h = 1.5f + (float)rng.NextDouble() * 4f;
                        Sprites.Block("Column", theme.mid, .8f, h, MidOrder, layer, x, G + h * .5f);
                        Sprites.Block("Capital", theme.midDark, 1.2f, .3f, MidOrder + 1, layer, x, G + h);
                        Sprites.Blob("Rubble", theme.midDark, 2f + (float)rng.NextDouble(), .8f, MidOrder + 1, layer, x + 1f, G + .2f);
                        if (rng.Next(3) == 0)
                            Sprites.Blob("Brazier", new Color(theme.accent.r, theme.accent.g, theme.accent.b, .85f), .5f, .7f, MidOrder + 2, layer, x + .4f, G + h + .5f);
                    }
                    break;
                case "shelves":   // bookcases: tall cases with rows of coloured spines
                    for (float x = x0; x < x1; x += 2.6f + (float)rng.NextDouble() * 1.5f)
                    {
                        float h = 3f + (float)rng.NextDouble() * 2.5f, w = 2f;
                        Sprites.Block("Bookcase", theme.mid, w, h, MidOrder, layer, x, G + h * .5f);
                        for (float sy = .5f; sy < h - .4f; sy += .7f)
                            for (float sx = -w * .42f; sx < w * .42f; sx += .3f)
                                Sprites.Block("Spine", new Color(.3f + (float)rng.NextDouble() * .6f, .2f + (float)rng.NextDouble() * .4f, .2f + (float)rng.NextDouble() * .6f),
                                              .22f, .45f, MidOrder + 1, layer, x + sx, G + sy);
                    }
                    break;
                case "palms":
                    for (float x = x0; x < x1; x += 3.5f + (float)rng.NextDouble() * 4f)
                    {
                        float h = 3.5f + (float)rng.NextDouble() * 2.5f, lean = (float)rng.NextDouble() * .8f - .4f;
                        var trunk = Sprites.Block("Palm trunk", theme.midDark, .35f, h, MidOrder, layer, x + lean * h * .5f, G + h * .5f);
                        trunk.transform.rotation = Quaternion.Euler(0, 0, -lean * 40f);
                        for (int i = 0; i < 5; i++)
                        {
                            var frond = Sprites.Blob("Frond", theme.mid, 2.4f, .5f, MidOrder + 1, layer, x + lean * h, G + h);
                            frond.transform.rotation = Quaternion.Euler(0, 0, -70f + i * 35f);
                            frond.transform.localScale = new Vector3(1f, 1f, 1f);
                        }
                    }
                    break;
                case "timbers":   // mine props: posts and a cap beam, a lamp on some
                    for (float x = x0; x < x1; x += 3.2f + (float)rng.NextDouble() * 2.5f)
                    {
                        float h = 3f + (float)rng.NextDouble() * 1.5f, w = 2.2f + (float)rng.NextDouble();
                        Sprites.Block("Post", theme.mid, .35f, h, MidOrder + 1, layer, x - w * .5f, G + h * .5f);
                        Sprites.Block("Post", theme.mid, .35f, h, MidOrder + 1, layer, x + w * .5f, G + h * .5f);
                        Sprites.Block("Beam", theme.midDark, w + .7f, .4f, MidOrder + 1, layer, x, G + h);
                        if (rng.Next(2) == 0)
                            Sprites.Blob("Lamp", new Color(theme.accent.r, theme.accent.g, theme.accent.b, .85f), .4f, .5f, MidOrder + 2, layer, x, G + h - .5f);
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
                Sprites.Tiled("Ground art", sprite, w, sprite.bounds.size.y, GroundOrder + 1, root, mid, G + theme.groundSurface);
                body = sprite.bounds.size.y;
                var back = Sprites.Art(art + "ground_back");   // optional strip behind your feet that fades into the backdrop
                if (back != null) Sprites.Tiled("Ground back", back, w, back.bounds.size.y, GroundOrder + 1, root, mid, G + theme.groundSurface);
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
                float top = p.top + theme.platformSurface;
                if (left != null) Sprites.Picture("Platform left", left, PlatformOrder, root, p.left + lw * .5f, top);
                if (right != null) Sprites.Picture("Platform right", right, PlatformOrder, root, p.right - rw * .5f, top);
                float inner = width - lw - rw;
                Sprites.Tiled("Platform", middle, inner, middle.bounds.size.y, PlatformOrder, root, p.left + lw + inner * .5f, top);
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
            var sprite = Sprites.Art(art + "ladder") ?? Sprites.Art("Art/Props/ladder");   // a theme's own, else the shared one
            if (sprite != null) { Sprites.Tiled("Ladder", sprite, sprite.bounds.size.x, h, LadderOrder, root, l.x, l.bottom); return; }
            for (int side = -1; side <= 1; side += 2)
                Sprites.Block("Ladder rail", theme.ladderRail, .11f, h, LadderOrder, root, l.x + side * .27f, (l.bottom + l.top) * .5f);
            for (float y = l.bottom + .24f; y < l.top; y += .38f)
                Sprites.Block("Ladder rung", theme.ladderRung, .65f, .09f, LadderOrder + 1, root, l.x, y);
        }

        // ---------------------------------------------------------------- portals and town props

        public static readonly Color MapPortalColor = new Color(.45f, .68f, 1f), ReturnPortalColor = new Color(1f, .78f, .38f),
                                     TownPortalColor = new Color(.7f, .55f, 1f);

        // The portal picture (Art/Props/swirl_portal): a tall swirl standing on a round seal. It is drawn PortalDrawHeight
        // tall (the whole picture, margins included), with the middle of its seal SealLift above the floor line.
        // The three numbers after that describe the picture: how far up it the seal's middle and the swirl's middle
        // are (as fractions of its height), and the seal's half-width (as a fraction of its width).
        const float PortalDrawHeight = 3.0f, SealLift = .08f;
        const float SealUp = 194f / 1214f, SwirlUp = 614f / 1214f, SealHalfWidth = 272f / 1295f;

        // Map gates and a town's return gate.
        static void BuildPortal(Transform root, float x, float y, bool townReturn)
        {
            Portal(root, x, y, townReturn ? "Art/Props/return_portal" : "Art/Props/portal", townReturn ? ReturnPortalColor : MapPortalColor, PortalOrder);
        }

        // A portal standing at (x, y) on the floor. Also the T town portal (WorldView, violet). The old `picture`
        // argument is kept so existing call sites remain compatible; every portal uses the same swirl art, and
        // `color` tints its particles.
        public static void Portal(Transform parent, float x, float y, string picture, Color color, int order)
        {
            var anchor = new GameObject("Swirl Portal").transform;      // sits at the middle of the seal
            anchor.SetParent(parent, false);
            anchor.localPosition = new Vector3(x, y + SealLift, 0f);

            Transform art = null;
            float sealRadius = .67f, swirlHeight = 1.05f;
            var sprite = Sprites.Art("Art/Props/swirl_portal");
            if (sprite != null)
            {
                var shown = Sprites.Picture("Swirl", sprite, order, anchor, 0f, 0f);
                float scale = PortalDrawHeight / sprite.bounds.size.y;
                float width = sprite.bounds.size.x * scale;
                shown.transform.localScale = new Vector3(scale, scale, 1f);
                // Put the seal's middle on the anchor, wherever the sprite's pivot is.
                shown.transform.localPosition = new Vector3(-sprite.bounds.center.x * scale, -(sprite.bounds.min.y * scale + SealUp * PortalDrawHeight), 0f);
                art = shown.transform;
                sealRadius = SealHalfWidth * width;
                swirlHeight = (SwirlUp - SealUp) * PortalDrawHeight;
            }
            else
            {
                // Stand-in until the picture is imported: a glow and a bright core.
                Color glow = color; glow.a = .45f;
                Color core = Color.Lerp(color, Color.white, .7f); core.a = .85f;
                Sprites.Blob("Portal glow", glow, 1.1f, 2.1f, order, parent, x, y + 1f);
                Sprites.Blob("Portal core", core, .5f, 1.5f, order + 1, parent, x, y + 1f);
            }

            anchor.gameObject.AddComponent<PortalManaVfx>().Initialize(color, order + 1, art, sealRadius, swirlHeight, swirlHeight * .6f);
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

