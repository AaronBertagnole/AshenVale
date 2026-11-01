namespace AshenVale
{
    // Everything a player asks the world to do in one tick. The world only ever reads this,
    // never the keyboard, so later a client can send it to the host over the network.
    public struct PlayerInput
    {
        public float moveX;     // -1 .. 1, held
        public float moveY;     // -1 .. 1, held (ladders, crouch)
        // Pressed since the last tick
        public bool jump, attack, skill, dash, pickup, up, townPortal, hpPotion, mpPotion, respawn;

        // A copy that keeps the held axes but clears the one-shot presses.
        public PlayerInput HeldOnly()
        {
            return new PlayerInput { moveX = moveX, moveY = moveY };
        }
    }
}
