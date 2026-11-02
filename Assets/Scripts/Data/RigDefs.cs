namespace AshenVale
{
    // One piece of a cutout hero: where it sits in its atlas (pixels, y counted from the bottom) and
    // the joint it turns around (0..1 across the piece, 0,0 = bottom left).
    public struct RigPart
    {
        public int x, y, width, height;
        public float pivotX, pivotY;
        public RigPart(int x, int y, int width, int height, float pivotX, float pivotY)
        { this.x = x; this.y = y; this.width = width; this.height = height; this.pivotX = pivotX; this.pivotY = pivotY; }
    }

    // A paper-doll hero: twelve painted pieces that rotate at the joints instead of a frame per pose.
    // Pieces are listed head, torso, pelvis, near upper arm, near forearm (with hand), far upper arm,
    // far forearm, near thigh, near shin (with boot), far thigh, far shin, weapon. Art faces right.
    public sealed class RigDef
    {
        public string hero;                 // HeroDef.folder this rig dresses
        public string atlas, capeAtlas;     // Resources paths of the parts sheet and the optional cape
        public bool ranger;                 // the ranger holds her bow in a raised hand; the knight carries his spear
        public RigPart[] parts;
        public RigPart cape;
        public float[] nearSole, farSole;   // lowest boot pixels (x, y pairs in shin heights, from the shin joint) for the gait
        public float[] nearFootSole, farFootSole;  // the same on the boot slice alone (in boot heights, from the ankle)
        public float headHeight = 1.05f, torsoHeight = 1.05f, pelvisHeight = .65f;
        public float upperArmHeight = .58f, forearmHeight = .60f, thighHeight = .75f, shinHeight = .85f, weaponHeight = 3.95f, capeHeight = 2.2f;
        public float scale = .4f;           // rig units to world units: the knight stands about 1.4 units tall
    }

    public static class Rigs
    {
        // Rects were measured from the sheets with Tools/rig_bounds.py (alpha > 64, two pixels of padding).
        public static readonly RigDef[] All =
        {
            new RigDef
            {
                hero = "ElvenRanger", atlas = "Art/Rigs/ranger_parts", ranger = true, scale = .37f,
                headHeight = 1.2f, pelvisHeight = .65f, weaponHeight = 2.5f,
                parts = new[]
                {
                    new RigPart(28, 733, 329, 340, .72f, .27f),    // head
                    new RigPart(416, 744, 302, 325, .60f, .06f),   // torso
                    new RigPart(770, 748, 278, 282, .50f, .80f),   // pelvis
                    new RigPart(1176, 767, 167, 271, .65f, .93f),  // near upper arm
                    new RigPart(128, 376, 134, 335, .42f, .94f),   // near forearm
                    new RigPart(464, 402, 151, 290, .58f, .93f),   // far upper arm
                    new RigPart(845, 378, 115, 331, .42f, .94f),   // far forearm
                    new RigPart(1213, 379, 119, 336, .50f, .95f),  // near thigh
                    new RigPart(115, 17, 137, 331, .46f, .95f),    // near shin
                    new RigPart(498, 32, 120, 315, .50f, .95f),    // far thigh
                    new RigPart(824, 19, 170, 329, .46f, .95f),    // far shin
                    new RigPart(1200, 12, 141, 339, .69f, .46f),   // bow
                },
                nearSole = new[] { -.171f, -.275f, -.128f, -.891f, -.086f, -.903f, -.047f, -.927f, -.005f, -.936f, .038f, -.939f, .080f, -.942f, .119f, -.939f, .162f, -.936f, .204f, -.930f },
                farSole = new[] { -.212f, -.277f, -.160f, -.897f, -.109f, -.906f, -.057f, -.900f, -.005f, -.921f, .047f, -.936f, .098f, -.942f, .150f, -.942f, .202f, -.939f, .253f, -.930f },
                nearFootSole = new[] { -.457f, -.640f, -.307f, -.683f, -.167f, -.769f, -.016f, -.801f, .134f, -.812f, .285f, -.823f, .425f, -.812f, .575f, -.801f, .726f, -.780f },
                farFootSole = new[] { -.573f, -.668f, -.388f, -.701f, -.203f, -.679f, -.018f, -.755f, .166f, -.810f, .351f, -.832f, .536f, -.832f, .721f, -.821f, .905f, -.788f },
            },
            new RigDef
            {
                hero = "DragonKnight", atlas = "Art/Rigs/warrior_parts", capeAtlas = "Art/Rigs/warrior_cape", scale = .4f,
                headHeight = 1.05f, pelvisHeight = .70f, weaponHeight = 3.95f,
                parts = new[]
                {
                    new RigPart(54, 744, 278, 322, .52f, .08f),    // head
                    new RigPart(374, 740, 343, 318, .60f, .06f),   // torso
                    new RigPart(773, 738, 285, 326, .50f, .80f),   // pelvis
                    new RigPart(1176, 746, 186, 323, .65f, .93f),  // near upper arm
                    new RigPart(127, 411, 136, 301, .42f, .94f),   // near forearm
                    new RigPart(458, 410, 179, 302, .58f, .93f),   // far upper arm
                    new RigPart(837, 410, 123, 301, .42f, .94f),   // far forearm
                    new RigPart(1183, 395, 169, 323, .50f, .95f),  // near thigh
                    new RigPart(117, 17, 139, 339, .46f, .95f),    // near shin
                    new RigPart(459, 31, 166, 320, .50f, .95f),    // far thigh
                    new RigPart(820, 17, 179, 339, .46f, .95f),    // far shin
                    new RigPart(1243, 16, 47, 343, .50f, .36f),    // spear
                },
                cape = new RigPart(37, 135, 850, 1177, .82f, .95f),
                nearSole = new[] { -.169f, -.901f, -.128f, -.916f, -.087f, -.937f, -.046f, -.940f, -.004f, -.943f, .037f, -.940f, .078f, -.940f, .120f, -.937f, .161f, -.934f, .202f, -.922f },
                farSole = new[] { -.218f, -.904f, -.165f, -.913f, -.112f, -.916f, -.059f, -.913f, -.005f, -.937f, .048f, -.940f, .101f, -.943f, .154f, -.940f, .207f, -.937f, .260f, -.928f },
                nearFootSole = new[] { -.605f, -.679f, -.457f, -.732f, -.310f, -.805f, -.163f, -.816f, -.015f, -.826f, .132f, -.816f, .280f, -.816f, .427f, -.805f, .574f, -.795f, .722f, -.753f },
                farFootSole = new[] { -.777f, -.689f, -.588f, -.721f, -.398f, -.732f, -.209f, -.721f, -.019f, -.805f, .170f, -.816f, .360f, -.826f, .549f, -.816f, .739f, -.805f, .928f, -.774f },
            },
        };

        // The rig for a hero, or null if that hero only has frame art.
        public static RigDef For(HeroDef hero)
        {
            foreach (var r in All) if (r.hero == hero.folder) return r;
            return null;
        }
    }
}
