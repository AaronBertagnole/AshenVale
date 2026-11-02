using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AshenVale
{
    // A hero drawn as a paper doll: painted pieces hung on a little skeleton and posed in code
    // (idle breathing, a walk cycle, jumps, climbing, attacks, hurt, death). Built from a RigDef;
    // the sim never sees it. Positive joint angles swing a limb forward (the art faces right).
    public sealed class HeroRig
    {
        public readonly Transform root;             // stands at the feet; mirrored to face left
        readonly RigDef def;
        readonly Transform visual, body, torso, head, nearArm, nearForearm, farArm, farForearm;
        readonly Transform nearThigh, nearShin, farThigh, farShin, weapon, cape, pelvis, nearFoot, farFoot;
        readonly SpriteRenderer nearFullLeg, farFullLeg, nearCalfArt, farCalfArt, nearFootArt, farFootArt;
        readonly float nearCalf, farCalf, nearFootHeight, farFootHeight;   // rig units
        readonly SortingGroup group;
        readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        float walkPhase, breath;
        bool walkingArt;

        static readonly Dictionary<string, Sprite[]> sheets = new Dictionary<string, Sprite[]>();

        // Null when the rig's art isn't in Resources yet (the hero keeps its frame animation).
        public static HeroRig Create(RigDef def, Transform parent, string name)
        {
            var parts = Pieces(def);
            if (parts == null) return null;
            return new HeroRig(def, parts, parent, name);
        }

        HeroRig(RigDef def, Sprite[] parts, Transform parent, string name)
        {
            this.def = def;
            root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localScale = Vector3.one * def.scale;
            visual = Joint("Visual", root, Vector2.zero);
            group = visual.gameObject.AddComponent<SortingGroup>();
            body = Joint("Body", visual, new Vector2(0, 1.5f));
            pelvis = Joint("Pelvis", body, Vector2.zero);
            Picture(parts[2], pelvis, def.pelvisHeight, 2);
            torso = Joint("Torso", body, new Vector2(0, .06f));
            Picture(parts[1], torso, def.torsoHeight, 0);
            head = Joint("Head", torso, new Vector2(def.ranger ? .02f : -.08f, def.ranger ? .82f : .96f));
            Picture(parts[0], head, def.headHeight, def.ranger ? -1 : 3);
            nearArm = Joint("NearArm", body, new Vector2(def.ranger ? .20f : -.36f, def.ranger ? .84f : .96f));
            Picture(parts[3], nearArm, def.upperArmHeight, 4);
            nearForearm = Joint("NearForearm", nearArm, new Vector2(def.ranger ? -.08f : -.04f, -.5f));
            Picture(parts[4], nearForearm, def.forearmHeight, 6);
            farArm = Joint("FarArm", body, new Vector2(def.ranger ? -.30f : .34f, .94f));
            Picture(parts[5], farArm, def.upperArmHeight, -4);
            farForearm = Joint("FarForearm", farArm, new Vector2(-.05f, -.5f));
            Picture(parts[6], farForearm, def.forearmHeight, -3);
            nearThigh = Joint("NearThigh", body, new Vector2(.18f, 0));
            Picture(parts[7], nearThigh, def.thighHeight, 1);
            nearShin = Joint("NearShin", nearThigh, new Vector2(0, -.66f));
            nearFullLeg = Picture(parts[8], nearShin, def.shinHeight, 2);
            farThigh = Joint("FarThigh", body, new Vector2(-.18f, 0));
            Picture(parts[9], farThigh, def.thighHeight, -5);
            farShin = Joint("FarShin", farThigh, new Vector2(0, -.66f));
            farFullLeg = Picture(parts[10], farShin, def.shinHeight, -4);
            // Walking swaps each whole shin for a calf plus a boot on its own ankle (slices of the same pixels).
            if (parts.Length > 16 && parts[13] != null && parts[14] != null && parts[15] != null && parts[16] != null)
            {
                nearCalfArt = Picture(parts[13], nearShin, def.shinHeight * parts[13].rect.height / parts[8].rect.height, 2);
                farCalfArt = Picture(parts[15], farShin, def.shinHeight * parts[15].rect.height / parts[10].rect.height, -4);
                nearCalf = def.shinHeight * (parts[8].pivot.y - parts[14].pivot.y) / parts[8].rect.height;
                farCalf = def.shinHeight * (parts[10].pivot.y - parts[16].pivot.y) / parts[10].rect.height;
                nearFootHeight = def.shinHeight * parts[14].rect.height / parts[8].rect.height;
                farFootHeight = def.shinHeight * parts[16].rect.height / parts[10].rect.height;
                nearFoot = Joint("NearFoot", nearShin, new Vector2(0, -nearCalf));
                farFoot = Joint("FarFoot", farShin, new Vector2(0, -farCalf));
                nearFootArt = Picture(parts[14], nearFoot, nearFootHeight, 3);
                farFootArt = Picture(parts[16], farFoot, farFootHeight, -3);
                LegArt(false);
            }
            weapon = Joint("Weapon", nearForearm, new Vector2(def.ranger ? .08f : .02f, -.5f));
            Picture(parts[11], weapon, def.weaponHeight, 5);
            if (parts.Length > 12 && parts[12] != null)
            {
                cape = Joint("Cape", body, new Vector2(-.22f, 1.05f));
                Picture(parts[12], cape, def.capeHeight, -7);
            }
            Stand();
        }

        public void Destroy() { Object.Destroy(root.gameObject); }

        public int Order { set { group.sortingOrder = value; } }

        public Color Tint { set { foreach (var r in renderers) r.color = value; } }

        // Poses the doll for this frame. `clip` is the animation WorldView chose and `clock` how long it has run.
        public void Pose(PlayerState p, string clip, float clock, float frameDt)
        {
            var h = p.Hero;
            visual.localScale = new Vector3(p.facing < 0 ? -1 : 1, 1, 1);
            visual.localRotation = Quaternion.identity;
            breath += frameDt;
            float wave = Mathf.Sin(breath * Mathf.PI * 2 / 2.4f);   // slow idle breathing
            Stand();
            Angle(torso, wave * .7f);
            Angle(head, -wave * .6f);
            if (cape) Angle(cape, wave * 1.5f);
            float nearUpper = def.ranger ? 18 : -30, nearLower = def.ranger ? 40 : 10;
            float farUpper = def.ranger ? -20 : -3, farLower = def.ranger ? 35 : 8;
            float weaponTurn = 0;                                   // beyond "stay upright in the hand"
            bool alignWeapon = false;                               // true: the weapon continues the forearm

            LegArt(clip == "run" && !p.climbing && nearFoot != null);
            if (clip == "run")
            {
                // The pack's stroll, with its cadence following the hero's speed (the stride itself stays
                // modest: long lunging steps read as side-stepping on a cutout, so the feet may slide a little).
                float ground = Mathf.Max(2.5f, Mathf.Abs(p.vx)) / def.scale;        // rig units per second
                float cadence = Mathf.Clamp(ground / 6f, 1.25f, 2.2f);                // cycles per second
                float run = Mathf.Clamp01((ground - 6f) / 8f) * .35f;                 // a touch longer and livelier at speed
                walkPhase += frameDt * cadence;
                float cycle = Mathf.Repeat(walkPhase, 1f), phase = cycle * Mathf.PI * 2;
                float g = Mathf.Sin(phase);
                if (nearFoot != null && !p.climbing)
                {
                    // Walk revision 2: planted stance, lifted recovery step, ankles that keep the boots flat.
                    float hipY = Mathf.Lerp(1.42f, 1.38f, run) + Mathf.Cos(phase * 2) * (.014f + .02f * run);
                    body.localPosition = new Vector3(g * .012f, hipY, 0);
                    Angle(pelvis, -g * 1.2f);
                    Angle(torso, 2f + 7f * run + g * (def.ranger ? 1.4f : 1f));
                    Angle(head, -1f - 3f * run - Mathf.Sin(phase - .25f) * .9f);
                    WalkLeg(nearThigh, nearShin, nearFoot, def.nearFootSole, nearCalf, nearFootHeight, cycle, hipY, run);
                    WalkLeg(farThigh, farShin, farFoot, def.farFootSole, farCalf, farFootHeight, Mathf.Repeat(cycle + .5f, 1f), hipY, run);
                    float swing = 1f + 1.5f * run;
                    nearUpper -= g * (def.ranger ? 5 : 3) * swing; nearLower += Mathf.Sin(phase + .65f) * (def.ranger ? 3 : 2) * swing;
                    farUpper += g * (def.ranger ? 15 : 10) * swing; farLower += Mathf.Sin(phase + .65f) * (def.ranger ? 7 : 5) * swing;
                    if (cape) Angle(cape, Mathf.Sin(phase - .5f) * 4f + Mathf.Sin(phase * 2) * .9f);
                }
                else
                {
                    float nearLeg = g * 22, farLeg = -g * 22;
                    float nearKnee = -Mathf.Max(0, -g) * 32, farKnee = -Mathf.Max(0, g) * 32;
                    Angle(nearThigh, nearLeg); Angle(farThigh, farLeg); Angle(nearShin, nearKnee); Angle(farShin, farKnee);
                    float low = Mathf.Min(SoleOffset(def.nearSole, nearLeg, nearKnee), SoleOffset(def.farSole, farLeg, farKnee));
                    body.localPosition = new Vector3(0, .02f - low, 0);
                    Angle(torso, 2); Angle(head, -1 + g * .7f);
                    if (cape) Angle(cape, g * 5);
                    nearUpper += g * -8; farUpper += g * 13;
                }
                if (p.climbing)
                {
                    nearUpper = 165 + g * 15; nearLower = 10; farUpper = 165 - g * 15; farLower = 10;
                    Angle(nearThigh, 10 + g * 12); Angle(farThigh, 10 - g * 12); Angle(nearShin, -20); Angle(farShin, -20);
                    body.localPosition = new Vector3(0, 1.45f, 0);
                }
            }
            else if (clip == "jump")
            {
                Angle(nearThigh, 40); Angle(nearShin, -60); Angle(farThigh, -15); Angle(farShin, -40);
                Angle(torso, -5); nearUpper = 30; farUpper = -40;
                if (cape) Angle(cape, 14);
            }
            else if (clip == "fall")
            {
                Angle(nearThigh, 20); Angle(nearShin, -25); Angle(farThigh, 0); Angle(farShin, -25);
                Angle(torso, 4); nearUpper = 50; farUpper = -50; nearLower = 20; farLower = 20;
                if (cape) Angle(cape, -18);
            }
            else if (clip == "hurt")
            {
                float t = Mathf.Clamp01(clock / Tuning.HurtFlash);
                float k = 1 - t * t;
                Angle(torso, -18 * k); Angle(head, -12 * k);
                nearUpper = -50 * k; farUpper = 50 * k;
                body.localPosition = new Vector3(0, 1.5f - .05f * k, 0);
            }
            else if (clip == "death")
            {
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(clock / .45f));
                visual.localRotation = Quaternion.Euler(0, 0, 80 * t);
                body.localPosition = new Vector3(0, Mathf.Lerp(1.5f, 1.25f, t), 0);   // lies along the ground behind the feet
                Angle(torso, -10 * t); Angle(head, -15 * t);
                nearUpper = Mathf.Lerp(nearUpper, 60, t); farUpper = Mathf.Lerp(farUpper, -70, t);
                Angle(nearThigh, 15 * t); Angle(farThigh, -10 * t); Angle(nearShin, -20 * t); Angle(farShin, -15 * t);
            }
            else if (clip == h.basicAnimA || clip == h.basicAnimB || clip == h.skillAnim)
            {
                bool skill = clip == h.skillAnim;
                float t = Mathf.Clamp01(clock / (skill ? Tuning.SkillAttackVisual : Tuning.BasicAttackVisual));
                float snap = 1 - (1 - t) * (1 - t);            // fast out, settling in
                if (def.ranger)
                {
                    // Bow arm straight out; the draw hand pulls back to the cheek and lets go.
                    float aim = skill ? 100 : 90;
                    nearUpper = aim; nearLower = 0;
                    farUpper = Mathf.Lerp(80, 60, snap); farLower = Mathf.Lerp(-60, 10, snap);
                    Angle(torso, 4); Angle(head, 2);
                    if (skill) body.localPosition = new Vector3(0, 1.5f + .06f * Mathf.Sin(t * Mathf.PI), 0);
                }
                else if (skill)
                {
                    // Dragon Strike: both arms drive the spear forward as the knight lunges.
                    nearUpper = Mathf.Lerp(-60, 95, snap); nearLower = Mathf.Lerp(-40, 0, snap);
                    farUpper = Mathf.Lerp(-30, 70, snap); farLower = Mathf.Lerp(20, 0, snap);
                    Angle(torso, 14 * snap); Angle(head, -6 * snap);
                    Angle(nearThigh, 35 * snap); Angle(nearShin, -30 * snap); Angle(farThigh, -25 * snap);
                    body.localPosition = new Vector3(.25f * snap, 1.5f - .12f * snap, 0);
                    weaponTurn = -90;
                    if (cape) Angle(cape, 22 * snap);
                }
                else if (clip == h.basicAnimA)
                {
                    // Thrust: the spear levels out and shoots forward.
                    nearUpper = Mathf.Lerp(-10, 85, snap); nearLower = Mathf.Lerp(-30, 0, snap);
                    farUpper = Mathf.Lerp(-10, -45, snap); farLower = 10;
                    Angle(torso, 8 * snap); Angle(nearThigh, 18 * snap); Angle(farThigh, -12 * snap);
                    body.localPosition = new Vector3(.12f * snap, 1.5f, 0);
                    weaponTurn = -90;
                    if (cape) Angle(cape, 10 * snap);
                }
                else
                {
                    // Sweep: the spear comes over the shoulder and down across the front.
                    nearUpper = Mathf.Lerp(150, 25, snap); nearLower = Mathf.Lerp(-20, 10, snap);
                    farUpper = Mathf.Lerp(-30, 20, snap); farLower = 15;
                    Angle(torso, Mathf.Lerp(-6, 8, snap)); Angle(nearThigh, 12 * snap);
                    alignWeapon = true;
                    if (cape) Angle(cape, Mathf.Lerp(-8, 12, snap));
                }
            }
            else if (p.crouching)
            {
                body.localPosition = new Vector3(0, 1.05f, 0);
                Angle(nearThigh, 55); Angle(nearShin, -95); Angle(farThigh, 45); Angle(farShin, -95);
                Angle(torso, 12 + wave * .7f); Angle(head, -6);
                nearUpper += 10; farUpper += 10;
            }
            else
            {
                // Idle: a breath and the slightest sway.
                Angle(nearThigh, 3); Angle(farThigh, -3);
                body.localPosition = new Vector3(0, 1.5f + wave * .018f, 0);
                nearUpper += wave; farUpper += wave * -.6f;
            }

            Angle(nearArm, nearUpper); Angle(nearForearm, nearLower);
            Angle(farArm, farUpper); Angle(farForearm, farLower);
            Angle(weapon, alignWeapon ? 180 : -nearUpper - nearLower + weaponTurn + (clip == "run" ? Mathf.Sin(walkPhase * Mathf.PI * 2 - .25f) * 1.5f : wave * .5f));
        }

        // One leg of the walk (from the rig pack, revision 2): the boot slides back while planted, then
        // lifts and swings forward; a two-bone solve puts the knee where the thigh and calf meet.
        void WalkLeg(Transform thigh, Transform shin, Transform foot, float[] sole, float calfLength, float footHeight, float phase, float hipY, float run)
        {
            const float thighLength = .66f;
            float stance = Mathf.Lerp(def.ranger ? .60f : .64f, .5f, run);
            float stride = Mathf.Lerp(def.ranger ? .33f : .30f, .58f, run);
            float x, lift = 0, pitch;
            if (phase < stance)
            {
                float t = phase / stance;
                x = Mathf.Lerp(stride, -stride, t);
                // The toe keeps pointing the way the hero walks: heel-first in front, flat under the body,
                // only a slight heel lift as the leg trails behind.
                pitch = t < .15f ? Mathf.Lerp(4, 0, t / .15f) : t > .7f ? Mathf.Lerp(0, -9, (t - .7f) / .3f) : 0;
            }
            else
            {
                float t = (phase - stance) / (1 - stance);
                float ease = t * t * (3 - 2 * t);
                x = Mathf.Lerp(-stride, stride, ease);
                lift = Mathf.Sin(t * Mathf.PI) * Mathf.Lerp(def.ranger ? .17f : .13f, .3f, run);
                pitch = Mathf.Lerp(-9, 4, ease) + Mathf.Sin(t * Mathf.PI) * 3;
            }
            float footLow = float.PositiveInfinity, radians = pitch * Mathf.Deg2Rad;
            if (sole != null)
                for (int i = 0; i + 1 < sole.Length; i += 2)
                    footLow = Mathf.Min(footLow, (sole[i] * Mathf.Sin(radians) + sole[i + 1] * Mathf.Cos(radians)) * footHeight);
            if (float.IsPositiveInfinity(footLow)) footLow = -footHeight * .85f;
            var target = new Vector2(x, .02f - footLow + lift - hipY);
            float distance = Mathf.Clamp(target.magnitude, Mathf.Abs(thighLength - calfLength) + .001f, thighLength + calfLength - .001f);
            target = target.normalized * distance;
            float a = Mathf.Atan2(target.y, target.x);
            float bend = Mathf.Acos(Mathf.Clamp((thighLength * thighLength + distance * distance - calfLength * calfLength) / (2 * thighLength * distance), -1, 1));
            float upper = a + bend;
            var knee = new Vector2(Mathf.Cos(upper), Mathf.Sin(upper)) * thighLength;
            float lower = Mathf.Atan2(target.y - knee.y, target.x - knee.x);
            float thighDegrees = upper * Mathf.Rad2Deg + 90;
            float shinDegrees = lower * Mathf.Rad2Deg + 90;
            Angle(thigh, thighDegrees);
            Angle(shin, shinDegrees - thighDegrees);
            Angle(foot, pitch - shinDegrees);          // the ankle keeps the boot flat instead of pointing the toe
        }

        // Whole shins for standing poses; calf + boot slices while walking.
        void LegArt(bool walking)
        {
            if (nearFoot == null || walking == walkingArt) return;
            walkingArt = walking;
            nearFullLeg.enabled = farFullLeg.enabled = !walking;
            nearCalfArt.enabled = farCalfArt.enabled = nearFootArt.enabled = farFootArt.enabled = walking;
            if (!walking) { Angle(nearFoot, 0); Angle(farFoot, 0); }
        }

        void Stand()
        {
            body.localPosition = new Vector3(0, 1.5f, 0);
            Angle(torso, 0); Angle(head, 0); Angle(pelvis, 0);
            Angle(nearThigh, 0); Angle(farThigh, 0); Angle(nearShin, 0); Angle(farShin, 0);
            if (cape) Angle(cape, 0);
        }

        // How far the lowest boot pixel of a swung leg sits below the hip (rig units, negative = below).
        float SoleOffset(float[] sole, float upper, float lower)
        {
            float a = upper * Mathf.Deg2Rad, b = (upper + lower) * Mathf.Deg2Rad;
            float h = def.shinHeight;
            float low = -.66f * Mathf.Cos(a) - .95f * h;
            if (sole != null && sole.Length >= 2)
            {
                low = float.PositiveInfinity;
                for (int i = 0; i + 1 < sole.Length; i += 2) low = Mathf.Min(low, sole[i] * h * Mathf.Sin(b) + sole[i + 1] * h * Mathf.Cos(b));
                low += -.66f * Mathf.Cos(a);
            }
            return low;
        }

        static void Angle(Transform t, float degrees) { t.localRotation = Quaternion.Euler(0, 0, degrees); }

        static Transform Joint(string name, Transform parent, Vector2 at)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = at;
            return t;
        }

        SpriteRenderer Picture(Sprite sprite, Transform parent, float height, int order)
        {
            var t = Joint(sprite.name, parent, Vector2.zero);
            var sr = t.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            t.localScale = Vector3.one * (height / Mathf.Max(.001f, sprite.bounds.size.y));
            renderers.Add(sr);
            return sr;
        }

        // Cuts the pieces out of the atlas once per rig. Entries 12: cape (or null); 13-16: near calf, near boot,
        // far calf, far boot, sliced from the shin pieces for the walk (top 78% / bottom 28%, ankle 24% up).
        static Sprite[] Pieces(RigDef def)
        {
            Sprite[] result;
            if (sheets.TryGetValue(def.hero, out result)) return result;
            var tex = Resources.Load<Texture2D>(def.atlas);
            if (tex == null) { sheets[def.hero] = null; return null; }
            result = new Sprite[17];
            string[] names = { "Head", "Torso", "Pelvis", "NearUpperArm", "NearForearm", "FarUpperArm", "FarForearm", "NearThigh", "NearShin", "FarThigh", "FarShin", "Weapon" };
            for (int i = 0; i < 12; i++) result[i] = Cut(tex, def.parts[i], names[i]);
            if (!string.IsNullOrEmpty(def.capeAtlas))
            {
                var capeTex = Resources.Load<Texture2D>(def.capeAtlas);
                if (capeTex != null) result[12] = Cut(capeTex, def.cape, "Cape");
            }
            Slice(tex, def.parts[8], result, 13, "Near");
            Slice(tex, def.parts[10], result, 15, "Far");
            sheets[def.hero] = result;
            return result;
        }

        static void Slice(Texture2D tex, RigPart shin, Sprite[] into, int at, string side)
        {
            float cut = Mathf.Round(shin.height * .22f), footTop = Mathf.Round(shin.height * .28f), ankle = Mathf.Round(shin.height * .24f);
            var calfRect = new Rect(shin.x, shin.y + cut, shin.width, shin.height - cut);
            var footRect = new Rect(shin.x, shin.y, shin.width, footTop);
            float pivotY = shin.pivotY * shin.height;
            into[at] = Sprite.Create(tex, calfRect, new Vector2(shin.pivotX, (pivotY - cut) / calfRect.height), 100f, 0, SpriteMeshType.FullRect);
            into[at].name = side + "Calf";
            into[at + 1] = Sprite.Create(tex, footRect, new Vector2(shin.pivotX, ankle / footTop), 100f, 0, SpriteMeshType.FullRect);
            into[at + 1].name = side + "Foot";
        }

        static Sprite Cut(Texture2D tex, RigPart part, string name)
        {
            var rect = new Rect(part.x, part.y, Mathf.Min(part.width, tex.width - part.x), Mathf.Min(part.height, tex.height - part.y));
            var s = Sprite.Create(tex, rect, new Vector2(part.pivotX, part.pivotY), 100f, 0, SpriteMeshType.FullRect);
            s.name = name;
            return s;
        }
    }
}
