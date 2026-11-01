using UnityEngine;

namespace AshenVale
{
    // In-game IMGUI: status panel, key hints, inventory and shop. Clicks become World commands
    // (Equip, BuyPotion, BuyGear, Sell), which the world checks before applying.
    public sealed class GameHud
    {
        const float MerchantReach = 2f;      // how close you must be to open the shop
        const float MerchantLeaveReach = 3f; // walking further than this closes it

        readonly World world;
        public bool inventoryOpen, shopOpen;
        public bool WindowOpen { get { return inventoryOpen || shopOpen; } }

        GUIStyle titleText, barText, detailText, bodyStyle, smallStyle, centeredStyle;

        public GameHud(World world) { this.world = world; }

        public void Close() { inventoryOpen = shopOpen = false; }

        // Window keys: I inventory, Esc close, E or B at the merchant for the shop.
        public void HandleKeys(PlayerState me)
        {
            if (me == null || me.dead) { Close(); return; }
            if (Input.GetKeyDown(KeyCode.I)) { inventoryOpen = !inventoryOpen; shopOpen = false; }
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
            if ((Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.B)) && world.NearMerchant(me, MerchantReach)) { shopOpen = !shopOpen; inventoryOpen = false; }
            if (shopOpen && !world.NearMerchant(me, MerchantLeaveReach)) shopOpen = false;
        }

        void InitStyles()
        {
            if (titleText != null) return;
            titleText = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            titleText.normal.textColor = new Color(1f, .85f, .57f);
            barText = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            barText.normal.textColor = Color.white;
            detailText = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            detailText.normal.textColor = new Color(.88f, .9f, .94f);
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 17 };
            bodyStyle.normal.textColor = Color.white;
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            smallStyle.normal.textColor = new Color(.82f, .85f, .9f);
            centeredStyle = new GUIStyle(bodyStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 21 };
        }

        public void Draw(PlayerState me)
        {
            if (me == null) return;
            InitStyles();
            bool town = world.maps[me.map].def.kind == MapKind.Town;
            DrawStatus(me);
            GUI.Box(new Rect(10, Screen.height - 48, Mathf.Min(Screen.width - 20, 920), 38), GUIContent.none);
            GUI.Label(new Rect(22, Screen.height - 42, 1250, 30), town
                ? "A/D Move | W/Up at blue portal: Return | E at merchant: Shop | I Inventory"
                : "A/D Move | S/Down Crouch/Crawl | SPACE Jump | W/S Ladder | J/K Attack | L Dash | Q/F Potions | E Loot | W/Up Portal | T Town | I Bag", smallStyle);
            if (inventoryOpen) DrawInventory(me);
            if (shopOpen && town) DrawShop(me);
            GUI.Label(new Rect(18, 218, 900, 35), world.maps[me.map].def.hint, bodyStyle);
            if (me.portalCast > 0) GUI.Label(new Rect(18, 245, 500, 35), "Casting Town Portal...", bodyStyle);
            if (me.dead)
                GUI.Label(new Rect(0, Screen.height / 2f - 25, Screen.width, 65),
                          me.deathClock < Tuning.RespawnDelay ? "YOU FELL IN BATTLE" : "YOU FELL IN BATTLE — R: WAKE IN EMBERHAVEN   N: NEW HERO", centeredStyle);
        }

        static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        void Bar(float x, float y, float width, string label, int current, int maximum, Color fill)
        {
            float ratio = maximum > 0 ? Mathf.Clamp01((float)current / maximum) : 0f;
            Fill(new Rect(x, y, width, 24), new Color(.055f, .075f, .12f, .95f));
            Fill(new Rect(x + 2, y + 2, (width - 4) * ratio, 20), fill);
            GUI.Label(new Rect(x + 9, y + 2, width - 18, 20), label + "   " + current + " / " + maximum, barText);
        }

        void DrawStatus(PlayerState me)
        {
            var ch = me.ch;
            Matrix4x4 oldMatrix = GUI.matrix;
            float scale = Mathf.Min(1f, Mathf.Max(.65f, Screen.width / 420f));
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            Fill(new Rect(10, 10, 364, 195), new Color(.055f, .075f, .12f, .90f));
            Fill(new Rect(10, 10, 364, 3), new Color(.65f, .48f, .27f));
            GUI.Label(new Rect(23, 17, 300, 25), me.Hero.name.ToUpperInvariant(), titleText);
            GUI.Label(new Rect(310, 19, 55, 22), "LV " + ch.level, detailText);
            Bar(23, 49, 338, "HP", me.hp, me.MaxHp, new Color(.82f, .20f, .25f));
            Bar(23, 80, 338, "MP", me.mp, me.MaxMp, new Color(.19f, .46f, .88f));
            Bar(23, 111, 338, "XP", ch.xp, Tuning.XpNeeded(ch.level), new Color(.88f, .68f, .24f));
            Fill(new Rect(23, 144, 338, 1), new Color(.45f, .45f, .48f, .55f));
            GUI.Label(new Rect(23, 152, 220, 20), "GOLD  " + ch.gold + "     KILLS  " + ch.kills, detailText);
            GUI.Label(new Rect(23, 175, 338, 19), "HP POTIONS [Q]  " + ch.hpPotions + "       MP POTIONS [F]  " + ch.mpPotions, detailText);
            GUI.matrix = oldMatrix;
        }

        static Rect PanelRect() { return new Rect(Mathf.Max(10, (Screen.width - 630) / 2f), Mathf.Max(120, (Screen.height - 480) / 2f), 630, 480); }

        void DrawInventory(PlayerState me)
        {
            var ch = me.ch;
            Rect r = PanelRect();
            GUI.Box(r, "INVENTORY — " + Tuning.InventoryCapacity + " SLOTS   [I] Close");
            GUI.Label(new Rect(r.x + 18, r.y + 25, 580, 22), "Click item to equip. Gear bonuses: ATK +" + ch.BonusAttack + "  DEF +" + ch.BonusDefense + "  HP +" + ch.BonusHealth, smallStyle);
            for (int i = 0; i < 4; i++)
            {
                Item item = ch.equipped[i];
                GUI.Label(new Rect(r.x + 18 + (i % 2) * 300, r.y + 52 + (i / 2) * 27, 290, 25), ((Slot)i) + ": " + (item == null ? "Empty" : item.name), smallStyle);
            }
            for (int i = 0; i < Tuning.InventoryCapacity; i++)
            {
                int col = i % 5, row = i / 5;
                Rect cell = new Rect(r.x + 18 + col * 120, r.y + 119 + row * 75, 115, 68);
                Item item = i < ch.inventory.Count ? ch.inventory[i] : null;
                if (item == null) { GUI.Box(cell, "—"); continue; }
                Color old = GUI.color;
                GUI.color = Loot.RarityColor(item.rarity);
                bool clicked = GUI.Button(cell, item.name + "\n+" + item.attack + " ATK / +" + item.defense + " DEF\n+" + item.health + " HP");
                GUI.color = old;
                if (clicked) { world.Equip(me.id, i); break; }   // the list changed: redraw next frame
            }
            GUI.Label(new Rect(r.x + 18, r.y + 430, 570, 25), "Equipment drops use 75% Common / 20% Rare / 5% Epic.", smallStyle);
        }

        void DrawShop(PlayerState me)
        {
            var ch = me.ch;
            Rect r = PanelRect();
            GUI.Box(r, "EMBERHAVEN GENERAL STORE — [B] Close");
            GUI.Label(new Rect(r.x + 20, r.y + 32, 550, 28), "Gold: " + ch.gold + "   |   Sell unwanted gear for half its value", bodyStyle);
            if (GUI.Button(new Rect(r.x + 20, r.y + 70, 270, 42), "Buy HP Potion — " + Tuning.HpPotionPrice + " gold")) world.BuyPotion(me.id, true);
            if (GUI.Button(new Rect(r.x + 320, r.y + 70, 270, 42), "Buy MP Potion — " + Tuning.MpPotionPrice + " gold")) world.BuyPotion(me.id, false);
            GUI.Label(new Rect(r.x + 20, r.y + 123, 560, 25), "Merchant equipment (reliable Common gear)", smallStyle);
            for (int i = 0; i < 4; i++)
            {
                Slot slot = (Slot)i;
                int cost = Loot.ShopGearCost(slot, ch.level);
                if (GUI.Button(new Rect(r.x + 20 + (i % 2) * 300, r.y + 154 + (i / 2) * 49, 270, 40), "Buy Common " + Loot.SlotName(slot, me.Hero) + " — " + cost + "g"))
                    world.BuyGear(me.id, slot);
            }
            GUI.Label(new Rect(r.x + 20, r.y + 269, 550, 25), "Sell inventory items", smallStyle);
            for (int i = 0; i < ch.inventory.Count && i < 10; i++)
            {
                Item item = ch.inventory[i];
                int col = i % 2, row = i / 2;
                if (GUI.Button(new Rect(r.x + 20 + col * 300, r.y + 297 + row * 31, 270, 29), "Sell " + item.name + " +" + Loot.SellPrice(item) + "g"))
                { world.Sell(me.id, i); break; }
            }
        }
    }
}
