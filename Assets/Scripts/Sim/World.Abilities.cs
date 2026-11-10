namespace AshenVale
{
    // Spending and taking back ability points (see Abilities). Asked for through IGameCommands, so on a
    // client these run on the host.
    public sealed partial class World
    {
        // One point into an ability: unlocks it at rank 0, ranks it up after that.
        public void RankUp(int playerId, int slot)
        {
            var p = GetPlayer(playerId);
            if (p == null || p.dead || !Abilities.CanRankUp(p.ch, slot)) return;
            p.ch.abilityRanks[slot]++;
        }

        // Takes every point back. Towns only.
        public void Respec(int playerId)
        {
            var p = GetPlayer(playerId);
            if (p == null || p.dead || !Maps.IsTown(p.map)) return;
            for (int i = 0; i < Abilities.Count; i++) { p.ch.abilityRanks[i] = Abilities.StartRank(i); p.ch.abilityPicks[i] = 0; }
        }
    }
}
