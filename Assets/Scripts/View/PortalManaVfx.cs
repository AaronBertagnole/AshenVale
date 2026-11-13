using UnityEngine;

namespace AshenVale
{
    /// <summary>
    /// Life for the standing swirl portal: mana motes rising from its ground seal, glints turning with the swirl,
    /// sparks drifting off its rim, and a slow breathing pulse of the picture itself.
    /// The component sits at the centre of the seal; (swirlHeight, swirlRadius) say where the swirl's oval is.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PortalManaVfx : MonoBehaviour
    {
        static Material glowMaterial;
        static readonly Color Teal = new Color(.30f, .95f, .88f, 1f), Gold = new Color(1f, .78f, .30f, 1f);
        bool built;
        Transform picture; Vector3 pictureScale; SpriteRenderer pictureRenderer; float phase;

        public void Initialize(Color accent, int sortingOrder, Transform art = null, float sealRadius = .67f, float swirlHeight = 1.05f, float swirlRadius = .62f)
        {
            if (built) return;
            built = true;
            picture = art;
            if (art != null) { pictureScale = art.localScale; pictureRenderer = art.GetComponent<SpriteRenderer>(); }
            phase = Random.value * 10f;
            Color mana = Color.Lerp(Teal, accent, .25f);

            // 1. Motes rising from the seal on the ground.
            var seal = NewSystem("Seal Motes", Vector3.zero, sortingOrder);
            var main = seal.main;
            main.duration = 2f; main.maxParticles = 60;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.3f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0f);
            main.startSize = new ParticleSystem.MinMaxCurve(.07f, .15f);
            var emission = seal.emission; emission.rateOverTime = 12f;
            var shape = seal.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = sealRadius; shape.radiusThickness = 1f; shape.scale = new Vector3(1f, .17f, 1f);     // the seal lies flat
            var velocity = seal.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-.06f, .06f);
            velocity.y = new ParticleSystem.MinMaxCurve(.45f, 1.0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            Fade(seal, mana, Gold, .8f);
            Shrink(seal);
            seal.Play();

            // 2. Glints inside the swirl, turning with it.
            var glints = NewSystem("Swirl Glints", new Vector3(0f, swirlHeight, 0f), sortingOrder + 1);
            main = glints.main;
            main.duration = 2f; main.maxParticles = 50;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.7f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0f);
            main.startSize = new ParticleSystem.MinMaxCurve(.06f, .16f);
            emission = glints.emission; emission.rateOverTime = 14f;
            shape = glints.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = swirlRadius; shape.radiusThickness = 1f; shape.scale = new Vector3(1f, 1.35f, 1f);  // the swirl is a tall oval
            velocity = glints.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(-1.1f);        // the way the painted swirl turns
            velocity.radial = new ParticleSystem.MinMaxCurve(-.18f);         // drawn slowly into the bright centre
            Fade(glints, Color.Lerp(mana, Color.white, .35f), mana, .95f);
            Shrink(glints);
            glints.Play();

            // 3. Sparks thrown off the rim, drifting outward and up.
            var sparks = NewSystem("Rim Sparks", new Vector3(0f, swirlHeight, 0f), sortingOrder + 1);
            main = sparks.main;
            main.duration = 1f; main.maxParticles = 40;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.8f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.15f, .5f);     // outward from the rim
            main.startSize = new ParticleSystem.MinMaxCurve(.05f, .11f);
            emission = sparks.emission; emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst { time = .05f, minCount = 2, maxCount = 5, cycleCount = 0, repeatInterval = .3f } });
            shape = sparks.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = swirlRadius * 1.08f; shape.radiusThickness = .05f; shape.scale = new Vector3(1f, 1.35f, 1f);
            velocity = sparks.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(.1f, .45f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            Fade(sparks, Gold, Color.Lerp(Gold, accent, .4f), .9f);
            Shrink(sparks);
            sparks.Play();
        }

        // The picture breathes: a slight swell and a soft brightening, slow enough to read as a pulse of power.
        void Update()
        {
            if (picture == null) return;
            float wave = Mathf.Sin((Time.time + phase) * 2.1f), flicker = Mathf.Sin((Time.time + phase) * 5.3f);
            picture.localScale = new Vector3(pictureScale.x * (1f + wave * .018f), pictureScale.y * (1f + wave * .012f), 1f);
            if (pictureRenderer != null) { float light = .93f + wave * .05f + flicker * .02f; pictureRenderer.color = new Color(light, light, light, 1f); }
        }

        ParticleSystem NewSystem(string name, Vector3 at, int sortingOrder)
        {
            var host = new GameObject(name);
            host.transform.SetParent(transform, false);
            host.transform.localPosition = at;
            var system = host.AddComponent<ParticleSystem>();
            // A new system starts playing at once, and Unity refuses changes to its duration while it plays.
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = true; main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = Color.white;
            var renderer = host.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = sortingOrder;
            renderer.sharedMaterial = GlowMaterial();
            return system;
        }

        static void Fade(ParticleSystem system, Color from, Color to, float peak)
        {
            var colorLife = system.colorOverLifetime; colorLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(from, 0f), new GradientColorKey(Color.Lerp(from, to, .5f), .55f), new GradientColorKey(to, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(peak, .15f), new GradientAlphaKey(peak * .75f, .7f), new GradientAlphaKey(0f, 1f) });
            colorLife.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        static void Shrink(ParticleSystem system)
        {
            var sizeLife = system.sizeOverLifetime; sizeLife.enabled = true;
            sizeLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, .3f), new Keyframe(.2f, 1f), new Keyframe(.75f, .75f), new Keyframe(1f, .1f)));
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
