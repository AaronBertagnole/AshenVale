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
        readonly Transform nearThigh, nearShin, farThigh, farShin, weapon, cape;
        readonly SortingGroup group;
        readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        float walkPhase, breath;

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
            Picture(parts[2], body, def.pelvisHeight, 2);
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
            Picture(parts[8], nearShin, def.shinHeight, 2);
            farThigh = Joint("FarThigh", body, new Vector2(-.18f, 0));
            Picture(parts[9], farThigh, def.thighHeight, -5);
            farShin = Joint("FarShin", farThigh, new Vector2(0, -.66f));
            Picture(parts[10], farShin, def.shinHeight, -4);
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

            if (clip == "run")
            {
                walkPhase += frameDt * Mathf.Max(2.5f, Mathf.Abs(p.vx)) / 4.4f;
                float g = Mathf.Sin(walkPhase * Mathf.PI * 2);
                float nearLeg = g * 22, farLeg = -g * 22;
                float nearKnee = -Mathf.Max(0, -g) * 32, farKnee = -Mathf.Max(0, g) * 32;
                Angle(nearThigh, nearLeg); Angle(farThigh, farLeg); Angle(nearShin, nearKnee); Angle(farShin, farKnee);
                float low = Mathf.Min(SoleOffset(def.nearSole, nearLeg, nearKnee), SoleOffset(def.farSole, farLeg, farKnee));
                body.localPosition = new Vector3(0, .02f - low, 0);
                Angle(torso, 2); Angle(head, -1 + g * .7f);
                if (cape) Angle(cape, g * 5);
                nearUpper += g * -8; farUpper += g * 13;
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
            Angle(weapon, alignWeapon ? 180 : -nearUpper - nearLower + weaponTurn + (clip == "run" ? Mathf.Sin(walkPhase * Mathf.PI * 2) * 2 : wave * .5f));
        }

        void Stand()
        {
            body.localPosition = new Vector3(0, 1.5f, 0);
            Angle(torso, 0); Angle(head, 0);
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

        void Picture(Sprite sprite, Transform parent, float height, int order)
        {
            var t = Joint(sprite.name, parent, Vector2.zero);
            var sr = t.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            t.localScale = Vector3.one * (height / Mathf.Max(.001f, sprite.bounds.size.y));
            renderers.Add(sr);
        }

        // Cuts the pieces out of the atlas once per rig (13th entry: the cape, or null).
        static Sprite[] Pieces(RigDef def)
        {
            Sprite[] result;
            if (sheets.TryGetValue(def.hero, out result)) return result;
            var tex = Resources.Load<Texture2D>(def.atlas);
            if (tex == null) { sheets[def.hero] = null; return null; }
            result = new Sprite[13];
            string[] names = { "Head", "Torso", "Pelvis", "NearUpperArm", "NearForearm", "FarUpperArm", "FarForearm", "NearThigh", "NearShin", "FarThigh", "FarShin", "Weapon" };
            for (int i = 0; i < 12; i++) result[i] = Cut(tex, def.parts[i], names[i]);
            if (!string.IsNullOrEmpty(def.capeAtlas))
            {
                var capeTex = Resources.Load<Texture2D>(def.capeAtlas);
                if (capeTex != null) result[12] = Cut(capeTex, def.cape, "Cape");
            }
            sheets[def.hero] = result;
            return result;
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
