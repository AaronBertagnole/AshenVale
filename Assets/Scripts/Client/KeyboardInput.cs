using UnityEngine;

namespace AshenVale
{
    // Turns the keyboard into PlayerInput (through the bindings in Keys). Presses are collected every frame and
    // handed to the next world tick, so a quick tap is never lost between ticks.
    public sealed class KeyboardInput
    {
        PlayerInput pending;

        // allowAttacks is false while the inventory or shop is open.
        public void Sample(bool allowAttacks, bool ignoreMouse = false)
        {
            pending.moveX = (Keys.Held(GameAction.Right, ignoreMouse) ? 1 : 0) - (Keys.Held(GameAction.Left, ignoreMouse) ? 1 : 0);
            pending.moveY = (Keys.Held(GameAction.Up, ignoreMouse) ? 1 : 0) - (Keys.Held(GameAction.Down, ignoreMouse) ? 1 : 0);
            if (Keys.Pressed(GameAction.Jump, ignoreMouse)) pending.jump = true;
            if (allowAttacks && Keys.Pressed(GameAction.Attack, ignoreMouse)) pending.attack = true;
            if (allowAttacks && Keys.Pressed(GameAction.Skill, ignoreMouse)) pending.skill = true;
            if (Keys.Pressed(GameAction.Dash, ignoreMouse)) pending.dash = true;
            if (Keys.Pressed(GameAction.Interact, ignoreMouse)) pending.pickup = true;
            if (Keys.Pressed(GameAction.Up, ignoreMouse)) pending.up = true;
            if (Keys.Pressed(GameAction.TownPortal, ignoreMouse)) pending.townPortal = true;
            if (Keys.Pressed(GameAction.HpPotion, ignoreMouse)) pending.hpPotion = true;
            if (Keys.Pressed(GameAction.MpPotion, ignoreMouse)) pending.mpPotion = true;
            if (Input.GetKeyDown(KeyCode.R)) pending.respawn = true;
        }

        // A Canvas click is latched just like a keyboard press, then consumed by the normal solo/online tick.
        // Call only after the UI has checked window/death state; the world still validates cooldowns and resources.
        public void QueueAction(GameAction action)
        {
            switch (action)
            {
                case GameAction.Attack: pending.attack = true; break;
                case GameAction.Skill: pending.skill = true; break;
                case GameAction.Dash: pending.dash = true; break;
                case GameAction.TownPortal: pending.townPortal = true; break;
                case GameAction.HpPotion: pending.hpPotion = true; break;
                case GameAction.MpPotion: pending.mpPotion = true; break;
            }
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

