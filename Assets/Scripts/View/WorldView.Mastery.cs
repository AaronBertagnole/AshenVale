using UnityEngine;

namespace AshenVale
{
    // The level-30 mastery skills on screen (SimEventKind.Mastery; the sim is World.Mastery). Drawn from sparks for
    // now, so each one reads without painted art: flame rolling forward, arrows falling, ice bursting outward.
    public sealed partial class WorldView
    {
        void SpawnMastery(SimEvent e)
        {
            if (e.amount == Abilities.Dragonfire) Dragonfire(e);
            else if (e.amount == Abilities.ArrowStorm) ArrowVolleyUp(e);
            else if (e.amount == Abilities.ArrowStormWave) ArrowWave(e);
            else GlacialNova(e);
        }

        // The painted dragon maw (Art/Effects/dragonfire, six frames): it forms around the Knight, breathes the flame
        // out to the skill's reach and burns down to embers. A few loose embers fly with it. Without the art, embers only.
        const string DragonfireArt = "Art/Effects/dragonfire";
        void Dragonfire(SimEvent e)
        {
            var frames = Sprites.Frames(DragonfireArt);
            if (frames.Length > 0)
            {
                var go = new GameObject("Dragonfire");
                go.AddComponent<DragonfireFx>().Begin(frames, e.x + e.facing * DragonfireFx.ForwardOffset, e.y, e.facing, SlashOrder + 1);
                rimeFx.RemoveAll(fx => fx == null);
                rimeFx.Add(go);                         // the list of passing effects that is cleared with the map
            }
            var mouth = new Vector2(e.x + e.facing * 1.6f, e.y + 1f);
            int embers = frames.Length > 0 ? 0 : 44;
            for (int i = 0; i < embers; i++)
            {
                float far = Random.value, time = .32f + .3f * Random.value;
                // A spark covers velocity * time * .68 over its life (it slows as it fades).
                float speed = Abilities.DragonfireReach * (.25f + .75f * far) / (time * .68f);
                var velocity = new Vector2(e.facing * speed, Random.Range(-1.6f, 1.3f) - .5f);
                var color = Color.Lerp(new Color(1f, .93f, .5f, .95f), new Color(1f, .32f, .08f, .9f), Random.value);
                float size = frames.Length > 0 ? .08f + .1f * Random.value : .28f + .5f * Random.value;
                AddAttackSpark(Sprites.Disc, mouth + new Vector2(0f, Random.Range(-.45f, .35f)), velocity, color, size * 1.5f, size, time);
            }
        }

        // The Ranger looses the volley skyward; the waves land a moment later.
        void ArrowVolleyUp(SimEvent e)
        {
            for (int i = 0; i < 9; i++)
                AddAttackSpark(Sprites.Square, new Vector2(e.x + e.facing * .4f, e.y + 1.3f), new Vector2(e.facing * (2f + i * .45f), 17f + Random.value * 4f),
                               new Color(.88f, 1f, .8f, .9f), .7f, .045f, .3f);
        }

        // One wave: arrows streak down across the target ground, and dust kicks up where they land.
        void ArrowWave(SimEvent e)
        {
            float half = Abilities.ArrowStormHalfWidth;
            for (int i = 0; i < 7; i++)
            {
                float lands = e.x + Random.Range(-half, half);
                const float time = .16f, drop = 4.2f, lean = 1.1f;
                var velocity = new Vector2(lean, -drop) / (time * .68f);
                AddAttackSpark(Sprites.Square, new Vector2(lands - lean, e.y + drop + .1f), velocity, new Color(.9f, 1f, .82f, .95f), .8f, .05f, time);
                AddAttackSpark(Sprites.Disc, new Vector2(lands, e.y + .08f), new Vector2(Random.Range(-1.5f, 1.5f), 1.6f + Random.value), new Color(.8f, .9f, .7f, .6f), .16f, .16f, .3f);
            }
        }

        // A flash at the caster and a ring of shards thrown out to the nova's edge.
        void GlacialNova(SimEvent e)
        {
            var centre = new Vector2(e.x, e.y + .9f);
            AddAttackSpark(Sprites.Disc, centre, Vector2.zero, new Color(.75f, .95f, 1f, .7f), 2.6f, 2.6f, .22f);
            const int shards = 40;
            for (int i = 0; i < shards; i++)
            {
                float angle = i * Mathf.PI * 2f / shards + Random.Range(-.05f, .05f), time = .3f + .12f * Random.value;
                var outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * .55f);       // flattened: it spreads along the ground more than up
                float speed = Abilities.NovaRadius * (.75f + .25f * Random.value) / (time * .68f);
                AddAttackSpark(Sprites.Square, centre, outward * speed, new Color(.6f + .25f * (i % 2), .92f, 1f, .95f), .5f + .2f * (i % 3), .07f, time);
            }
        }
    }

    // Plays the Dragonfire frames once at a fixed spot and removes itself. The frames share one canvas: the ground
    // glow the fire grows from is OriginX of the way across, and the flame runs from there to the right edge.
    public sealed class DragonfireFx : MonoBehaviour
    {
        public const float ForwardOffset = .95f;
        const float OriginX = 90f / 754f, FlameShare = 1f - OriginX;
        static readonly float[] hold = { .10f, .14f, .11f, .43f, .19f, .23f };   // seconds on each frame: a quick rear-up, the long breath, the burn-down
        Sprite[] frames; SpriteRenderer sr; float x, floor, age; int facing;

        public void Begin(Sprite[] art, float originX, float floorY, int facingSide, int order)
        {
            frames = art; x = originX; floor = floorY; facing = facingSide >= 0 ? 1 : -1;
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            Draw();
        }

        void Update() { age += Time.deltaTime; Draw(); }

        void Draw()
        {
            float t = age; int index = 0;
            while (index < frames.Length && t >= hold[Mathf.Min(index, hold.Length - 1)]) { t -= hold[Mathf.Min(index, hold.Length - 1)]; index++; }
            if (index >= frames.Length) { Destroy(gameObject); return; }
            var sprite = frames[index];
            sr.sprite = sprite;
            float fullWidth = (Abilities.DragonfireReach - ForwardOffset) / FlameShare;
            float progress = Mathf.Clamp01(t / .15f);
            float width = index == 3 ? Mathf.Lerp(.76f, 1f, Mathf.SmoothStep(0f, 1f, progress)) :
                index < 3 ? .76f : 1f;
            float scaleX = fullWidth * width / sprite.bounds.size.x;
            float scaleY = fullWidth * .66f / sprite.bounds.size.x;
            float origin = sprite.bounds.min.x + sprite.bounds.size.x * OriginX;
            transform.localScale = new Vector3(facing * scaleX, scaleY, 1f);
            transform.position = new Vector3(x - facing * origin * scaleX,
                floor - .04f - sprite.bounds.min.y * scaleY, 0f);

            float opacity = index == frames.Length - 1
                ? 1f - t / hold[Mathf.Min(index, hold.Length - 1)] : 1f;
            float heat = index == 3
                ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - .18f) / .18f)) : index > 3 ? 1f : 0f;
            sr.color = Color.Lerp(Color.white, new Color(1f, .73f, .43f), heat);
            var color = sr.color;
            color.a = opacity;
            sr.color = color;
        }
    }
}
