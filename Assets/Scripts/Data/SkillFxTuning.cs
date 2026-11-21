using System;
using UnityEngine;

namespace AshenVale
{
    // The knobs for skill effects, as an asset you can edit in the Inspector WHILE THE GAME RUNS:
    // select Assets/Resources/SkillFxTuning in the Project window, press Play, cast, and drag the numbers.
    // Changes apply to the next cast and (being an asset) stay after you stop playing.
    //
    // A skill effect is three beats: the wind-up at the caster, the hit (the brightest moment, and the tick the
    // damage lands on), and the burn-down. Each skill's section below is laid out in that order.
    [CreateAssetMenu(fileName = "SkillFxTuning", menuName = "AshenVale/Skill FX Tuning")]
    public sealed class SkillFxTuning : ScriptableObject
    {
        [Serializable]
        public sealed class DragonfireTuning
        {
            [Header("1. Wind-up")]
            [Tooltip("Seconds from the key press to the damage landing. The first three Frame Seconds (before the full breath) should add up to about this.")]
            [Range(0f, .6f)] public float hitDelay = .22f;
            [Tooltip("Embers drawn in toward the spear during the wind-up.")]
            [Range(0, 30)] public int gatherEmbers = 10;

            [Header("2. The flame (six painted frames)")]
            [Tooltip("Seconds each frame stays up: rear-up, maw opens, first fire, full breath, burning down, embers.")]
            public float[] frameSeconds = { .06f, .08f, .08f, .24f, .16f, .2f };
            [Tooltip("Where the ground glow sits, in units ahead of the Knight (negative: behind him).")]
            [Range(-1.5f, 2.5f)] public float forward = .55f;
            [Tooltip("Lifts or sinks the whole effect, in units.")]
            [Range(-1f, 1.5f)] public float lift = -.04f;
            [Tooltip("1 = the flame's tip lands exactly on the skill's reach. Smaller or larger scales the whole picture.")]
            [Range(.4f, 1.8f)] public float size = 1f;

            [Header("3. The hit")]
            [Tooltip("Bright burst at the flame's mouth on the hit. Size in units; 0 turns it off.")]
            [Range(0f, 5f)] public float flashSize = 2.4f;
            [Range(.03f, .4f)] public float flashSeconds = .14f;
            [Tooltip("Units ahead of the Knight where the flash sits.")]
            [Range(0f, 5f)] public float flashForward = 2.2f;
            [Tooltip("How far the camera kicks, in units; 0 turns it off.")]
            [Range(0f, .6f)] public float shake = .16f;
            [Range(.05f, .6f)] public float shakeSeconds = .2f;
            [Tooltip("The game holds still this long when the flame hits something (solo play only); 0 turns it off.")]
            [Range(0f, .15f)] public float hitPause = .05f;
        }

        public DragonfireTuning dragonfire = new DragonfireTuning();

        static SkillFxTuning loaded;
        // The asset under Resources, or the defaults above if it is missing.
        public static SkillFxTuning Get
        {
            get
            {
                if (loaded == null) loaded = Resources.Load<SkillFxTuning>("SkillFxTuning");
                if (loaded == null) loaded = CreateInstance<SkillFxTuning>();
                return loaded;
            }
        }
    }
}
