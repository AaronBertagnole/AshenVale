using System.Collections.Generic;
using UnityEngine;

namespace AshenVale
{
    public sealed partial class WorldView
    {
        sealed class AttackSpark
        {
            public SpriteRenderer renderer;
            public Vector2 origin, velocity;
            public Color color;
            public float age, duration;
        }

        readonly List<AttackSpark> attackSparks = new List<AttackSpark>();

        void PlayAttackFeedback(SimEvent e)
        {
            switch (e.kind)
            {
                case SimEventKind.RangerRelease:
                    var green = new Color(.73f, 1f, .57f, .75f);
                    Burst(e, green, e.heavy ? 7 : 4, .36f, 2.7f, true);
                    break;
                case SimEventKind.HeroImpact:
                    var attacker = world.GetPlayer(e.playerId);
                    if (attacker == null) break;
                    bool knight = attacker.Hero.dragonKnight;
                    var impact = knight
                        ? new Color(1f, .22f, .36f, .9f)
                        : new Color(.93f, .96f, .60f, .85f);
                    Burst(e, impact, knight ? 7 : 4, .40f, knight ? 3.5f : 2.7f, false);
                    break;
                case SimEventKind.RimeBurst:
                    Burst(e, new Color(.65f, .94f, 1f, .86f), 8, .50f, 3.2f, false);
                    break;
                case SimEventKind.RimeStorm:
                    Burst(e, new Color(.85f, .72f, 1f, .8f), 5, .34f, 2.6f, false);
                    break;
            }
        }

        void Burst(SimEvent e, Color color, int count, float duration, float speed, bool forward)
        {
            var origin = new Vector2(e.x, e.y);
            AddAttackSpark(Sprites.Disc, origin, Vector2.zero, color, .30f, .26f, .16f);
            for (int i = 0; i < count; i++)
            {
                float angle = forward
                    ? Mathf.Lerp(-.65f, .65f, i / (float)(count - 1))
                    : .32f + i * 2f * Mathf.PI / count;
                float facing = forward && e.facing < 0 ? -1f : 1f;
                var velocity = new Vector2(Mathf.Cos(angle) * facing, Mathf.Sin(angle))
                    * speed * (.7f + .16f * (i % 3));
                AddAttackSpark(Sprites.Square, origin, velocity, color,
                    .12f + .04f * (i % 3), .035f, duration);
            }
        }

        void AddAttackSpark(Sprite sprite, Vector2 position, Vector2 velocity, Color color,
            float width, float height, float duration)
        {
            if (attackSparks.Count >= 160) return;
            var go = Sprites.Picture("Attack spark", sprite, SlashOrder + 2, null, position.x, position.y);
            go.transform.localScale = new Vector3(width, height, 1f);
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.color = color;
            attackSparks.Add(new AttackSpark
            {
                renderer = renderer, origin = position, velocity = velocity,
                color = color, duration = duration
            });
        }

        void UpdateAttackFeedback(float dt)
        {
            for (int i = attackSparks.Count - 1; i >= 0; i--)
            {
                var spark = attackSparks[i];
                spark.age += dt;
                if (spark.age >= spark.duration)
                {
                    Object.Destroy(spark.renderer.gameObject);
                    attackSparks.RemoveAt(i);
                    continue;
                }
                float t = spark.age / spark.duration;
                Vector2 position = spark.origin + spark.velocity * (spark.age * (1f - .32f * t));
                spark.renderer.transform.position = new Vector3(position.x, position.y, 0f);
                var color = spark.color;
                color.a *= 1f - t * t;
                spark.renderer.color = color;
            }
        }

        void ClearAttackFeedback()
        {
            foreach (var spark in attackSparks)
                Object.Destroy(spark.renderer.gameObject);
            attackSparks.Clear();
        }

        static void AddAttackTrail(GameObject projectile, bool rime, float x, float y)
        {
            // Put the object at its first rendered position before emission begins.
            projectile.transform.position = new Vector3(x, y, 0f);
            var trail = projectile.AddComponent<TrailRenderer>();
            trail.sharedMaterial = projectile.GetComponent<SpriteRenderer>().sharedMaterial;
            trail.sortingOrder = ArrowOrder - 1;
            trail.time = rime ? .28f : .16f;
            trail.minVertexDistance = .045f;
            trail.startWidth = rime ? .18f : .055f;
            trail.endWidth = 0f;
            var color = rime ? new Color(.53f, .87f, 1f, .62f) : new Color(.72f, 1f, .59f, .6f);
            trail.startColor = color;
            color.a = 0f;
            trail.endColor = color;
        }
    }
}
