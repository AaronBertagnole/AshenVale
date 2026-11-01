using System;

namespace AshenVale
{
    // What the inventory and shop windows can ask for. Solo and on the host these go straight to the World;
    // on a client they are sent to the host, which checks and applies them (see NetSession).
    public interface IGameCommands
    {
        void Equip(int inventoryIndex);
        void Sell(int inventoryIndex);
        void BuyPotion(bool health);
        void BuyGear(Slot slot);
    }

    public sealed class LocalCommands : IGameCommands
    {
        readonly World world;
        readonly Func<int> player;

        public LocalCommands(World world, Func<int> player) { this.world = world; this.player = player; }

        public void Equip(int inventoryIndex) { world.Equip(player(), inventoryIndex); }
        public void Sell(int inventoryIndex) { world.Sell(player(), inventoryIndex); }
        public void BuyPotion(bool health) { world.BuyPotion(player(), health); }
        public void BuyGear(Slot slot) { world.BuyGear(player(), slot); }
    }
}
