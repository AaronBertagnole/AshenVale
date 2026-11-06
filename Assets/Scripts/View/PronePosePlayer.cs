using UnityEngine;

namespace AshenVale
{
    // Per-rendered-hero animation state; never changes simulation input or collision.
    public sealed class PronePosePlayer
    {
        const float LowerSeconds = .30f, RiseSeconds = .24f;
        float progress, breath;
        bool warned;
        public bool Visible { get; private set; }

        public bool TryDraw(PlayerState p, string normalClip, SpriteRenderer renderer, float dt)
        {
            Visible = false;
            // Existing renderer priorities still decide interruption behavior.
            if (!p.grounded || p.dead || (normalClip != "idle" && normalClip != "run"))
            { progress = breath = 0f; return false; }
            bool lowering = p.crouching;
            if (!lowering && progress <= 0f) return false;
            string folder = p.Hero.folder;
            if (folder != "DragonKnight" && folder != "ElvenRanger")
            { progress = breath = 0f; return false; }
            var enter = Sprites.Hero(folder + "/prone_enter");
            var hold = Sprites.Hero(folder + "/prone");
            if (enter.Length < 4 || hold.Length < 2)
            {
                if (!warned) Debug.LogWarning("Prone needs four prone_enter and two prone frames for " + folder + ".");
                warned = true; progress = breath = 0f; return false;
            }
            progress = Mathf.MoveTowards(progress, lowering ? 1f : 0f,
                Mathf.Max(0f, dt) / (lowering ? LowerSeconds : RiseSeconds));
            if (progress <= 0f) { breath = 0f; return false; }
            if (progress >= 1f && lowering)
            {
                breath += Mathf.Max(0f, dt);
                renderer.sprite = hold[Mathf.FloorToInt(breath * 2f) % hold.Length];
            }
            else
            {
                breath = 0f;
                renderer.sprite = enter[Mathf.Min(enter.Length - 1, Mathf.FloorToInt(progress * enter.Length))];
            }
            renderer.flipX = p.facing < 0;
            Visible = true;
            return true;
        }
    }
}
