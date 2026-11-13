using UnityEngine;

namespace AshenVale
{
    /// <summary>Mana motes and quick glints that rise straight up from a ground rune portal's ring.</summary>
    [DisallowMultipleComponent]
    public sealed class PortalManaVfx : MonoBehaviour
    {
        static Material glowMaterial;
        ParticleSystem particles;
        ParticleSystem sparkles;

        public void Initialize(Color accent, int sortingOrder)
        {
            if (particles != null) return;

            var particleObject = new GameObject("Orbiting Mana");
            particleObject.transform.SetParent(transform, false);
            particleObject.transform.localScale = new Vector3(1f, .25f, 1f);     // the motes follow the flattened circle
            particles = particleObject.AddComponent<ParticleSystem>();
            // A new system starts playing at once, and Unity refuses changes to its duration while it plays:
            // stop and clear it, set it up, then Play() at the end.
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 2f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 54;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0f);     // no sideways push from the ring
            main.startSize = new ParticleSystem.MinMaxCurve(.13f, .22f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = Color.white;

            var emission = particles.emission;
            emission.rateOverTime = 11f;

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1.05f;
            shape.radiusThickness = .12f;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0f);      // no circling: they start on the ring and go straight up
            // Rise out of the circle as they turn. (Local space, and this object is squashed to .25 in y, so the
            // numbers are four times the speed seen on screen: about half to one unit a second.)
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(2.0f, 3.8f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.radial = new ParticleSystem.MinMaxCurve(0f);

            var rotation = particles.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-.45f, .45f);

            var colorLife = particles.colorOverLifetime;
            colorLife.enabled = true;
            var mana = Color.Lerp(accent, new Color(.50f, .96f, .88f, 1f), .42f);
            var warm = new Color(1f, .78f, .40f, 1f);
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(mana, 0f),
                    new GradientColorKey(Color.Lerp(mana, warm, .38f), .58f),
                    new GradientColorKey(warm, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(.92f, .16f),
                    new GradientAlphaKey(.70f, .72f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorLife.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeLife = particles.sizeOverLifetime;
            sizeLife.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f, .35f), new Keyframe(.22f, 1f), new Keyframe(.78f, .78f), new Keyframe(1f, .12f));
            sizeLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = sortingOrder;
            renderer.sharedMaterial = GlowMaterial();
            particles.Play();

            CreateSparkles(accent, sortingOrder + 1);
        }

        void CreateSparkles(Color accent, int sortingOrder)
        {
            var sparkleObject = new GameObject("Rune Shimmer Sparks");
            sparkleObject.transform.SetParent(transform, false);
            sparkleObject.transform.localScale = new Vector3(1f, .25f, 1f);     // flat, like the circle
            sparkles = sparkleObject.AddComponent<ParticleSystem>();
            sparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // see Initialize

            var main = sparkles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 70;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.40f, .85f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0f);
            main.startSize = new ParticleSystem.MinMaxCurve(.17f, .30f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = Color.white;

            var emission = sparkles.emission;
            emission.rateOverTime = 0f;
            var burst = new ParticleSystem.Burst
            {
                time = .04f,
                minCount = 4,
                maxCount = 7,
                cycleCount = 0,
                repeatInterval = .24f
            };
            emission.SetBursts(new[] { burst });

            var shape = sparkles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1.08f;
            shape.radiusThickness = .08f;

            var velocity = sparkles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0f);
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(1.4f, 2.8f);     // glints drift upward too (see the motes above)
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var colorLife = sparkles.colorOverLifetime;
            colorLife.enabled = true;
            var mana = Color.Lerp(accent, new Color(.48f, 1f, .91f, 1f), .5f);
            var bright = Color.Lerp(mana, new Color(1f, .91f, .61f, 1f), .55f);
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(bright, 0f),
                    new GradientColorKey(mana, .48f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, .12f),
                    new GradientAlphaKey(.82f, .42f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorLife.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeLife = sparkles.sizeOverLifetime;
            sizeLife.enabled = true;
            sizeLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, .1f), new Keyframe(.18f, 1.2f), new Keyframe(.52f, .68f), new Keyframe(1f, .08f)));

            var renderer = sparkleObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = sortingOrder;
            renderer.sharedMaterial = GlowMaterial();
            sparkles.Play();
        }

        static Material GlowMaterial()
        {
            if (glowMaterial != null) return glowMaterial;
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) return null;
            glowMaterial = new Material(shader) { name = "Ashen Vale Portal Mana", hideFlags = HideFlags.HideAndDontSave };
            glowMaterial.mainTexture = Sprites.Glow.texture;
            return glowMaterial;
        }
    }
}
