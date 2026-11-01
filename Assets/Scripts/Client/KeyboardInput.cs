using UnityEngine;

namespace AshenVale
{
    // Turns the keyboard into PlayerInput. Presses are collected every frame and handed to the
    // next world tick, so a quick tap is never lost between ticks.
    public sealed class KeyboardInput
    {
        PlayerInput pending;

        static bool Held(KeyCode a, KeyCode b) { return Input.GetKey(a) || Input.GetKey(b); }
        static bool Pressed(KeyCode a, KeyCode b) { return Input.GetKeyDown(a) || Input.GetKeyDown(b); }

        // allowAttacks is false while the inventory or shop is open.
        public void Sample(bool allowAttacks)
        {
            pending.moveX = (Held(KeyCode.D, KeyCode.RightArrow) ? 1 : 0) - (Held(KeyCode.A, KeyCode.LeftArrow) ? 1 : 0);
            pending.moveY = (Held(KeyCode.W, KeyCode.UpArrow) ? 1 : 0) - (Held(KeyCode.S, KeyCode.DownArrow) ? 1 : 0);
            if (Input.GetKeyDown(KeyCode.Space)) pending.jump = true;
            if (allowAttacks && Input.GetKeyDown(KeyCode.J)) pending.attack = true;
            if (allowAttacks && Input.GetKeyDown(KeyCode.K)) pending.skill = true;
            if (Input.GetKeyDown(KeyCode.L)) pending.dash = true;
            if (Input.GetKeyDown(KeyCode.E)) pending.pickup = true;
            if (Pressed(KeyCode.W, KeyCode.UpArrow)) pending.up = true;
            if (Input.GetKeyDown(KeyCode.T)) pending.townPortal = true;
            if (Input.GetKeyDown(KeyCode.Q)) pending.hpPotion = true;
            if (Input.GetKeyDown(KeyCode.F)) pending.mpPotion = true;
            if (Input.GetKeyDown(KeyCode.R)) pending.respawn = true;
        }

        // The input for one tick; presses are delivered once, held keys carry on.
        public PlayerInput Take()
        {
            var input = pending;
            pending = pending.HeldOnly();
            return input;
        }

        public void Clear() { pending = new PlayerInput(); }
    }
}
