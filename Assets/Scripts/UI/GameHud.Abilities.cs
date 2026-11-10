using UnityEngine;

namespace AshenVale
{
    // The ability window (GameAction.Abilities, P by default): the hero's three abilities, their ranks and
    // numbers, a button to put a point in, and a respec button that works in towns.
    public sealed partial class GameHud
    {
        void DrawAbilities(PlayerState me)
        {
            var ch = me.ch;
            Rect r = PanelRect();
            GUI.Box(r, "ABILITIES   [" + Keys.Name(GameAction.Abilities) + "] Close");
            GUI.Label(new Rect(r.x + 18, r.y + 27, r.width - 36, 20),
                      ch.Def.name + "   |   Level " + ch.level + "   |   Ability points to spend: " + ch.UnspentPoints, smallStyle);

            const float cardH = 118f;
            for (int slot = 0; slot < Abilities.Count; slot++)
            {
                Rect card = new Rect(r.x + 18, r.y + 54 + slot * (cardH + 8), r.width - 36, cardH);
                int rank = ch.abilityRanks[slot];
                bool locked = rank < 1;
                GUI.Box(card, "");
                Fill(new Rect(card.x, card.y, 4, card.height), locked ? new Color(.35f, .35f, .35f) : HudTheme.Copper);
                GUI.Label(new Rect(card.x + 16, card.y + 6, 300, 24), Abilities.Name(ch, slot), titleText);
                GUI.Label(new Rect(card.x + 16, card.y + 29, 300, 18),
                          Abilities.SlotLabel(slot) + "   [" + Keys.Name(Abilities.Key(slot)) + "]", detailText);
                GUI.Label(new Rect(card.x + 16, card.y + 49, card.width - 150, 22), Abilities.Blurb(ch, slot), smallStyle);
                GUI.Label(new Rect(card.x + 16, card.y + 72, card.width - 32, 20),
                          locked ? "Locked" : Abilities.Summary(me, slot), detailText);
                GUI.Label(new Rect(card.x + 16, card.y + 93, card.width - 32, 20), Abilities.RankText(ch, slot), detailText);

                // Rank pips and the button, right side of the card.
                float pipX = card.xMax - 14 - Abilities.MaxRank * 13;
                GUI.Label(new Rect(pipX - 96, card.y + 8, 90, 20), locked ? "LOCKED" : "RANK " + rank, detailRight);
                for (int i = 0; i < Abilities.MaxRank; i++)
                {
                    Rect pip = new Rect(pipX + i * 13, card.y + 11, 10, 14);
                    Fill(pip, new Color(0f, 0f, 0f, .55f));
                    if (i < rank) Fill(new Rect(pip.x + 1, pip.y + 1, 8, 12), HudTheme.Copper);
                }
                Rect button = new Rect(card.xMax - 134, card.y + 36, 120, 30);
                if (rank >= Abilities.MaxRank) GUI.Label(button, "MAX RANK", detailRight);
                else if (ch.level < Abilities.UnlockLevel[slot]) GUI.Label(button, "Unlocks at level " + Abilities.UnlockLevel[slot], detailRight);
                else
                {
                    bool was = GUI.enabled;
                    GUI.enabled = was && Abilities.CanRankUp(ch, slot);
                    if (GUI.Button(button, locked ? "UNLOCK  (1)" : "RANK UP  (1)")) commands.RankUp(slot);
                    GUI.enabled = was;
                }
            }

            bool town = Maps.IsTown(me.map);
            GUI.Label(new Rect(r.x + 18, r.y + 438, r.width - 170, 28),
                      town ? "Every level earns one point. Respec takes every point back, free."
                           : "Every level earns one point. You can respec in any town.", partNameText);
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && town && ch.PointsSpent > 0;
            if (GUI.Button(new Rect(r.xMax - 138, r.y + 438, 120, 28), "RESPEC")) commands.Respec();
            GUI.enabled = enabled;
        }

        // A small reminder while points are waiting.
        void DrawPointsHint(PlayerState me)
        {
            string text = "Ability point ready   [" + Keys.Name(GameAction.Abilities) + "]";
            Rect at = new Rect(Screen.width / 2f - 150, Screen.height - 168, 300, 24);
            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, .8f);
            GUI.Label(new Rect(at.x + 1, at.y + 1, at.width, at.height), text, nameplate);
            GUI.color = new Color(1f, .85f, .45f);
            GUI.Label(at, text, nameplate);
            GUI.color = old;
        }
    }
}
