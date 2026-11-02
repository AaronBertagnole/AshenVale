using UnityEngine;

namespace AshenVale.CharacterRigs
{
    // Visual-only articulated sprite rig. It never changes player input, physics, combat or saved data.
    public sealed class CutoutCharacterRig : MonoBehaviour
    {
        public CharacterRigDefinition definition;
        public bool walking, facingRight = true;
        [HideInInspector] public Transform visual, body, torso, head, nearArm, nearForearm, farArm, farForearm;
        [HideInInspector] public Transform nearThigh, nearShin, farThigh, farShin, weapon, cape;
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
            if (animator && animator.runtimeAnimatorController) animator.SetBool(Walking, walking);
            else { clock += Time.deltaTime; ApplyPose(clock, walking); }
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
            Picture("Pelvis", source.parts[2], body, source.pelvisHeight, 2);
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
            Picture("NearShinArt", source.parts[8], nearShin, source.shinHeight, 2);
            farThigh = Joint("FarThigh", body, new Vector2(-.18f, 0));
            Picture("FarThighArt", source.parts[9], farThigh, source.thighHeight, -5);
            farShin = Joint("FarShin", farThigh, new Vector2(0, -.66f));
            Picture("FarShinArt", source.parts[10], farShin, source.shinHeight, -4);
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
            return joints.ToArray();
        }
        public void ApplyPose(float seconds, bool walk)
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
        static void Picture(string name, Sprite sprite, Transform parent, float height, int order)
        {
            if (!sprite) throw new System.ArgumentException("Missing sprite: " + name);
            var t = Joint(name, parent, Vector2.zero); var sr = t.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sortingOrder = order;
            t.localScale = Vector3.one * (height / Mathf.Max(.001f, sprite.bounds.size.y));
        }
    }
}
