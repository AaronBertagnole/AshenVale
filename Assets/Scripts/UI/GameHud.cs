using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshenVale
{
    // In-game IMGUI: status panel, key hints, map banner, minimap, labels over portals and town folk, monster health bars,
    // inventory, shop, waystone, smithy, world map and the Esc menu. Clicks become World commands (Equip, BuyPotion, BuyGear, Sell,
    // Travel, Upgrade), which the world checks before applying.
    public sealed class GameHud
    {
        const float NpcReach = 2f;          // how close you must be to the merchant or waystone to open its window
        const float NpcLeaveReach = 3f;     // walking further than this closes it
        const float BannerTime = 3.2f;
        const float SmithyRowHeight = 58f;
        const float MinimapWidth = 300f, MinimapMaxHeight = 160f;
        const string WorldMapArt = "Art/UI/world_map";     // optional painted world map (see WorldAtlas)

        readonly World world;
        public bool inventoryOpen, shopOpen, menuOpen, waystoneOpen, smithyOpen, worldMapOpen;
        public bool minimapShown = true;    // Tab
        public bool WindowOpen { get { return inventoryOpen || shopOpen || menuOpen || waystoneOpen || smithyOpen || worldMapOpen; } }

        // Esc menu choices, handled by the game (they save first).
        public Action onCharacterSelect, onQuit;
        public IGameCommands commands;      // local World, or the host when playing as a client
        public string status = "";          // top-right line: join code, players, connection state
        public bool online;                 // changes the Esc menu wording

        GUIStyle titleText, barText, detailText, detailRight, bodyStyle, smallStyle, centeredStyle, menuButton, statusStyle, nameplate,
                 bannerTitle, bannerSub, worldLabel, mapLabel, minimapTitle, minimapHint, tooltipText,
                 atlasTitle, atlasCorner, atlasName, atlasSub, atlasSmall;
        static Texture2D dotTexture;
        int bannerMap = -1;
        float bannerClock;
        Vector2 smithyScroll;

        public GameHud(World world) { this.world = world; }

        public void Close() { inventoryOpen = shopOpen = menuOpen = waystoneOpen = smithyOpen = worldMapOpen = false; bannerMap = -1; }

        // Window keys: I inventory, M world map, Tab shows / hides the minimap, E (or B) at the merchant for the shop,
        // E at a waystone for travel, E at a smithy for upgrades, Esc closes a window or opens the menu. The world
        // keeps running while windows are open; only attacks are blocked.
        public void HandleKeys(PlayerState me)
        {
            if (me == null || me.dead) { inventoryOpen = shopOpen = menuOpen = waystoneOpen = smithyOpen = worldMapOpen = false; return; }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (inventoryOpen || shopOpen || waystoneOpen || smithyOpen || worldMapOpen) { inventoryOpen = shopOpen = waystoneOpen = smithyOpen = worldMapOpen = false; }
                else menuOpen = !menuOpen;
            }
            if (menuOpen) return;
            if (Input.GetKeyDown(KeyCode.Tab)) minimapShown = !minimapShown;
            if (Input.GetKeyDown(KeyCode.M)) { worldMapOpen = !worldMapOpen; inventoryOpen = shopOpen = waystoneOpen = smithyOpen = false; }
            if (Input.GetKeyDown(KeyCode.I)) { inventoryOpen = !inventoryOpen; shopOpen = waystoneOpen = smithyOpen = worldMapOpen = false; }
            bool use = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.B);
            bool talk = Input.GetKeyDown(KeyCode.E);
            if (use && world.NearMerchant(me, NpcReach)) { shopOpen = !shopOpen; inventoryOpen = waystoneOpen = smithyOpen = worldMapOpen = false; }
            else if (talk && world.NearWaystone(me, NpcReach)) { waystoneOpen = !waystoneOpen; inventoryOpen = shopOpen = smithyOpen = worldMapOpen = false; }
            else if (talk && world.NearSmithy(me, NpcReach)) { smithyOpen = !smithyOpen; inventoryOpen = shopOpen = waystoneOpen = worldMapOpen = false; }
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
            minimapTitle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
            minimapTitle.normal.textColor = new Color(1f, .9f, .68f);
            minimapHint = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.UpperRight };
            minimapHint.normal.textColor = new Color(.7f, .74f, .8f);
            tooltipText = new GUIStyle(smallStyle) { fontSize = 13, wordWrap = true };
            // World map text: dark ink on parchment.
            atlasTitle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
            atlasTitle.normal.textColor = new Color(.30f, .19f, .10f);
            atlasCorner = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperRight };
            atlasCorner.normal.textColor = new Color(.38f, .26f, .15f);
            atlasName = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            atlasName.normal.textColor = new Color(.24f, .15f, .08f);
            atlasSub = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter };
            atlasSub.normal.textColor = new Color(.42f, .30f, .18f);
            atlasSmall = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            atlasSmall.normal.textColor = new Color(.32f, .22f, .12f);
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
            float corner = string.IsNullOrEmpty(status) ? 10 : 36;
            if (minimapShown) DrawMinimap(me, def, corner);
            else GUI.Label(new Rect(Screen.width - 420, corner + 2, 408, 24), def.name.ToUpperInvariant() + "  ·  " + def.LevelText.ToUpperInvariant(), mapLabel);
            GUI.Box(new Rect(10, Screen.height - 48, Mathf.Min(Screen.width - 20, 1080), 38), GUIContent.none);
            GUI.Label(new Rect(22, Screen.height - 42, 1250, 30), town
                ? "A/D Move | SPACE Jump | DOWN+SPACE Drop | W/Up Portal | E Merchant / Waystone / Smithy | I Inventory | M Map"
                : "A/D Move | SPACE Jump | DOWN+SPACE Drop | W/S Ladder | J/K Attack | L Dash | Q/F Potions | E Loot | W/Up Portal | T Town | I Bag | M Map", smallStyle);
            if (inventoryOpen) DrawInventory(me);
            if (shopOpen && town) DrawShop(me, def);
            if (waystoneOpen && town) DrawWaystone(me);
            if (smithyOpen && def.hasSmithy) DrawSmithy(me, def);
            if (worldMapOpen) DrawWorldMap(me);
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

        // ---------------------------------------------------------------- minimap and world map

        // Top right (Tab hides it): this map's ground, platforms, ladders and portals, town folk, monsters (red),
        // chests (gold), other heroes (green), you (yellow), and a frame around the part of the map on screen.
        void DrawMinimap(PlayerState me, MapDef def, float top)
        {
            float s = Mathf.Min(MinimapWidth / def.columns, MinimapMaxHeight / def.rows);   // pixels per world unit
            float w = def.columns * s, h = def.rows * s;
            float x0 = Screen.width - 18 - w, y0 = top + 26;
            float low = def.Bottom, high = def.Top;
            float MX(float x) { return x0 + Mathf.Clamp(x, 0f, def.columns) * s; }
            float MY(float y) { return y0 + (high - Mathf.Clamp(y, low, high)) * s; }

            Rect frame = new Rect(x0 - 8, top, w + 16, h + 52);
            Fill(frame, new Color(.055f, .075f, .12f, .88f));
            Fill(new Rect(frame.x, frame.y, frame.width, 2), new Color(.65f, .48f, .27f));
            GUI.Label(new Rect(frame.x + 8, top + 5, frame.width - 16, 20), def.name.ToUpperInvariant() + "  ·  " + def.LevelText.ToUpperInvariant(), minimapTitle);
            Fill(new Rect(x0, y0, w, h), new Color(.6f, .7f, .9f, .08f));

            var floor = new Color(.86f, .78f, .6f, .9f);
            Fill(new Rect(x0, MY(low) - 2, w, 2), floor);
            foreach (var p in def.platforms) Fill(new Rect(MX(p.left), MY(p.top) - 1, (p.right - p.left) * s, 2), floor);
            foreach (var l in def.ladders) Fill(new Rect(MX(l.x) - .5f, MY(l.top), 1, (l.top - l.bottom) * s), new Color(.7f, .55f, .35f, .8f));
            foreach (var portal in def.portals)
            {
                if (WorldAtlas.IsSecret(portal.targetMap) && !me.ch.HasSeen(portal.targetMap)) continue;   // don't give secrets away
                Marker(MX(portal.x), MY(portal.y + .8f), 6, new Color(.55f, .6f, 1f));
            }
            if (def.kind == MapKind.Town)
            {
                Marker(MX(def.returnX), MY(def.returnY + .8f), 6, new Color(.8f, .7f, 1f));
                foreach (var stand in def.merchants) Square(MX(stand.x), MY(stand.y + .6f), 4, new Color(1f, .85f, .5f));
                Square(MX(def.waystoneX), MY(def.waystoneY + .6f), 4, new Color(.55f, .95f, 1f));
            }
            if (def.hasSmithy) Square(MX(def.smithyX), MY(def.smithyY + .6f), 4, new Color(1f, .6f, .35f));
            foreach (var m in world.maps[me.map].monsters)
            {
                if (m.Def.IsChest) Square(MX(m.x), MY(m.y + .4f), 5, new Color(1f, .8f, .25f));
                else Marker(MX(m.x), MY(m.y + .5f), 4, new Color(1f, .3f, .28f), false);
            }
            foreach (var p in world.players)
                if (p != me && p.map == me.map && !p.dead) Marker(MX(p.x), MY(p.y + .6f), 6, new Color(.45f, 1f, .5f));
            Marker(MX(me.x), MY(me.y + .6f), 7, new Color(1f, .92f, .3f));

            var cam = Camera.main;
            if (cam != null && cam.orthographic)
            {
                float hh = cam.orthographicSize, hw = hh * cam.aspect;
                Vector3 c = cam.transform.position;
                float left = MX(c.x - hw), right = MX(c.x + hw), viewTop = MY(c.y + hh), viewBottom = MY(c.y - hh);
                Outline(new Rect(left, viewTop, right - left, viewBottom - viewTop), new Color(1f, 1f, 1f, .35f));
            }
            GUI.Label(new Rect(frame.x + 8, y0 + h + 6, frame.width - 16, 18), "M  WORLD MAP   ·   TAB  HIDE", minimapHint);
        }

        // M: the world as far as you know it. Places you have been, "???" for the places next door, the roads
        // between them, where you are and where the rest of your party is. Hover a place to see its monsters.
        void DrawWorldMap(PlayerState me)
        {
            var ch = me.ch;
            float w = Mathf.Min(Screen.width - 40, 1000), h = Mathf.Min(Screen.height - 150, 580);
            Rect r = new Rect((Screen.width - w) / 2f, Mathf.Max(60, (Screen.height - h) / 2f - 10), w, h);
            Fill(new Rect(r.x - 5, r.y - 5, r.width + 10, r.height + 10), new Color(.23f, .15f, .09f, .97f));
            var art = Sprites.Art(WorldMapArt);
            if (art != null) GUI.DrawTexture(r, art.texture, ScaleMode.StretchToFill);
            else
            {
                Fill(r, new Color(.91f, .84f, .68f, .98f));
                Outline(new Rect(r.x + 8, r.y + 8, r.width - 16, r.height - 16), new Color(.55f, .4f, .25f, .6f));
            }
            GUI.Label(new Rect(r.x, r.y + 14, r.width, 34), "WORLD MAP", atlasTitle);
            GUI.Label(new Rect(r.x + 20, r.y + 20, r.width - 44, 20), "[M] CLOSE", atlasCorner);
            Rect area = new Rect(r.x + 70, r.y + 72, r.width - 140, r.height - 160);

            foreach (var road in WorldAtlas.Roads)
            {
                bool a = ch.HasSeen(road.x), b = ch.HasSeen(road.y);
                if ((!a && !b) || !WorldAtlas.Shown(ch, road.x) || !WorldAtlas.Shown(ch, road.y)) continue;
                DottedLine(AtlasPoint(area, road.x), AtlasPoint(area, road.y), new Color(.42f, .28f, .16f, a && b ? .9f : .35f));
            }

            int hovered = -1, found = 0;
            Vector2 mouse = Event.current.mousePosition;
            for (int i = 0; i < Maps.All.Length; i++)
            {
                bool seen = ch.HasSeen(i);
                if (seen) found++;
                if (!WorldAtlas.Shown(ch, i)) continue;
                var def = Maps.All[i];
                bool here = i == me.map, town = def.kind == MapKind.Town;
                Vector2 at = AtlasPoint(area, i);
                if (here) Marker(at.x, at.y, 30f + 6f * Mathf.Sin(Time.unscaledTime * 4f), new Color(1f, .9f, .35f, .5f), false);
                if (!seen) Marker(at.x, at.y, 16, new Color(.62f, .57f, .5f));
                else if (town) Square(at.x, at.y, 17, new Color(.95f, .74f, .34f));
                else Marker(at.x, at.y, 18, DangerColor(def, ch.level));
                if (!seen) GUI.Label(new Rect(at.x - 10, at.y - 10, 20, 20), "?", atlasName);
                AtlasText(at.x, at.y + 22, seen ? def.name : "???", atlasName);
                AtlasText(at.x, at.y + 38, !seen ? "unexplored" : town ? "Town · waystone" : def.LevelText, atlasSub);
                if (here) AtlasText(at.x, at.y - 29, "YOU ARE HERE", atlasSub);
                if ((mouse - at).sqrMagnitude < 20f * 20f) hovered = i;
            }

            int k = 0;
            var party = new System.Text.StringBuilder();
            foreach (var p in world.players)
            {
                if (p == me) continue;
                Vector2 at = AtlasPoint(area, p.map);
                Marker(at.x + 15 + 8 * (k % 3), at.y - 15 - 8 * (k / 3), 8, new Color(.4f, .95f, .45f));
                if (party.Length > 0) party.Append("   ·   ");
                party.Append(p.ch.name ?? "Hero").Append(" (Lv ").Append(p.ch.level).Append(")  ").Append(Maps.All[p.map].name);
                k++;
            }

            float footer = r.yMax - 70;
            GUI.Label(new Rect(r.x + 26, footer, r.width - 52, 20), "Discovered " + found + " of " + Maps.All.Length + " places", atlasSmall);
            GUI.Label(new Rect(r.x + 26, footer + 20, r.width - 52, 20),
                      "Circle: hunting ground (green about your level, red tougher, grey too easy)   ·   Square: town   ·   ?: not explored yet", atlasSmall);
            if (party.Length > 0) GUI.Label(new Rect(r.x + 26, footer + 40, r.width - 52, 20), "PARTY   " + party, atlasSmall);
            if (hovered >= 0) AtlasTooltip(me, hovered, mouse);
        }

        // A world map label centred on (x, y), on a patch of parchment so roads don't run through the letters.
        static void AtlasText(float x, float y, string text, GUIStyle style)
        {
            var size = style.CalcSize(new GUIContent(text));
            Fill(new Rect(x - size.x / 2f - 4f, y - size.y / 2f + 1f, size.x + 8f, size.y - 2f), new Color(.91f, .84f, .68f, .88f));
            GUI.Label(new Rect(x - size.x / 2f - 4f, y - size.y / 2f, size.x + 8f, size.y), text, style);
        }

        // What hovering a place on the world map tells you.
        void AtlasTooltip(PlayerState me, int map, Vector2 mouse)
        {
            var def = Maps.All[map];
            bool seen = me.ch.HasSeen(map);
            var text = new System.Text.StringBuilder();
            if (!seen) text.Append("Not explored yet. Follow the road to find out what lives here.");
            else if (def.kind == MapKind.Town)
                text.Append(def.merchants.Length > 1 ? "Merchants" : "Merchant").Append(", waystone").Append(def.hasSmithy ? ", smithy" : "").Append(".");
            else
            {
                var names = new List<string>();
                bool chests = false;
                foreach (var spawn in def.spawns)
                {
                    var m = Monsters.All[spawn.monster];
                    if (m.IsChest) { chests = true; continue; }
                    string entry = m.name + " (Lv " + m.level + ")";
                    if (!names.Contains(entry)) names.Add(entry);
                }
                text.Append(string.Join(", ", names.ToArray()));
                if (chests) text.Append("\nChests turn up here now and then.");
            }
            string title = seen ? def.name.ToUpperInvariant() + "   ·   " + def.LevelText.ToUpperInvariant() : "???";
            var body = new GUIContent(text.ToString());
            float width = 320f, height = tooltipText.CalcHeight(body, width - 20f) + 38f;
            Rect box = new Rect(Mathf.Min(mouse.x + 18, Screen.width - width - 10), Mathf.Min(mouse.y + 18, Screen.height - height - 10), width, height);
            Fill(box, new Color(.055f, .075f, .12f, .96f));
            Fill(new Rect(box.x, box.y, box.width, 2), new Color(.65f, .48f, .27f));
            GUI.Label(new Rect(box.x + 10, box.y + 7, width - 20, 20), title, minimapTitle);
            GUI.Label(new Rect(box.x + 10, box.y + 29, width - 20, height - 32), body, tooltipText);
        }

        // Green: monsters about your level. Red: tougher than you. Grey: so far below you they give little XP.
        static Color DangerColor(MapDef def, int level)
        {
            if (def.maxLevel <= 0) return new Color(.62f, .57f, .5f);
            if (def.minLevel > level + 3) return new Color(.86f, .32f, .25f);
            if (def.maxLevel < level - Tuning.OutLevelGrace) return new Color(.62f, .64f, .58f);
            return new Color(.40f, .72f, .36f);
        }

        static Vector2 AtlasPoint(Rect area, int map)
        {
            var spot = WorldAtlas.Spot(map);
            return new Vector2(area.x + spot.x * area.width, area.y + spot.y * area.height);
        }

        static void DottedLine(Vector2 a, Vector2 b, Color color)
        {
            int dots = Mathf.Max(1, Mathf.FloorToInt(Vector2.Distance(a, b) / 11f));
            for (int i = 1; i < dots; i++)      // the ends are under the place markers
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)dots);
                Marker(p.x, p.y, 5, color, false);
            }
        }

        // A soft round dot, white, 16 px (tinted and scaled for map markers).
        static Texture2D DotTexture
        {
            get
            {
                if (dotTexture != null) return dotTexture;
                const int size = 16;
                dotTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                float radius = size * .5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float d = Mathf.Sqrt((x + .5f - radius) * (x + .5f - radius) + (y + .5f - radius) * (y + .5f - radius));
                        dotTexture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - d)));
                    }
                dotTexture.Apply();
                return dotTexture;
            }
        }

        // A round marker centred on (x, y); the dark rim makes it read on any background.
        static void Marker(float x, float y, float size, Color color, bool rim = true)
        {
            Color previous = GUI.color;
            if (rim)
            {
                GUI.color = new Color(.05f, .04f, .03f, .85f * color.a);
                GUI.DrawTexture(new Rect(x - size / 2f - 1.5f, y - size / 2f - 1.5f, size + 3f, size + 3f), DotTexture);
            }
            GUI.color = color;
            GUI.DrawTexture(new Rect(x - size / 2f, y - size / 2f, size, size), DotTexture);
            GUI.color = previous;
        }

        // A square marker with a dark rim (chests, towns, town folk).
        static void Square(float x, float y, float size, Color color)
        {
            Fill(new Rect(x - size / 2f - 1f, y - size / 2f - 1f, size + 2f, size + 2f), new Color(.05f, .04f, .03f, .9f));
            Fill(new Rect(x - size / 2f, y - size / 2f, size, size), color);
        }

        static void Outline(Rect r, Color color)
        {
            Fill(new Rect(r.x, r.y, r.width, 1), color);
            Fill(new Rect(r.x, r.yMax - 1, r.width, 1), color);
            Fill(new Rect(r.x, r.y, 1, r.height), color);
            Fill(new Rect(r.xMax - 1, r.y, 1, r.height), color);
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
