using UnityEngine;

namespace AshenVale.CharacterRigs
{
    [CreateAssetMenu(menuName = "Ashen Vale/Character Rig Definition")]
    public sealed class CharacterRigDefinition : ScriptableObject
    {
        public bool ranger;
        // Row-major, top-left first: head, torso, pelvis, near upper arm, near forearm,
        // far upper arm, far forearm, near thigh, near shin/boot, far thigh, far shin/boot, weapon.
        public Sprite[] parts = new Sprite[12];
        public Sprite cape;
        public Vector2[] nearSole, farSole;
        public float headHeight = 1.05f, torsoHeight = 1.05f, pelvisHeight = .65f;
        public float upperArmHeight = .58f, forearmHeight = .60f;
        public float thighHeight = .75f, shinHeight = .85f;
        public float weaponHeight = 3.95f;
    }
}
