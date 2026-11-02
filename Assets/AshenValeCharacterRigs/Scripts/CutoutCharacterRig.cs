using UnityEngine;

namespace AshenVale.CharacterRigs
{
    // Visual-only articulated sprite rig. It never changes player input, physics, combat or saved data.
    public sealed class CutoutCharacterRig : MonoBehaviour
    {
        public CharacterRigDefinition definition;
        public bool walking, facingRight = true;
        [HideInInspector] public Transform visual, body, torso, head, nearArm, nearForearm, farArm, farForearm;
        [HideInInspector] public Transform nearThigh, nearShin, farThigh, farShin, weapon, cape, pelvis, nearFoot, farFoot;
        [HideInInspector] public SpriteRenderer nearFullLeg, farFullLeg, nearCalfArt, farCalfArt, nearFootArt, farFootArt;
        [Range(.5f, 2f)] public float walkPlaybackSpeed = 1f;
        Animator animator;
        float clock;
        static readonly int Walking = Animator.StringToHash("Walking");

        void Awake()
        {
            if (!body && definition) Build(definition);
            animator = GetComponent<Animator>();
        }
        void LateUpdate()
        {
            if (!body) return;
            visual.localScale = new Vector3(facingRight ? 1 : -1, 1, 1);
            SetLegArt(walking && definition.HasFeet);
            if (animator && animator.runtimeAnimatorController)
            {
                animator.SetBool(Walking, walking);
                animator.speed = walking ? walkPlaybackSpeed : 1f;
            }
            else { clock += Time.deltaTime * (walking ? walkPlaybackSpeed : 1f); ApplyPose(clock, walking); }
        }
        public void SetMovement(float signedSpeed)
        {
            walking = Mathf.Abs(signedSpeed) > .05f;
            if (walking) facingRight = signedSpeed > 0;
        }
        public void Build(CharacterRigDefinition source)
        {
            definition = source;
            if (!source || source.parts == null || source.parts.Length != 12) throw new System.ArgumentException("Rig needs twelve part sprites.");
            if (visual) { if (Application.isPlaying) Destroy(visual.gameObject); else DestroyImmediate(visual.gameObject); }
            visual = Joint("Visual", transform, Vector2.zero);
            visual.gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
            body = Joint("Body", visual, new Vector2(0, 1.5f));
            pelvis = Joint("Pelvis", body, Vector2.zero);
            Picture("PelvisArt", source.parts[2], pelvis, source.pelvisHeight, 2);
            torso = Joint("Torso", body, new Vector2(0, .06f));
            Picture("TorsoArt", source.parts[1], torso, source.torsoHeight, 0);
            head = Joint("Head", torso, new Vector2(source.ranger ? .02f : -.08f, source.ranger ? .82f : .96f));
            Picture("HeadArt", source.parts[0], head, source.headHeight, source.ranger ? -1 : 3);
            nearArm = Joint("NearArm", body, new Vector2(source.ranger ? .20f : -.36f, source.ranger ? .84f : .96f));
            Picture("NearUpperArmArt", source.parts[3], nearArm, source.upperArmHeight, 4);
            nearForearm = Joint("NearForearm", nearArm, new Vector2(source.ranger ? -.08f : -.04f, -.5f));
            Picture("NearForearmArt", source.parts[4], nearForearm, source.forearmHeight, 6);
            farArm = Joint("FarArm", body, new Vector2(source.ranger ? -.30f : .34f, .94f));
            Picture("FarUpperArmArt", source.parts[5], farArm, source.upperArmHeight, -4);
            farForearm = Joint("FarForearm", farArm, new Vector2(-.05f, -.5f));
            Picture("FarForearmArt", source.parts[6], farForearm, source.forearmHeight, -3);
            nearThigh = Joint("NearThigh", body, new Vector2(.18f, 0));
            Picture("NearThighArt", source.parts[7], nearThigh, source.thighHeight, 1);
            nearShin = Joint("NearShin", nearThigh, new Vector2(0, -.66f));
            nearFullLeg = Picture("NearShinArt", source.parts[8], nearShin, source.shinHeight, 2);
            farThigh = Joint("FarThigh", body, new Vector2(-.18f, 0));
            Picture("FarThighArt", source.parts[9], farThigh, source.thighHeight, -5);
            farShin = Joint("FarShin", farThigh, new Vector2(0, -.66f));
            farFullLeg = Picture("FarShinArt", source.parts[10], farShin, source.shinHeight, -4);
            nearFoot = farFoot = null;
            nearCalfArt = farCalfArt = nearFootArt = farFootArt = null;
            if (source.HasFeet)
            {
                nearCalfArt = Picture("NearCalfArt", source.nearCalf, nearShin, source.shinHeight * source.nearCalf.rect.height / source.parts[8].rect.height, 2);
                farCalfArt = Picture("FarCalfArt", source.farCalf, farShin, source.shinHeight * source.farCalf.rect.height / source.parts[10].rect.height, -4);
                nearFoot = Joint("NearFoot", nearShin, new Vector2(0, -CalfLength(true)));
                farFoot = Joint("FarFoot", farShin, new Vector2(0, -CalfLength(false)));
                nearFootArt = Picture("NearFootArt", source.nearFoot, nearFoot, FootHeight(true), 3);
                farFootArt = Picture("FarFootArt", source.farFoot, farFoot, FootHeight(false), -3);
            }
            weapon = Joint("Weapon", nearForearm, new Vector2(source.ranger ? .08f : .02f, -.5f));
            Picture("WeaponArt", source.parts[11], weapon, source.weaponHeight, 5);
            if (source.cape)
            {
                cape = Joint("Cape", body, new Vector2(-.22f, 1.05f));
                Picture("CapeArt", source.cape, cape, 2.2f, -7);
            }
            ApplyPose(0, false);
        }
        public Transform[] AnimatedJoints()
        {
            var joints = new System.Collections.Generic.List<Transform> { body, torso, head, nearArm, nearForearm, farArm, farForearm, nearThigh, nearShin, farThigh, farShin, weapon };
            if (cape) joints.Add(cape);
            if (pelvis) joints.Add(pelvis);
            if (nearFoot) joints.Add(nearFoot);
            if (farFoot) joints.Add(farFoot);
            return joints.ToArray();
        }
        public void ApplyPose(float seconds, bool walk)
        {
            if (!body) return;
            SetLegArt(walk && definition.HasFeet);
            if (walk && definition.HasFeet) { ApplyWalkingPose(seconds); return; }
            // Original idle, including its full unsliced boot artwork, is retained.
            if (pelvis) Angle(pelvis, 0);
            ApplyOriginalPose(seconds, walk);
        }
        void ApplyOriginalPose(float seconds, bool walk)
        {
            if (!body) return;
            float p = seconds * Mathf.PI * 2 / (walk ? .8f : 2.4f);
            float wave = Mathf.Sin(p);
            if (cape) Angle(cape, wave * (walk ? 5 : 1.5f));
            float nearUpperLeg = walk ? wave * 22 : 3, farUpperLeg = walk ? -wave * 22 : -3;
            float nearLowerLeg = walk ? -Mathf.Max(0, -wave) * 32 : 0, farLowerLeg = walk ? -Mathf.Max(0, wave) * 32 : 0;
            float hipY = walk ? .02f - Mathf.Min(SoleOffset(definition.parts[8], definition.nearSole, nearUpperLeg, nearLowerLeg), SoleOffset(definition.parts[10], definition.farSole, farUpperLeg, farLowerLeg)) : 1.5f + wave * .018f;
            body.localPosition = new Vector3(0, hipY, 0);
            Angle(torso, walk ? 2 : wave * .7f);
            Angle(head, walk ? -1 + wave * .7f : -wave * .6f);
            Angle(nearThigh, nearUpperLeg);
            Angle(farThigh, farUpperLeg);
            Angle(nearShin, nearLowerLeg);
            Angle(farShin, farLowerLeg);
            float nearUpper = (definition.ranger ? 18 : -30) + wave * (walk ? -8 : 1);
            float nearLower = definition.ranger ? 40 : 10;
            Angle(nearArm, nearUpper); Angle(nearForearm, nearLower);
            Angle(farArm, (definition.ranger ? -20 : -3) + wave * (walk ? 13 : -.6f));
            Angle(farForearm, definition.ranger ? 35 : 8);
            // Cancel arm rotation so the carried weapon stays upright while hands and body move.
            Angle(weapon, -nearUpper - nearLower + wave * (walk ? 2 : .5f));
        }
        void ApplyWalkingPose(float seconds)
        {
            float cycle = Mathf.Repeat(seconds / .8f, 1f), phase = cycle * Mathf.PI * 2;
            float wave = Mathf.Sin(phase);
            float hipY = 1.40f + Mathf.Cos(phase * 2) * .014f;
            body.localPosition = new Vector3(wave * .012f, hipY, 0);
            if (pelvis) Angle(pelvis, -wave * 1.2f);
            Angle(torso, 2f + wave * (definition.ranger ? 1.4f : 1f));
            Angle(head, -1f - Mathf.Sin(phase - .25f) * .9f);
            WalkLeg(nearThigh, nearShin, nearFoot, definition.nearFootSole, CalfLength(true), FootHeight(true), cycle, hipY);
            WalkLeg(farThigh, farShin, farFoot, definition.farFootSole, CalfLength(false), FootHeight(false), Mathf.Repeat(cycle + .5f, 1f), hipY);
            float upper = (definition.ranger ? 18 : -30) - wave * (definition.ranger ? 5 : 3);
            float fore = (definition.ranger ? 40 : 10) + Mathf.Sin(phase + .65f) * (definition.ranger ? 3 : 2);
            Angle(nearArm, upper); Angle(nearForearm, fore);
            Angle(farArm, (definition.ranger ? -20 : -3) + wave * (definition.ranger ? 15 : 10));
            Angle(farForearm, (definition.ranger ? 35 : 8) + Mathf.Sin(phase + .65f) * (definition.ranger ? 7 : 5));
            Angle(weapon, -upper - fore + Mathf.Sin(phase - .25f) * 1.5f);
            if (cape) Angle(cape, Mathf.Sin(phase - .5f) * 4f + Mathf.Sin(phase * 2) * .9f);
        }
        void WalkLeg(Transform thigh, Transform shin, Transform foot, Vector2[] sole, float calfLength, float footHeight, float phase, float hipY)
        {
            const float thighLength = .66f;
            float stance = definition.ranger ? .60f : .64f;
            float stride = definition.ranger ? .33f : .30f;
            float x, lift = 0, pitch;
            if (phase < stance)
            {
                float t = phase / stance;
                x = Mathf.Lerp(stride, -stride, t);
                pitch = t < .15f ? Mathf.Lerp(6, 0, t / .15f) : t > .75f ? Mathf.Lerp(0, -10, (t - .75f) / .25f) : 0;
            }
            else
            {
                float t = (phase - stance) / (1 - stance);
                float ease = t * t * (3 - 2 * t);
                x = Mathf.Lerp(-stride, stride, ease);
                lift = Mathf.Sin(t * Mathf.PI) * (definition.ranger ? .17f : .13f);
                pitch = Mathf.Lerp(-10, 6, ease) + Mathf.Sin(t * Mathf.PI) * 8;
            }
            float footLow = float.PositiveInfinity, radians = pitch * Mathf.Deg2Rad;
            if (sole != null) foreach (var point in sole)
                footLow = Mathf.Min(footLow, (point.x * Mathf.Sin(radians) + point.y * Mathf.Cos(radians)) * footHeight);
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
            // Independent ankle prevents calf bend from turning the entire boot into a pointed toe.
            Angle(foot, pitch - shinDegrees);
        }
        float CalfLength(bool near)
        {
            Sprite full = definition.parts[near ? 8 : 10], foot = near ? definition.nearFoot : definition.farFoot;
            return definition.shinHeight * (full.pivot.y - foot.pivot.y) / full.rect.height;
        }
        float FootHeight(bool near)
        {
            Sprite full = definition.parts[near ? 8 : 10], foot = near ? definition.nearFoot : definition.farFoot;
            return definition.shinHeight * foot.rect.height / full.rect.height;
        }
        void SetLegArt(bool walkingArt)
        {
            if (nearFullLeg) nearFullLeg.enabled = !walkingArt;
            if (farFullLeg) farFullLeg.enabled = !walkingArt;
            if (nearCalfArt) nearCalfArt.enabled = walkingArt;
            if (farCalfArt) farCalfArt.enabled = walkingArt;
            if (nearFootArt) nearFootArt.enabled = walkingArt;
            if (farFootArt) farFootArt.enabled = walkingArt;
        }
        float SoleOffset(Sprite shin, Vector2[] sole, float upper, float lower)
        {
            float a = upper * Mathf.Deg2Rad, b = (upper + lower) * Mathf.Deg2Rad;
            float h = definition.shinHeight;
            if (sole != null && sole.Length > 0)
            {
                float low = float.PositiveInfinity;
                foreach (var point in sole) low = Mathf.Min(low, point.x * h * Mathf.Sin(b) + point.y * h * Mathf.Cos(b));
                return -.66f * Mathf.Cos(a) + low;
            }
            float w = shin.rect.width / shin.rect.height * h;
            float px = shin.pivot.x / shin.rect.width, py = shin.pivot.y / shin.rect.height;
            float side = Mathf.Sin(b) >= 0 ? -px * w : (1 - px) * w;
            return -.66f * Mathf.Cos(a) - py * h * Mathf.Cos(b) + side * Mathf.Sin(b);
        }
        static void Angle(Transform target, float degrees) { target.localRotation = Quaternion.Euler(0, 0, degrees); }
        static Transform Joint(string name, Transform parent, Vector2 position)
        { var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position; return t; }
        static SpriteRenderer Picture(string name, Sprite sprite, Transform parent, float height, int order)
        {
            if (!sprite) throw new System.ArgumentException("Missing sprite: " + name);
            var t = Joint(name, parent, Vector2.zero); var sr = t.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sortingOrder = order;
            t.localScale = Vector3.one * (height / Mathf.Max(.001f, sprite.bounds.size.y));
            return sr;
        }
    }
}
