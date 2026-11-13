using UnityEngine;

namespace AshenVale
{
    /// <summary>Small, slow mana motes that orbit a ground rune portal.</summary>
    [DisallowMultipleComponent]
    public sealed class PortalManaVfx : MonoBehaviour
    {
        static Material glowMaterial;
        ParticleSystem particles;

        public void Initialize(Color accent, int sortingOrder)
        {
            if (particles != null) return;

            var particleObject = new GameObject("Orbiting Mana");
            particleObject.transform.SetParent(transform, false);
            particleObject.transform.localScale = new Vector3(1f, .58f, 1f);
            particles = particleObject.AddComponent<ParticleSystem>();
            // A new system starts playing at once, and Unity refuses changes to its duration while it plays:
            // stop and clear it, set it up, then Play() at the end.
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 2f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 36;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.03f, .12f);
            main.startSize = new ParticleSystem.MinMaxCurve(.09f, .15f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = Color.white;

            var emission = particles.emission;
            emission.rateOverTime = 7.5f;

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1.05f;
            shape.radiusThickness = .12f;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(.55f);
            velocity.radial = new ParticleSystem.MinMaxCurve(-.035f);

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
