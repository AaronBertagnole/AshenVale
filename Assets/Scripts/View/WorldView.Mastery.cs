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
            else if (e.amount == Abilities.DragonfireHit) DragonfireHit(e);
            else if (e.amount == Abilities.ArrowStorm) ArrowVolleyUp(e);
            else if (e.amount == Abilities.ArrowStormWave) ArrowWave(e);
            else GlacialNova(e);
        }

        // Dragonfire, in three beats (numbers in SkillFxTuning, editable in the Inspector while playing):
        //   the cast   embers gather at the spear while the painted maw rears up (Art/Effects/dragonfire, six frames)
        //   the hit    Abilities.DragonfireHitDelay later the sim lands the damage: a flash, a camera kick, a held beat
        //   after      the frames burn down to embers
        const string DragonfireArt = "Art/Effects/dragonfire";
        void Dragonfire(SimEvent e)
        {
            var tune = SkillFxTuning.Get.dragonfire;
            var frames = Sprites.Frames(DragonfireArt);
            if (frames.Length > 0)
            {
                var go = new GameObject("Dragonfire");
                go.AddComponent<DragonfireFx>().Begin(frames, e.x, e.y, e.facing, SlashOrder + 1);
                rimeFx.RemoveAll(fx => fx == null);
                rimeFx.Add(go);                         // the list of passing effects that is cleared with the map
            }
            // Embers pulled in to the spear over the wind-up.
            var spear = new Vector2(e.x + e.facing * .75f, e.y + .9f);
            float gather = Mathf.Max(.08f, tune.hitDelay);
            for (int i = 0; i < tune.gatherEmbers; i++)
            {
                float angle = Random.Range(-.4f, Mathf.PI + .4f), from = 1.1f + Random.value * 1.1f;
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * from;
                // A spark covers velocity * time * .68 over its life (it slows as it fades).
                AddAttackSpark(Sprites.Disc, spear + offset, -offset / (gather * .68f), new Color(1f, .7f + .25f * Random.value, .3f, .9f), .14f, .09f, gather);
            }
        }

        void DragonfireHit(SimEvent e)
        {
            var tune = SkillFxTuning.Get.dragonfire;
            if (tune.flashSize > 0f)
            {
                var at = new Vector2(e.x + e.facing * tune.flashForward, e.y + 1f);
                AddAttackSpark(Sprites.Disc, at, Vector2.zero, new Color(1f, .93f, .62f, .75f), tune.flashSize * 1.5f, tune.flashSize, tune.flashSeconds);
                AddAttackSpark(Sprites.Disc, at, Vector2.zero, new Color(1f, 1f, .92f, .8f), tune.flashSize * .7f, tune.flashSize * .5f, tune.flashSeconds * .7f);
            }
            // Everyone watching feels the kick, harder when it caught something; only the caster's own game holds its breath.
            Shake(tune.shake * (e.heavy ? 1f : .5f), tune.shakeSeconds);
            if (e.heavy && e.playerId == shownLocalId) HitPauseRequest = Mathf.Max(HitPauseRequest, tune.hitPause);
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
    // Timing, place and size are read from SkillFxTuning every frame, so they can be tuned while it plays.
    public sealed class DragonfireFx : MonoBehaviour
    {
        const float OriginX = 90f / 754f, FlameShare = 1f - OriginX;
        Sprite[] frames; SpriteRenderer sr; float casterX, floor, age; int facing;

        public void Begin(Sprite[] art, float knightX, float floorY, int facingSide, int order)
        {
            frames = art; casterX = knightX; floor = floorY; facing = facingSide >= 0 ? 1 : -1;
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            Draw();
        }

        void Update() { age += Time.deltaTime; Draw(); }

        static float Hold(SkillFxTuning.DragonfireTuning tune, int index)
        {
            var seconds = tune.frameSeconds;
            return seconds == null || seconds.Length == 0 ? .1f : Mathf.Max(.01f, seconds[Mathf.Min(index, seconds.Length - 1)]);
        }

        void Draw()
        {
            var tune = SkillFxTuning.Get.dragonfire;
            float t = age; int index = 0;
            while (index < frames.Length && t >= Hold(tune, index)) { t -= Hold(tune, index); index++; }
            if (index >= frames.Length) { Destroy(gameObject); return; }
            var sprite = frames[index];
            sr.sprite = sprite;
            // At size 1 the flame runs from the glow to the end of the skill's reach.
            float fullWidth = (Abilities.DragonfireReach - tune.forward) / FlameShare;
            float scale = fullWidth / sprite.bounds.size.x * tune.size;
            float origin = sprite.bounds.min.x + sprite.bounds.size.x * OriginX;       // the glow, from the sprite's pivot
            transform.localScale = new Vector3(facing * scale, scale, 1f);
            transform.position = new Vector3(casterX + facing * (tune.forward - origin * scale), floor + tune.lift - sprite.bounds.min.y * scale, 0f);
            // The last frame thins away rather than blinking out.
            sr.color = new Color(1f, 1f, 1f, index == frames.Length - 1 ? 1f - t / Hold(tune, index) : 1f);
        }
    }
}
