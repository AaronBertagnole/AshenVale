using UnityEngine;

namespace AshenVale
{
    // Turns the keyboard into PlayerInput (through the bindings in Keys). Presses are collected every frame and
    // handed to the next world tick, so a quick tap is never lost between ticks.
    public sealed class KeyboardInput
    {
        PlayerInput pending;

        // allowAttacks is false while the inventory or shop is open.
        public void Sample(bool allowAttacks)
        {
            pending.moveX = (Keys.Held(GameAction.Right) ? 1 : 0) - (Keys.Held(GameAction.Left) ? 1 : 0);
            pending.moveY = (Keys.Held(GameAction.Up) ? 1 : 0) - (Keys.Held(GameAction.Down) ? 1 : 0);
            if (Keys.Pressed(GameAction.Jump)) pending.jump = true;
            if (allowAttacks && Keys.Pressed(GameAction.Attack)) pending.attack = true;
            if (allowAttacks && Keys.Pressed(GameAction.Skill)) pending.skill = true;
            if (Keys.Pressed(GameAction.Dash)) pending.dash = true;
            if (Keys.Pressed(GameAction.Interact)) pending.pickup = true;
            if (Keys.Pressed(GameAction.Up)) pending.up = true;
            if (Keys.Pressed(GameAction.TownPortal)) pending.townPortal = true;
            if (Keys.Pressed(GameAction.HpPotion)) pending.hpPotion = true;
            if (Keys.Pressed(GameAction.MpPotion)) pending.mpPotion = true;
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
