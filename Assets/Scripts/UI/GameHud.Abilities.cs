using UnityEngine;

namespace AshenVale
{
    // The ability window (GameAction.Abilities, P by default): the hero's three abilities, their ranks and
    // their current numbers. Phase 1 only shows them; spending points arrives with phase 2.
    public sealed partial class GameHud
    {
        void DrawAbilities(PlayerState me)
        {
            var ch = me.ch;
            Rect r = PanelRect();
            GUI.Box(r, "ABILITIES   [" + Keys.Name(GameAction.Abilities) + "] Close");
            GUI.Label(new Rect(r.x + 18, r.y + 27, r.width - 36, 20),
                      ch.Def.name + "   |   Level " + ch.level + "   |   Ability points: " + ch.UnspentPoints, smallStyle);

            const float cardH = 112f;
            for (int slot = 0; slot < Abilities.Count; slot++)
            {
                Rect card = new Rect(r.x + 18, r.y + 58 + slot * (cardH + 10), r.width - 36, cardH);
                GUI.Box(card, "");
                Fill(new Rect(card.x, card.y, 4, card.height), HudTheme.Copper);
                GUI.Label(new Rect(card.x + 16, card.y + 8, 330, 24), Abilities.Name(ch, slot), titleText);
                GUI.Label(new Rect(card.x + 16, card.y + 32, 330, 18),
                          Abilities.SlotLabel(slot) + "   [" + Keys.Name(Abilities.Key(slot)) + "]", detailText);
                GUI.Label(new Rect(card.x + 16, card.y + 54, card.width - 32, 22), Abilities.Blurb(ch, slot), smallStyle);
                GUI.Label(new Rect(card.x + 16, card.y + 80, card.width - 32, 22), Abilities.Summary(me, slot), detailText);

                // Rank pips, right side of the card.
                int rank = ch.abilityRanks[slot];
                float pipX = card.xMax - 16 - Abilities.MaxRank * 22;
                GUI.Label(new Rect(pipX - 70, card.y + 10, 64, 20), "RANK " + rank, detailRight);
                for (int i = 0; i < Abilities.MaxRank; i++)
                {
                    Rect pip = new Rect(pipX + i * 22, card.y + 12, 16, 16);
                    Fill(pip, new Color(0f, 0f, 0f, .55f));
                    if (i < rank) Fill(new Rect(pip.x + 2, pip.y + 2, 12, 12), HudTheme.Copper);
                }
            }
            GUI.Label(new Rect(r.x + 18, r.y + 433, r.width - 36, 28),
                      "Every level earns one ability point. Spending points to rank up abilities arrives in the next update.", partNameText);
        }
    }
}
