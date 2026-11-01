using System;
using UnityEngine;

namespace AshenVale
{
    // In-game IMGUI: status panel, key hints, map banner, labels over portals and town folk, monster health bars,
    // inventory, shop, waystone, smithy and the Esc menu. Clicks become World commands (Equip, BuyPotion, BuyGear, Sell,
    // Travel, Upgrade), which the world checks before applying.
    public sealed class GameHud
    {
        const float NpcReach = 2f;          // how close you must be to the merchant or waystone to open its window
        const float NpcLeaveReach = 3f;     // walking further than this closes it
        const float BannerTime = 3.2f;
        const float SmithyRowHeight = 58f;

        readonly World world;
        public bool inventoryOpen, shopOpen, menuOpen, waystoneOpen, smithyOpen;
        public bool WindowOpen { get { return inventoryOpen || shopOpen || menuOpen || waystoneOpen || smithyOpen; } }

        // Esc menu choices, handled by the game (they save first).
        public Action onCharacterSelect, onQuit;
        public IGameCommands commands;      // local World, or the host when playing as a client
        public string status = "";          // top-right line: join code, players, connection state
        public bool online;                 // changes the Esc menu wording

        GUIStyle titleText, barText, detailText, detailRight, bodyStyle, smallStyle, centeredStyle, menuButton, statusStyle, nameplate,
                 bannerTitle, bannerSub, worldLabel, mapLabel;
        int bannerMap = -1;
        float bannerClock;
        Vector2 smithyScroll;

        public GameHud(World world) { this.world = world; }

        public void Close() { inventoryOpen = shopOpen = menuOpen = waystoneOpen = smithyOpen = false; bannerMap = -1; }

        // Window keys: I inventory, E (or B) at the merchant for the shop, E at a waystone for travel, E at a smithy
        // for upgrades, Esc closes a window or opens the menu. The world keeps running while windows are open;
        // only attacks are blocked.
        public void HandleKeys(PlayerState me)
        {
            if (me == null || me.dead) { inventoryOpen = shopOpen = menuOpen = waystoneOpen = smithyOpen = false; return; }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (inventoryOpen || shopOpen || waystoneOpen || smithyOpen) { inventoryOpen = shopOpen = waystoneOpen = smithyOpen = false; }
                else menuOpen = !menuOpen;
            }
            if (menuOpen) return;
            if (Input.GetKeyDown(KeyCode.I)) { inventoryOpen = !inventoryOpen; shopOpen = waystoneOpen = smithyOpen = false; }
            bool use = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.B);
            bool talk = Input.GetKeyDown(KeyCode.E);
            if (use && world.NearMerchant(me, NpcReach)) { shopOpen = !shopOpen; inventoryOpen = waystoneOpen = smithyOpen = false; }
            else if (talk && world.NearWaystone(me, NpcReach)) { waystoneOpen = !waystoneOpen; inventoryOpen = shopOpen = smithyOpen = false; }
            else if (talk && world.NearSmithy(me, NpcReach)) { smithyOpen = !smithyOpen; inventoryOpen = shopOpen = waystoneOpen = false; }
            if (shopOpen && !world.NearMerchant(me, NpcLeaveReach)) shopOpen = false;
            if (waystoneOpen && !world.NearWaystone(me, NpcLeaveReach)) waystoneOpen = false;
            if (smithyOpen && !world.NearSmithy(me, NpcLeaveReach)) smithyOpen = false;
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
            detailRight = new GUIStyle(detailText) { alignment = TextAnchor.UpperRight };
            menuButton = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 17 };
            bodyStyle.normal.textColor = Color.white;
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            smallStyle.normal.textColor = new Color(.82f, .85f, .9f);
            centeredStyle = new GUIStyle(bodyStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 21 };
            statusStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperRight };
            statusStyle.normal.textColor = new Color(.62f, .9f, 1f);
            nameplate = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            nameplate.normal.textColor = new Color(1f, .93f, .75f);
            bannerTitle = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            bannerSub = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            worldLabel = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            mapLabel = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperRight };
            mapLabel.normal.textColor = new Color(1f, .9f, .68f);
        }

        public void Draw(PlayerState me)
        {
            if (me == null) return;
            InitStyles();
            var def = world.maps[me.map].def;
            bool town = def.kind == MapKind.Town;
            DrawWorldLabels(me, def);
            DrawMonsterBars(me);
            DrawNameplates(me);
            DrawStatus(me);
            if (!string.IsNullOrEmpty(status)) GUI.Label(new Rect(Screen.width - 620, 12, 608, 24), status, statusStyle);
            GUI.Label(new Rect(Screen.width - 420, string.IsNullOrEmpty(status) ? 12 : 36, 408, 24), def.name.ToUpperInvariant() + "  ·  " + def.LevelText.ToUpperInvariant(), mapLabel);
            GUI.Box(new Rect(10, Screen.height - 48, Mathf.Min(Screen.width - 20, 1000), 38), GUIContent.none);
            GUI.Label(new Rect(22, Screen.height - 42, 1250, 30), town
                ? "A/D Move | SPACE Jump | DOWN+SPACE Drop | W/Up Portal | E Merchant / Waystone / Smithy | I Inventory"
                : "A/D Move | SPACE Jump | DOWN+SPACE Drop | W/S Ladder | J/K Attack | L Dash | Q/F Potions | E Loot | W/Up Portal | T Town | I Bag", smallStyle);
            if (inventoryOpen) DrawInventory(me);
            if (shopOpen && town) DrawShop(me, def);
            if (waystoneOpen && town) DrawWaystone(me);
            if (smithyOpen && def.hasSmithy) DrawSmithy(me, def);
            if (me.portalCast > 0) GUI.Label(new Rect(18, 218, 500, 35), "Casting Town Portal...", bodyStyle);
            DrawBanner(me, def);
            if (me.dead)
            {
                string townName = Maps.All[Maps.IsTown(me.ch.lastTown) ? me.ch.lastTown : Maps.FirstTown].name.ToUpperInvariant();
                GUI.Label(new Rect(0, Screen.height / 2f - 25, Screen.width, 65),
                          me.deathClock < Tuning.RespawnDelay ? "YOU FELL IN BATTLE" : "YOU FELL IN BATTLE — R: WAKE IN " + townName + "   N: YOUR HEROES", centeredStyle);
            }
            if (menuOpen) DrawMenu();
        }

        // The map's name, big, for a few seconds after arriving.
        void DrawBanner(PlayerState me, MapDef def)
        {
            if (me.map != bannerMap) { bannerMap = me.map; bannerClock = 0; }
            bannerClock += Time.deltaTime;
            if (bannerClock > BannerTime) return;
            float fade = Mathf.Clamp01(Mathf.Min(bannerClock / .35f, (BannerTime - bannerClock) / .8f));
            float y = Screen.height * .2f;
            Fill(new Rect(Screen.width / 2f - 260, y - 8, 520, 86), new Color(.04f, .05f, .09f, .55f * fade));
            bannerTitle.normal.textColor = new Color(1f, .9f, .66f, fade);
            bannerSub.normal.textColor = new Color(.85f, .9f, 1f, fade);
            GUI.Label(new Rect(0, y - 4, Screen.width, 46), def.name.ToUpperInvariant(), bannerTitle);
            GUI.Label(new Rect(0, y + 42, Screen.width, 26), def.kind == MapKind.Town ? "TOWN" : "MONSTERS " + def.LevelText.ToUpperInvariant(), bannerSub);
        }

        // Screen position of a world point, in GUI coordinates (y down). False if it is off screen.
        static bool ToScreen(Camera cam, float x, float y, out Vector2 point)
        {
            Vector3 s = cam.WorldToScreenPoint(new Vector3(x, y, 0));
            point = new Vector2(s.x, Screen.height - s.y);
            return s.z >= 0 && s.x > -150 && s.x < Screen.width + 150 && s.y > -50 && s.y < Screen.height + 50;
        }

        void Label(Camera cam, float x, float y, string text, Color color)
        {
            Vector2 at;
            if (!ToScreen(cam, x, y, out at)) return;
            var size = worldLabel.CalcSize(new GUIContent(text));
            Fill(new Rect(at.x - size.x / 2f - 5, at.y - 10, size.x + 10, 20), new Color(.04f, .05f, .09f, .6f));
            worldLabel.normal.textColor = color;
            GUI.Label(new Rect(at.x - 150, at.y - 10, 300, 20), text, worldLabel);
        }

        // Where each portal leads, and who's who in town.
        void DrawWorldLabels(PlayerState me, MapDef def)
        {
            var cam = Camera.main;
            if (cam == null) return;
            foreach (var portal in def.portals)
                Label(cam, portal.x, portal.y + 2.35f, Maps.All[portal.targetMap].name.ToUpperInvariant() + "  [UP]", new Color(.75f, .9f, 1f));
            if (def.kind != MapKind.Town) return;
            if (me.returnReady && me.returnMap >= 0 && me.returnMap < Maps.All.Length)
                Label(cam, def.returnX, def.returnY + 2.35f, "BACK TO " + Maps.All[me.returnMap].name.ToUpperInvariant() + "  [UP]", new Color(1f, .88f, .6f));
            else
                Label(cam, def.returnX, def.returnY + 2.35f, "RETURN PORTAL (OPENS WHEN YOU USE T IN THE FIELD)", new Color(.75f, .72f, .66f));
            foreach (var stand in def.merchants) Label(cam, stand.x, stand.y + 2.5f, "MERCHANT  [E]", new Color(1f, .85f, .55f));
            Label(cam, def.waystoneX, def.waystoneY + 2.35f, "WAYSTONE  [E]", new Color(.7f, .95f, 1f));
            if (def.hasSmithy) Label(cam, def.smithyX, def.smithyY + 2.5f, "SMITHY  [E]", new Color(1f, .7f, .45f));
        }

        // A health bar and level over each monster that has been hurt (classic MapleStory shows its bar once you hit it).
        void DrawMonsterBars(PlayerState me)
        {
            var cam = Camera.main;
            if (cam == null) return;
            foreach (var m in world.maps[me.map].monsters)
            {
                var d = m.Def;
                if (m.hp >= d.hp - .01f) continue;
                Vector2 at;
                if (!ToScreen(cam, m.x, m.y + d.hover + d.height + .35f, out at)) continue;
                float ratio = Mathf.Clamp01(m.hp / d.hp);
                Fill(new Rect(at.x - 30, at.y - 4, 60, 8), new Color(.05f, .05f, .08f, .85f));
                Fill(new Rect(at.x - 29, at.y - 3, 58 * ratio, 6), new Color(.9f, .25f, .25f));
                worldLabel.normal.textColor = d.level > me.ch.level + 3 ? new Color(1f, .55f, .5f) : Color.white;
                GUI.Label(new Rect(at.x - 100, at.y - 22, 200, 18), "Lv " + d.level + "  " + d.name, worldLabel);
            }
        }

        // Names over the other heroes on your map (online play).
        void DrawNameplates(PlayerState me)
        {
            var cam = Camera.main;
            if (cam == null || world.players.Count < 2) return;
            foreach (var p in world.players)
            {
                if (p == me || p.map != me.map) continue;
                Vector3 s = cam.WorldToScreenPoint(new Vector3(p.x, p.y + 2.3f, 0));
                if (s.z < 0) continue;
                GUI.Label(new Rect(s.x - 90, Screen.height - s.y - 11, 180, 22), (p.ch.name ?? "") + "  Lv " + p.ch.level, nameplate);
            }
        }

        void DrawMenu()
        {
            Rect r = new Rect((Screen.width - 320) / 2f, (Screen.height - 250) / 2f, 320, 250);
            GUI.Box(r, "MENU   [ESC] Close");
            GUI.Label(new Rect(r.x + 20, r.y + 30, 280, 24), "Your hero saves automatically.", smallStyle);
            if (GUI.Button(new Rect(r.x + 30, r.y + 66, 260, 44), "RESUME", menuButton)) menuOpen = false;
            if (GUI.Button(new Rect(r.x + 30, r.y + 120, 260, 44), online ? "SAVE & LEAVE GAME" : "SAVE & YOUR HEROES", menuButton)) { menuOpen = false; if (onCharacterSelect != null) onCharacterSelect(); }
            if (GUI.Button(new Rect(r.x + 30, r.y + 174, 260, 44), "SAVE & QUIT", menuButton)) { menuOpen = false; if (onQuit != null) onQuit(); }
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
            GUI.Label(new Rect(23, 17, 200, 25), (ch.name ?? me.Hero.name).ToUpperInvariant(), titleText);
            GUI.Label(new Rect(170, 19, 191, 22), me.Hero.name.ToUpperInvariant() + "   LV " + ch.level, detailRight);
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
                GUI.Label(new Rect(r.x + 18 + (i % 2) * 300, r.y + 52 + (i / 2) * 27, 290, 25), ((Slot)i) + ": " + (item == null ? "Empty" : item.DisplayName), smallStyle);
            }
            for (int i = 0; i < Tuning.InventoryCapacity; i++)
            {
                int col = i % 5, row = i / 5;
                Rect cell = new Rect(r.x + 18 + col * 120, r.y + 119 + row * 75, 115, 68);
                Item item = i < ch.inventory.Count ? ch.inventory[i] : null;
                if (item == null) { GUI.Box(cell, "—"); continue; }
                Color old = GUI.color;
                GUI.color = Loot.RarityColor(item.rarity);
                bool clicked = GUI.Button(cell, item.DisplayName + "\n+" + item.Attack + " ATK / +" + item.Defense + " DEF\n+" + item.Health + " HP");
                GUI.color = old;
                if (clicked) { commands.Equip(i); break; }   // the list changed: redraw next frame
            }
            GUI.Label(new Rect(r.x + 18, r.y + 422, 594, 50), PartsLine(ch), smallStyle);
        }

        void DrawShop(PlayerState me, MapDef def)
        {
            var ch = me.ch;
            Rect r = PanelRect();
            GUI.Box(r, def.name.ToUpperInvariant() + " GENERAL STORE — [B] Close");
            GUI.Label(new Rect(r.x + 20, r.y + 32, 550, 28), "Gold: " + ch.gold + "   |   Sell unwanted gear for half its value", bodyStyle);
            if (GUI.Button(new Rect(r.x + 20, r.y + 70, 270, 42), "Buy HP Potion — " + Tuning.HpPotionPrice + " gold")) commands.BuyPotion(true);
            if (GUI.Button(new Rect(r.x + 320, r.y + 70, 270, 42), "Buy MP Potion — " + Tuning.MpPotionPrice + " gold")) commands.BuyPotion(false);
            GUI.Label(new Rect(r.x + 20, r.y + 123, 560, 25), "Merchant equipment (reliable Common gear)", smallStyle);
            for (int i = 0; i < 4; i++)
            {
                Slot slot = (Slot)i;
                int cost = Loot.ShopGearCost(slot, ch.level);
                if (GUI.Button(new Rect(r.x + 20 + (i % 2) * 300, r.y + 154 + (i / 2) * 49, 270, 40), "Buy Common " + Loot.SlotName(slot, me.Hero) + " — " + cost + "g"))
                    commands.BuyGear(slot);
            }
            GUI.Label(new Rect(r.x + 20, r.y + 269, 550, 25), "Sell inventory items", smallStyle);
            for (int i = 0; i < ch.inventory.Count && i < 10; i++)
            {
                Item item = ch.inventory[i];
                int col = i % 2, row = i / 2;
                if (GUI.Button(new Rect(r.x + 20 + col * 300, r.y + 297 + row * 31, 270, 29), "Sell " + item.DisplayName + " +" + Loot.SellPrice(item) + "g"))
                { commands.Sell(i); break; }
            }
        }
    
        // Waystone: travel to any town you have found (walking into a town finds it).
        void DrawWaystone(PlayerState me)
        {
            Rect r = new Rect((Screen.width - 420) / 2f, Mathf.Max(120, (Screen.height - 360) / 2f), 420, 360);
            GUI.Box(r, "WAYSTONE — [E] Close");
            GUI.Label(new Rect(r.x + 20, r.y + 28, 380, 24), "Travel to a town you have found.", smallStyle);
            float y = r.y + 62;
            for (int i = 0; i < Maps.All.Length; i++)
            {
                if (!Maps.IsTown(i)) continue;
                var town = Maps.All[i];
                bool known = me.ch.KnowsTown(i), here = i == me.map;
                GUI.enabled = known && !here;
                string text = here ? town.name + "  (you are here)" : known ? "Travel to " + town.name : "??? (not found yet)";
                if (GUI.Button(new Rect(r.x + 30, y, 360, 40), text, menuButton)) { commands.Travel(i); waystoneOpen = false; }
                GUI.enabled = true;
                y += 48;
            }
        }

        // "PARTS  Ember Ash 12 · Boar Tusk 3" (only the ones you carry).
        static string PartsLine(Character ch)
        {
            var text = new System.Text.StringBuilder("PARTS  ");
            bool any = false;
            for (int i = 0; i < ch.parts.Length; i++)
            {
                if (ch.parts[i] <= 0) continue;
                if (any) text.Append("  ·  ");
                text.Append(Parts.All[i].name).Append(' ').Append(ch.parts[i]);
                any = true;
            }
            if (!any) text.Append("none yet: monsters drop them, chests spill several");
            return text.ToString();
        }

        static string StatLine(Item item, int upgrade)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (item.attack > 0) parts.Add("ATK " + (item.attack + upgrade * Smithing.Step(item.attack)));
            if (item.defense > 0) parts.Add("DEF " + (item.defense + upgrade * Smithing.Step(item.defense)));
            if (item.health > 0) parts.Add("HP " + (item.health + upgrade * Smithing.Step(item.health)));
            return string.Join("  ", parts.ToArray());
        }

        // Smithy: every worn and carried item, what its next upgrade gives and costs.
        void DrawSmithy(PlayerState me, MapDef def)
        {
            var ch = me.ch;
            float w = Mathf.Min(Screen.width - 20, 820);
            Rect r = new Rect((Screen.width - w) / 2f, Mathf.Max(120, (Screen.height - 480) / 2f), w, 480);
            GUI.Box(r, def.name.ToUpperInvariant() + " SMITHY — [E] Close");
            GUI.Label(new Rect(r.x + 18, r.y + 26, w - 36, 22), "Upgrades always succeed, up to +" + Tuning.MaxUpgrade + ". Gold: " + ch.gold, smallStyle);
            GUI.Label(new Rect(r.x + 18, r.y + 48, w - 36, 44), PartsLine(ch), smallStyle);

            int rows = ch.equipped.Length + ch.inventory.Count;
            Rect view = new Rect(r.x + 10, r.y + 96, r.width - 20, r.height - 106);
            smithyScroll = GUI.BeginScrollView(view, smithyScroll, new Rect(0, 0, view.width - 20, rows * SmithyRowHeight));
            float y = 0;
            for (int target = 0; target < ch.equipped.Length; target++, y += SmithyRowHeight)
                SmithyRow(ch, target, "Worn " + ((Slot)target), y, view.width - 20);
            for (int i = 0; i < ch.inventory.Count; i++, y += SmithyRowHeight)
                SmithyRow(ch, Smithing.InventoryTarget + i, "Bag", y, view.width - 20);
            GUI.EndScrollView();
        }

        void SmithyRow(Character ch, int target, string where, float y, float width)
        {
            var item = World.UpgradeTarget(ch, target);
            GUI.Box(new Rect(0, y, width, SmithyRowHeight - 4), GUIContent.none);
            if (item == null) { GUI.Label(new Rect(10, y + 6, width - 20, 22), where + ": empty", smallStyle); return; }
            Color old = GUI.color;
            GUI.color = Loot.RarityColor(item.rarity);
            GUI.Label(new Rect(10, y + 4, 330, 22), where + ": " + item.DisplayName + "  (Lv " + item.level + ")", smallStyle);
            GUI.color = old;
            if (item.upgrade >= Tuning.MaxUpgrade)
            {
                GUI.Label(new Rect(10, y + 27, 330, 22), StatLine(item, item.upgrade) + "   ·   fully upgraded", smallStyle);
                return;
            }
            var cost = Smithing.Cost(item);
            GUI.Label(new Rect(10, y + 27, 330, 22), StatLine(item, item.upgrade) + "   ->   " + StatLine(item, item.upgrade + 1), smallStyle);
            string price = cost.partCount + " " + Parts.All[cost.part].name + " (" + ch.parts[cost.part] + ")";
            if (cost.rareCount > 0) price += "  +  " + cost.rareCount + " " + Parts.All[cost.rarePart].name + " (" + ch.parts[cost.rarePart] + ")";
            price += "  +  " + cost.gold + "g";
            GUI.Label(new Rect(340, y + 4, width - 350, 22), price, smallStyle);
            GUI.enabled = Smithing.CanPay(ch, cost);
            if (GUI.Button(new Rect(width - 160, y + 26, 150, 24), "UPGRADE TO +" + (item.upgrade + 1))) commands.Upgrade(target);
            GUI.enabled = true;
        }

    }
}
