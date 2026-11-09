using UnityEngine;

namespace AshenVale
{
    static class RollingGround
    {
        public const float Lowest = -1.25f;
        public const float Highest = 1.65f;
        static readonly Vector2[] Profile =
        {
            new Vector2(0, 0), new Vector2(7, 0), new Vector2(16, 1.45f), new Vector2(22, 1.45f),
            new Vector2(31, Lowest), new Vector2(38, Lowest), new Vector2(48, Highest),
            new Vector2(55, Highest), new Vector2(64, -.8f), new Vector2(69, -.8f),
            new Vector2(72, 0), new Vector2(74, 0)
        };

        public static float HeightAt(float x)
        {
            for (int i = 1; i < Profile.Length; i++)
            {
                var end = Profile[i];
                if (x > end.x) continue;
                var start = Profile[i - 1];
                float t = Mathf.InverseLerp(start.x, end.x, x);
                return Tuning.Ground + Mathf.Lerp(start.y, end.y, t * t * (3f - 2f * t));
            }
            return Tuning.Ground;
        }

        public static MapDef Configure(MapDef def)
        {
            def.rollingGround = true;
            // Emberfields has two permanent exits. Use the ground-level gates
            // as the canonical entries for both rendering and interaction.
            def.portals = new[]
            {
                new Portal { key = 'A', targetKey = 'A', targetMap = Maps.Emberhaven, x = 1.5f, y = HeightAt(1.5f) },
                new Portal { key = 'B', targetKey = 'B', targetMap = Maps.AmberleafTrail, x = 72.5f, y = HeightAt(72.5f) }
            };
            for (int i = 0; i < def.ladders.Length; i++)
            {
                var ladder = def.ladders[i];
                if (ladder.bottom == Tuning.Ground)
                    ladder.bottom = HeightAt(ladder.x);
                def.ladders[i] = ladder;
            }
            return def;
        }
    }
}
