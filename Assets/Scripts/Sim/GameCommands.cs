using System;

namespace AshenVale
{
    // What the inventory, shop, waystone and smithy windows can ask for. Solo and on the host these go straight to the World;
    // on a client they are sent to the host, which checks and applies them (see NetSession).
    public interface IGameCommands
    {
        void Equip(int inventoryIndex);
        void Sell(int inventoryIndex);
        void BuyPotion(bool health);
        void BuyGear(Slot slot);
        void Travel(int town);
        void Upgrade(int target);   // smithy: 0-3 worn slot, Smithing.InventoryTarget + i for inventory item i
        void SellPart(int part, int count);   // merchant: count parts of one kind (0 = all)
        void RankUp(int ability);   // ability window: put one point into Abilities.Basic / Skill / Movement
        void Respec();              // ability window, in a town: take every point back
    }

    public sealed class LocalCommands : IGameCommands
    {
        readonly World world;
        readonly Func<int> player;

        public LocalCommands(World world, Func<int> player) { this.world = world; this.player = player; }

        public void Equip(int inventoryIndex) { world.Equip(player(), inventoryIndex); }
        public void Sell(int inventoryIndex) { world.Sell(player(), inventoryIndex); }
        public void BuyPotion(bool health) { world.BuyPotion(player(), health); }
        public void SellPart(int part, int count) { world.SellPart(player(), part, count); }
        public void BuyGear(Slot slot) { world.BuyGear(player(), slot); }
        public void Travel(int town) { world.Travel(player(), town); }
        public void Upgrade(int target) { world.Upgrade(player(), target); }
        public void RankUp(int ability) { world.RankUp(player(), ability); }
        public void Respec() { world.Respec(player()); }
    }
}
