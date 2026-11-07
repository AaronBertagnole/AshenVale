using System.Collections.Generic;
using UnityEngine;

namespace AshenVale
{
    public sealed partial class WorldView
    {
        const float RimeLandingSeconds = .1f;
        const float HeavyDropHeight = 2.8f;

        sealed partial class HeroView
        {
            public int landingMap = -1;
            public bool wasAirborne;
            public float airPeak, landingTime;
            public AudioSource landingAudio;
        }

        sealed class LandingLeaf
        {
            public SpriteRenderer renderer;
            public Vector2 origin, velocity;
            public float age, spin;
        }

        readonly List<LandingLeaf> landingLeaves = new List<LandingLeaf>();
        AudioClip rimeFootfall;

        void TrackRimeLanding(PlayerState player, HeroView view, int localId, float dt, float alpha)
        {
            if (!RimecallerRules.Is(player.Hero)) return;

            view.landingTime = Mathf.Max(0f, view.landingTime - dt);
            if (view.landingMap != player.map || player.dead || player.climbing)
            {
                view.landingMap = player.map;
                view.wasAirborne = false;
                view.landingTime = 0f;
                return;
            }

            if (!player.grounded)
            {
                view.airPeak = view.wasAirborne
                    ? Mathf.Max(view.airPeak, player.y)
                    : Mathf.Max(player.py, player.y);
                view.wasAirborne = true;
                view.landingTime = 0f;
                return;
            }

            // Start at visible contact, after the interpolated feet reach the platform.
            if (!view.wasAirborne || Mathf.Abs(Mathf.Lerp(player.py, player.y, alpha) - player.y) > .075f)
                return;

            view.wasAirborne = false;
            view.landingTime = RimeLandingSeconds;
            if (player.map != Maps.Emberfields || view.airPeak - player.y < HeavyDropHeight)
                return;

            SpawnLandingLeaves(player.x, player.y);
            if (player.id != localId) return;

            if (rimeFootfall == null)
                rimeFootfall = Resources.Load<AudioClip>("Audio/RimecallerLeafFootfall");
            if (rimeFootfall == null) return;
            if (view.landingAudio == null)
            {
                view.landingAudio = view.go.AddComponent<AudioSource>();
                view.landingAudio.playOnAwake = false;
                view.landingAudio.spatialBlend = 0f;
            }
            view.landingAudio.PlayOneShot(rimeFootfall, .32f);
        }

        void SpawnLandingLeaves(float x, float y)
        {
            const int leafCount = 6;
            for (int i = 0; i < leafCount; i++)
            {
                float direction = i % 2 == 0 ? -1f : 1f;
                float spread = i / 2;
                Vector2 origin = new Vector2(x + direction * (.13f + spread * .05f), y + .08f);
                var leaf = Sprites.Picture("Landing leaf", Sprites.Leaf, HeroOrder - 1, null, origin.x, origin.y);
                leaf.transform.localScale = Vector3.one * (.10f + spread * .02f);
                var renderer = leaf.GetComponent<SpriteRenderer>();
                renderer.color = new Color(.91f, .55f - spread * .08f, .23f, .80f);
                landingLeaves.Add(new LandingLeaf
                {
                    renderer = renderer,
                    origin = origin,
                    velocity = new Vector2(direction * (.8f + spread * .35f), .85f + spread * .24f),
                    spin = direction * (160f + spread * 65f)
                });
            }
        }

        void UpdateLandingLeaves(float dt)
        {
            const float life = .30f;
            for (int i = landingLeaves.Count - 1; i >= 0; i--)
            {
                var leaf = landingLeaves[i];
                leaf.age += dt;
                if (leaf.age >= life)
                {
                    Object.Destroy(leaf.renderer.gameObject);
                    landingLeaves.RemoveAt(i);
                    continue;
                }

                Vector2 position = leaf.origin + leaf.velocity * leaf.age;
                position.y = Mathf.Max(leaf.origin.y - .03f, position.y - 3.4f * leaf.age * leaf.age);
                leaf.renderer.transform.position = new Vector3(position.x, position.y, 0f);
                leaf.renderer.transform.rotation = Quaternion.Euler(0f, 0f, leaf.spin * leaf.age);
                var color = leaf.renderer.color;
                color.a = .80f * (1f - leaf.age / life);
                leaf.renderer.color = color;
            }
        }

        void ClearLandingLeaves()
        {
            foreach (var leaf in landingLeaves)
                Object.Destroy(leaf.renderer.gameObject);
            landingLeaves.Clear();
        }
    }
}
