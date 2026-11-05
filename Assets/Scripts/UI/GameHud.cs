using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshenVale
{
    // Themed IMGUI windows and world labels; status/minimap fallbacks are hidden while the Canvas HUD is active.
    // Labels over portals and town folk, monster health bars,
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
        public bool canvasActive;
        public HudTheme canvasTheme;
        public Action<string, Color> onPickup;
        public Action<GameAction> onUseItem;
        public enum InventorySection { Equips, Useables, MonsterParts }
        public InventorySection inventorySection = InventorySection.Equips;
        Vector2 inventoryPartsScroll;
        GUISkin canvasSkin;

        // The Canvas buttons use the same window state and command routes as the keyboard.
        public void ToggleCanvasWindow(int which)
        {
            bool wasOpen = which == 0 ? inventoryOpen : which == 1 ? worldMapOpen : menuOpen;
            inventoryOpen = shopOpen = waystoneOpen = smithyOpen = worldMapOpen = menuOpen = controlsOpen = false;
            capturingAction = -1;
            if (!wasOpen) { if (which == 0) inventoryOpen = true; else if (which == 1) worldMapOpen = true; else menuOpen = true; }
        }

        public void ReleaseCanvasSkin()
        {
            if (canvasSkin != null) UnityEngine.Object.Destroy(canvasSkin);
            canvasSkin = null; titleText = null;
        }

        void UseCanvasSkin()
        {
            if (canvasSkin == null)
            {
                canvasSkin = UnityEngine.Object.Instantiate(GUI.skin);
                canvasSkin.hideFlags = HideFlags.DontSave;
                canvasSkin.box.normal.background = canvasTheme.panel.texture;
                canvasSkin.box.border = new RectOffset(16, 16, 16, 16);
                canvasSkin.box.normal.textColor = HudTheme.Ivory;
                canvasSkin.box.fontSize = 15; canvasSkin.box.fontStyle = FontStyle.Bold;
                canvasSkin.box.padding = new RectOffset(12, 12, 8, 8);
                canvasSkin.button.border = new RectOffset(16, 16, 16, 16);
                canvasSkin.button.normal.background = canvasTheme.slot.texture;
                canvasSkin.button.hover.background = canvasTheme.slot.texture;
                canvasSkin.button.active.background = canvasTheme.track.texture;
                canvasSkin.button.normal.textColor = HudTheme.Ivory;
                canvasSkin.button.hover.textColor = HudTheme.Teal;
                canvasSkin.button.active.textColor = HudTheme.Ivory;
                canvasSkin.label.normal.textColor = HudTheme.Ivory;
                titleText = null; // Recreate styles from this skin once.
            }
            GUI.skin = canvasSkin;
        }
        public bool inventoryOpen, shopOpen, menuOpen, waystoneOpen, smithyOpen, worldMapOpen, controlsOpen;
        public bool minimapShown = true;
        public bool WindowOpen { get { return inventoryOpen || shopOpen || menuOpen || waystoneOpen || smithyOpen || worldMapOpen || controlsOpen; } }
        // Controls window: which binding is waiting for a key (-1 none); while one is, the game ignores the keyboard.
        int capturingAction = -1, capturingSlot;
        public bool Capturing { get { return controlsOpen && capturingAction >= 0; } }

        // Esc menu choices, handled by the game (they save first).
        public Action onCharacterSelect, onQuit;
        public IGameCommands commands;      // local World, or the host when playing as a client
        public WorldView view;              // for the floating damage numbers
        public string status = "";          // top-right line: join code, players, connection state
        public string joinCode = "";        // while hosting online: shows a COPY button next to the status
        float copiedClock;
        public bool online;                 // changes the Esc menu wording

        GUIStyle titleText, barText, detailText, detailRight, bodyStyle, smallStyle, centeredStyle, menuButton, statusStyle, nameplate,
                 bannerTitle, bannerSub, worldLabel, mapLabel, minimapTitle, minimapHint, tooltipText,
                 atlasTitle, atlasCorner, atlasName, atlasSub, atlasSmall, atlasSmallLight, atlasCornerLight, numberStyle, bigNumberStyle, bossName, inventoryTabText, activeInventoryTabText, partNameText;
        static Texture2D dotTexture;
        int bannerMap = -1;
        float bannerClock;
        Vector2 smithyScroll;

        public GameHud(World world) { this.world = world; }

        public void Close() { inventoryOpen = shopOpen = menuOpen = waystoneOpen = smithyOpen = worldMapOpen = controlsOpen = false; capturingAction = -1; bannerMap = -1; }

        // Window keys (bindings in Keys): inventory, world map, minimap, and the loot / talk key at the merchant,
        // waystone or smithy; Esc closes a window or opens the menu. The world keeps running while windows are
        // open; only attacks are blocked.
        public void HandleKeys(PlayerState me, bool ignoreMouse = false)
        {
            if (me == null || me.dead) { inventoryOpen = shopOpen = menuOpen = waystoneOpen = smithyOpen = worldMapOpen = controlsOpen = false; capturingAction = -1; return; }
            if (Capturing) return;                      // the Controls window is waiting for a key
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (controlsOpen) controlsOpen = false;
                else if (inventoryOpen || shopOpen || waystoneOpen || smithyOpen || worldMapOpen) { inventoryOpen = shopOpen = waystoneOpen = smithyOpen = worldMapOpen = false; }
                else menuOpen = !menuOpen;
            }
            if (menuOpen || controlsOpen) return;
            if (Keys.Pressed(GameAction.Minimap, ignoreMouse)) minimapShown = !minimapShown;
            if (Keys.Pressed(GameAction.WorldMap, ignoreMouse)) { worldMapOpen = !worldMapOpen; inventoryOpen = shopOpen = waystoneOpen = smithyOpen = false; }
            if (Keys.Pressed(GameAction.Inventory, ignoreMouse)) { inventoryOpen = !inventoryOpen; shopOpen = waystoneOpen = smithyOpen = worldMapOpen = false; }
            bool talk = Keys.Pressed(GameAction.Interact, ignoreMouse);
            if (talk && world.NearMerchant(me, NpcReach)) { shopOpen = !shopOpen; inventoryOpen = waystoneOpen = smithyOpen = worldMapOpen = false; }
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
            atlasSmallLight = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            atlasSmallLight.normal.textColor = new Color(.85f, .78f, .62f);
            atlasCornerLight = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperRight };
            atlasCornerLight.normal.textColor = new Color(.92f, .84f, .66f);
            atlasSmall.normal.textColor = new Color(.32f, .22f, .12f);
            numberStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            bigNumberStyle = new GUIStyle(numberStyle) { fontSize = 30 };
            bossName = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            bossName.normal.textColor = new Color(1f, .92f, .75f);
            inventoryTabText = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold };
            activeInventoryTabText = new GUIStyle(inventoryTabText);
            activeInventoryTabText.normal.textColor = HudTheme.Copper;
            partNameText = new GUIStyle(smallStyle) { fontSize = 13, alignment = TextAnchor.MiddleLeft, wordWrap = false, clipping = TextClipping.Clip };
        }

        public void Draw(PlayerState me)
        {
            GUISkin previous = GUI.skin;
            try
            {
                if (canvasActive && canvasTheme != null) UseCanvasSkin();
                DrawCore(me);
            }
            finally { GUI.skin = previous; }
        }

        void DrawCore(PlayerState me)
        {
            if (me == null) return;
            InitStyles();
            var def = world.maps[me.map].def;
            bool town = def.kind == MapKind.Town;
            DrawWorldLabels(me, def);
            DrawMonsterBars(me);
            DrawNameplates(me);
            DrawNumbers();
            DrawBossBar(me);
            if (!canvasActive)
            {
                DrawStatus(me);
                if (!string.IsNullOrEmpty(status)) GUI.Label(new Rect(Screen.width - 620, 12, 608, 24), status, statusStyle);
                if (!string.IsNullOrEmpty(joinCode))
                {
                    copiedClock = Mathf.Max(0, copiedClock - Time.deltaTime);
                    if (GUI.Button(new Rect(Screen.width - 760, 10, 130, 26), copiedClock > 0 ? "COPIED!" : "COPY CODE"))
                    { GUIUtility.systemCopyBuffer = joinCode; copiedClock = 1.5f; }
                }
                float corner = string.IsNullOrEmpty(status) ? 10 : 36;
                if (minimapShown) DrawMinimap(me, def, corner);
                else GUI.Label(new Rect(Screen.width - 420, corner + 2, 408, 24), def.name.ToUpperInvariant() + "  ·  " + def.LevelText.ToUpperInvariant(), mapLabel);
            }
            DrawPickups(me);
            if (inventoryOpen) DrawInventory(me);
            if (shopOpen && town) DrawShop(me, def);
            if (waystoneOpen && town) DrawWaystone(me);
            if (smithyOpen && def.hasSmithy) DrawSmithy(me, def);
            if (worldMapOpen) DrawWorldMap(me);
            if (controlsOpen) DrawControls();
            if (!canvasActive && me.portalCast > 0) GUI.Label(new Rect(18, 218, 500, 35), "Casting Town Portal...", bodyStyle);
            if (!canvasActive) DrawBanner(me, def);
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
                Label(cam, portal.x, portal.y + 2.35f, Maps.All[portal.targetMap].name.ToUpperInvariant() + "  [" + Keys.Name(GameAction.Up) + "]", new Color(.75f, .9f, 1f));
            if (def.kind != MapKind.Town) return;
            if (me.returnReady && me.returnMap >= 0 && me.returnMap < Maps.All.Length)
                Label(cam, def.returnX, def.returnY + 2.35f, "BACK TO " + Maps.All[me.returnMap].name.ToUpperInvariant() + "  [" + Keys.Name(GameAction.Up) + "]", new Color(1f, .88f, .6f));
            else
                Label(cam, def.returnX, def.returnY + 2.35f, "RETURN PORTAL (OPENS WHEN YOU USE " + Keys.Name(GameAction.TownPortal) + " IN THE FIELD)", new Color(.75f, .72f, .66f));
            foreach (var stand in def.merchants) Label(cam, stand.x, stand.y + 2.5f, "MERCHANT  [" + Keys.Name(GameAction.Interact) + "]", new Color(1f, .85f, .55f));
            Label(cam, def.waystoneX, def.waystoneY + 2.35f, "WAYSTONE  [" + Keys.Name(GameAction.Interact) + "]", new Color(.7f, .95f, 1f));
            if (def.hasSmithy) Label(cam, def.smithyX, def.smithyY + 2.5f, "SMITHY  [" + Keys.Name(GameAction.Interact) + "]", new Color(1f, .7f, .45f));
        }

        // What you picked up: a line per item in the bottom left, fading after a few seconds. Found by watching your
        // hero's gold, potions, parts and bag change, so it works the same for a friend playing on your game.
        sealed class Pickup { public string text; public Color color; public float age; }
        readonly List<Pickup> pickups = new List<Pickup>();
        int seenGold = -1, seenHp, seenMp, seenBag; int[] seenParts; string seenHero;
        const float PickupShow = 4f;

        void DrawPickups(PlayerState me)
        {
            var ch = me.ch;
            if (seenGold < 0 || seenHero != ch.id || seenParts == null || seenParts.Length != ch.parts.Length)
            {
                seenHero = ch.id; seenGold = ch.gold; seenHp = ch.hpPotions; seenMp = ch.mpPotions; seenBag = ch.inventory.Count;
                seenParts = (int[])ch.parts.Clone();
                pickups.Clear();
            }
            if (ch.gold > seenGold) Note("+" + (ch.gold - seenGold) + " Embermarks", new Color(1f, .85f, .35f));
            if (ch.hpPotions > seenHp) Note("+" + (ch.hpPotions - seenHp) + " HP potion", new Color(1f, .5f, .55f));
            if (ch.mpPotions > seenMp) Note("+" + (ch.mpPotions - seenMp) + " MP potion", new Color(.55f, .7f, 1f));
            for (int i = 0; i < ch.parts.Length; i++)
                if (ch.parts[i] > seenParts[i]) Note("+" + (ch.parts[i] - seenParts[i]) + " " + Parts.All[i].name, Parts.All[i].color);
            if (ch.inventory.Count > seenBag)
            {
                var item = ch.inventory[ch.inventory.Count - 1];
                Note(item.DisplayName + "  (+" + item.Attack + " ATK / +" + item.Defense + " DEF / +" + item.Health + " HP)", Loot.RarityColor(item.rarity));
            }
            seenGold = ch.gold; seenHp = ch.hpPotions; seenMp = ch.mpPotions; seenBag = ch.inventory.Count;
            for (int i = 0; i < ch.parts.Length; i++) seenParts[i] = ch.parts[i];

            float y = Screen.height - 22;
            for (int i = pickups.Count - 1; i >= 0; i--)
            {
                var n = pickups[i];
                if (Event.current.type == EventType.Repaint) n.age += Time.deltaTime;
                if (n.age > PickupShow) { pickups.RemoveAt(i); continue; }
                if (canvasActive) continue;
                float alpha = n.age > PickupShow - 1f ? PickupShow - n.age : 1f;
                var size = smallStyle.CalcSize(new GUIContent(n.text));
                Fill(new Rect(12, y - 22, size.x + 16, 22), new Color(.04f, .05f, .09f, .6f * alpha));
                smallStyle.normal.textColor = new Color(n.color.r, n.color.g, n.color.b, alpha);
                GUI.Label(new Rect(20, y - 22, size.x + 10, 22), n.text, smallStyle);
                y -= 24;
            }
            smallStyle.normal.textColor = new Color(.82f, .85f, .9f);
        }

        void Note(string text, Color color)
        {
            // The same thing twice in a row stacks ("+12 Embermarks" then "+5 Embermarks" becomes two lines; fine).
            pickups.Add(new Pickup { text = text, color = color });
            if (onPickup != null) onPickup(text, color);
            if (pickups.Count > 6) pickups.RemoveAt(0);
        }

        // Damage and healing numbers: pop in, drift up, fade out (WorldView makes them from health changes).
        void DrawNumbers()
        {
            var cam = Camera.main;
            if (cam == null || view == null) return;
            foreach (var n in view.numbers)
            {
                float t = n.age / n.life;
                Vector2 at;
                if (!ToScreen(cam, n.x, n.y + t * 1.1f, out at)) continue;
                float pop = t < .12f ? 1f + (.12f - t) * 4f : 1f;
                float alpha = t > .6f ? 1f - (t - .6f) / .4f : 1f;
                var style = n.big ? bigNumberStyle : numberStyle;
                var rect = new Rect(at.x - 60, at.y - 16, 120, 32);
                Matrix4x4 old = GUI.matrix;
                GUIUtility.ScaleAroundPivot(new Vector2(pop, pop), at);
                style.normal.textColor = new Color(0, 0, 0, .8f * alpha);
                GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), n.text, style);
                style.normal.textColor = new Color(n.color.r, n.color.g, n.color.b, alpha);
                GUI.Label(rect, n.text, style);
                GUI.matrix = old;
            }
        }

        // A boss on this map gets a big bar across the top of the screen instead of the little one over its head.
        void DrawBossBar(PlayerState me)
        {
            foreach (var m in world.maps[me.map].monsters)
            {
                var d = m.Def;
                if (!d.boss) continue;
                float w = Mathf.Min(Screen.width - 80, 720), x = (Screen.width - w) / 2f, y = canvasActive ? 90 : 62;
                Fill(new Rect(x - 4, y - 4, w + 8, 36), new Color(.04f, .05f, .09f, .8f));
                Fill(new Rect(x, y, w, 12), new Color(.2f, .05f, .05f));
                float ratio = Mathf.Clamp01(m.hp / Mathf.Max(1f, m.maxHp));
                Fill(new Rect(x, y, w * ratio, 12), ratio < Tuning.BossEnrageHp ? new Color(1f, .45f, .15f) : new Color(.85f, .15f, .2f));
                GUI.Label(new Rect(x, y + 11, w, 20), d.name.ToUpperInvariant() + "   ·   LV " + d.level + (ratio < Tuning.BossEnrageHp ? "   ·   ENRAGED" : ""), bossName);
                break;
            }
        }

        // A health bar and level over each monster that has been hurt (classic MapleStory shows its bar once you hit it).
        void DrawMonsterBars(PlayerState me)
        {
            var cam = Camera.main;
            if (cam == null) return;
            foreach (var m in world.maps[me.map].monsters)
            {
                var d = m.Def;
                if (d.boss || m.hp >= m.maxHp - .01f) continue;
                Vector2 at;
                if (!ToScreen(cam, m.x, m.y + d.hover + d.height + .35f, out at)) continue;
                float ratio = Mathf.Clamp01(m.hp / Mathf.Max(1f, m.maxHp));
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
            Rect r = new Rect((Screen.width - 320) / 2f, (Screen.height - 304) / 2f, 320, 304);
            GUI.Box(r, "MENU   [ESC] Close");
            GUI.Label(new Rect(r.x + 20, r.y + 30, 280, 24), "Your hero saves automatically.", smallStyle);
            if (GUI.Button(new Rect(r.x + 30, r.y + 66, 260, 44), "RESUME", menuButton)) menuOpen = false;
            if (GUI.Button(new Rect(r.x + 30, r.y + 120, 260, 44), "CONTROLS", menuButton)) { menuOpen = false; controlsOpen = true; capturingAction = -1; }
            if (GUI.Button(new Rect(r.x + 30, r.y + 174, 260, 44), online ? "SAVE & LEAVE GAME" : "SAVE & YOUR HEROES", menuButton)) { menuOpen = false; if (onCharacterSelect != null) onCharacterSelect(); }
            if (GUI.Button(new Rect(r.x + 30, r.y + 228, 260, 44), "SAVE & QUIT", menuButton)) { menuOpen = false; if (onQuit != null) onQuit(); }
        }

        // Controls: two keys per action; click one and press the key you want (Esc keeps the old key, Backspace clears).
        void DrawControls()
        {
            int rows = Keys.Count;
            float h = 96 + rows * 27 + 50;
            Rect r = new Rect((Screen.width - 620) / 2f, Mathf.Max(20, (Screen.height - h) / 2f), 620, h);
            GUI.Box(r, "CONTROLS   [ESC] Close");
            GUI.Label(new Rect(r.x + 20, r.y + 28, 580, 44), Capturing ? "Press the key you want (Esc: keep the old one, Backspace: clear)."
                                                               : "Click a key to change it. Either key works for an action. Saved on this PC.", smallStyle);
            if (Capturing)
            {
                var e = Event.current;
                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { capturingAction = -1; e.Use(); }
                else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Backspace) { Keys.Set((GameAction)capturingAction, capturingSlot, KeyCode.None); capturingAction = -1; e.Use(); }
                else
                {
                    var key = Keys.AnyKeyDown();
                    if (key != KeyCode.None && key != KeyCode.Backspace) { Keys.Set((GameAction)capturingAction, capturingSlot, key); capturingAction = -1; }
                }
            }
            for (int a = 0; a < rows; a++)
            {
                float y = r.y + 78 + a * 27;
                if (a % 2 == 0) Fill(new Rect(r.x + 12, y - 1, 596, 26), new Color(1, 1, 1, .05f));
                GUI.Label(new Rect(r.x + 20, y + 2, 300, 22), Keys.Describe((GameAction)a), smallStyle);
                for (int s = 0; s < Keys.Slots; s++)
                {
                    bool mine = capturingAction == a && capturingSlot == s;
                    string label = mine ? "..." : Keys.KeyName(Keys.Get((GameAction)a, s));
                    GUI.enabled = !Capturing || mine;
                    if (GUI.Button(new Rect(r.x + 330 + s * 135, y, 125, 24), label)) { capturingAction = a; capturingSlot = s; }
                    GUI.enabled = true;
                }
            }
            GUI.enabled = !Capturing;
            if (GUI.Button(new Rect(r.x + 20, r.yMax - 40, 180, 30), "RESET TO DEFAULTS")) Keys.ResetAll();
            if (GUI.Button(new Rect(r.xMax - 140, r.yMax - 40, 120, 30), "DONE")) { controlsOpen = false; capturingAction = -1; }
            GUI.enabled = true;
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
            GUI.Label(new Rect(23, 152, 220, 20), "EMBERMARKS  " + ch.gold + "     KILLS  " + ch.kills, detailText);
            GUI.Label(new Rect(23, 175, 338, 19), "HP POTIONS [" + Keys.Name(GameAction.HpPotion) + "]  " + ch.hpPotions + "       MP POTIONS [" + Keys.Name(GameAction.MpPotion) + "]  " + ch.mpPotions, detailText);
            GUI.matrix = oldMatrix;
        }

        static Rect PanelRect() { return new Rect(Mathf.Max(10, (Screen.width - 630) / 2f), Mathf.Max(120, (Screen.height - 480) / 2f), 630, 480); }

        void DrawInventory(PlayerState me)
        {
            var ch = me.ch;
            Rect r = PanelRect();
            GUI.Box(r, "INVENTORY   [" + Keys.Name(GameAction.Inventory) + "] Close");
            GUI.Label(new Rect(r.x + 18, r.y + 27, r.width - 36, 20),
                      "Embermarks " + ch.gold + "   |   Gear bonuses: ATK +" + ch.BonusAttack + "  DEF +" + ch.BonusDefense + "  HP +" + ch.BonusHealth, smallStyle);
            int partKinds = 0;
            foreach (int count in ch.parts) if (count > 0) partKinds++;
            string[] tabs = { "EQUIPS  " + ch.inventory.Count + "/" + Tuning.InventoryCapacity,
                              "USEABLE ITEMS", "MONSTER PARTS  " + partKinds };
            float tabWidth = (r.width - 48) / 3;
            for (int i = 0; i < tabs.Length; i++)
            {
                Rect tab = new Rect(r.x + 18 + i * (tabWidth + 6), r.y + 55, tabWidth, 30);
                bool active = (int)inventorySection == i;
                if (GUI.Button(tab, tabs[i], active ? activeInventoryTabText : inventoryTabText)) inventorySection = (InventorySection)i;
                if (active) Fill(new Rect(tab.x + 8, tab.yMax - 3, tab.width - 16, 2), HudTheme.Copper);
            }
            switch (inventorySection)
            {
                case InventorySection.Equips: DrawEquipsTab(me, r); break;
                case InventorySection.Useables: DrawUseablesTab(me, r); break;
                case InventorySection.MonsterParts: DrawPartsTab(me, r); break;
            }
        }

        void DrawEquipsTab(PlayerState me, Rect r)
        {
            var ch = me.ch;
            for (int i = 0; i < 4; i++)
            {
                Item item = ch.equipped[i];
                GUI.Label(new Rect(r.x + 18 + (i % 2) * 300, r.y + 96 + (i / 2) * 25, 290, 23),
                          ((Slot)i) + ": " + (item == null ? "Empty" : item.DisplayName), smallStyle);
            }
            for (int i = 0; i < Tuning.InventoryCapacity; i++)
            {
                int col = i % 5, row = i / 5;
                Rect cell = new Rect(r.x + 18 + col * 120, r.y + 161 + row * 65, 115, 60);
                Item item = i < ch.inventory.Count ? ch.inventory[i] : null;
                if (item == null) { GUI.Box(cell, "—"); continue; }
                Color previous = GUI.color;
                GUI.color = Loot.RarityColor(item.rarity);
                bool clicked = GUI.Button(cell, item.DisplayName + "\n+" + item.Attack + " ATK / +" + item.Defense + " DEF\n+" + item.Health + " HP");
                GUI.color = previous;
                if (clicked) { commands.Equip(i); break; }
            }
            GUI.Label(new Rect(r.x + 18, r.y + 433, r.width - 36, 28), "Click an item to equip it. Potions and monster parts have their own tabs.", partNameText);
        }

        void DrawUseablesTab(PlayerState me, Rect r)
        {
            DrawPotionCard(me, r, true, r.x + 18);
            DrawPotionCard(me, r, false, r.x + 320);
            GUI.Label(new Rect(r.x + 18, r.y + 343, r.width - 36, 44),
                      "Potions restore a share of your maximum health or mana.\nSupplies can be restocked at a merchant.", smallStyle);
        }

        void DrawPotionCard(PlayerState me, Rect r, bool health, float x)
        {
            int count = health ? me.ch.hpPotions : me.ch.mpPotions;
            int current = health ? me.hp : me.mp, maximum = health ? me.MaxHp : me.MaxMp;
            int amount = health ? Tuning.PotionHeal(Tuning.HpPotionHeal, Tuning.HpPotionShare, maximum)
                                : Tuning.PotionHeal(Tuning.MpPotionHeal, Tuning.MpPotionShare, maximum);
            string stat = health ? "HP" : "MP";
            Rect card = new Rect(x, r.y + 106, 290, 214);
            GUI.Box(card, (health ? "HEALTH POTION" : "MANA POTION") + "  x" + count);
            var icon = Sprites.Art(health ? "Art/Items/hp_potion" : "Art/Items/mp_potion");
            if (icon != null) GUI.DrawTexture(new Rect(card.xMax - 92, card.y + 36, 76, 76), icon.texture, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(card.x + 16, card.y + 42, card.width - (icon != null ? 116 : 32), 84),
                      "Restore " + amount + " " + stat + ".\nCurrent " + stat + ": " + current + " / " + maximum + "\nHotkey: " + Keys.Name(health ? GameAction.HpPotion : GameAction.MpPotion), smallStyle);
            bool previous = GUI.enabled;
            GUI.enabled = previous && onUseItem != null && !me.dead && count > 0 && current < maximum;
            if (GUI.Button(new Rect(card.x + 16, card.y + 156, card.width - 32, 36), "USE " + stat + " POTION")) UseInventoryPotion(me, health);
            GUI.enabled = previous;
        }

        // Inventory uses the same one-shot input route as the hotbar; the host still validates each potion.
        public bool UseInventoryPotion(PlayerState me, bool health)
        {
            if (me == null || me.dead || onUseItem == null) return false;
            int count = health ? me.ch.hpPotions : me.ch.mpPotions;
            if (count <= 0 || (health ? me.hp >= me.MaxHp : me.mp >= me.MaxMp)) return false;
            onUseItem(health ? GameAction.HpPotion : GameAction.MpPotion);
            return true;
        }

        void DrawPartsTab(PlayerState me, Rect r)
        {
            int rows = 0;
            foreach (int count in me.ch.parts) if (count > 0) rows++;
            GUI.Label(new Rect(r.x + 22, r.y + 99, 294, 22), "MATERIAL", partNameText);
            GUI.Label(new Rect(r.x + 330, r.y + 99, 90, 22), "QUANTITY", partNameText);
            GUI.Label(new Rect(r.x + 440, r.y + 99, 138, 22), "SELL VALUE", partNameText);
            Rect area = new Rect(r.x + 18, r.y + 127, r.width - 36, 292);
            inventoryPartsScroll = GUI.BeginScrollView(area, inventoryPartsScroll, new Rect(0, 0, area.width - 20, Mathf.Max(area.height, rows * 38)));
            int shown = 0;
            for (int i = 0; i < me.ch.parts.Length; i++)
            {
                int count = me.ch.parts[i];
                if (count <= 0) continue;
                float y = shown++ * 38;
                if (shown % 2 != 0) Fill(new Rect(0, y, area.width - 20, 36), new Color(1, 1, 1, .04f));
                var part = Parts.All[i];
                var icon = Sprites.PartIcon(i);
                if (icon != null) GUI.DrawTexture(new Rect(4, y + 2, 32, 32), icon.texture, ScaleMode.ScaleToFit, true);
                GUI.Label(new Rect(icon != null ? 42 : 4, y + 4, 294, 28), part.name, partNameText);
                GUI.Label(new Rect(312, y + 4, 90, 28), "x" + count, partNameText);
                GUI.Label(new Rect(422, y + 4, 132, 28), (part.price * count) + " EM", partNameText);
            }
            if (shown == 0) GUI.Label(new Rect(4, 16, area.width - 20, 30), "No monster parts yet. Collect materials from defeated monsters.", smallStyle);
            GUI.EndScrollView();
            GUI.Label(new Rect(r.x + 18, r.y + 433, r.width - 36, 28), "Keep materials for the smithy, or sell them at a merchant.", partNameText);
        }

        void DrawShop(PlayerState me, MapDef def)
        {
            var ch = me.ch;
            Rect r = PanelRect();
            GUI.Box(r, def.name.ToUpperInvariant() + " GENERAL STORE — [" + Keys.Name(GameAction.Interact) + "] Close");
            GUI.Label(new Rect(r.x + 20, r.y + 32, 550, 28), "Embermarks: " + ch.gold + "   |   Sell unwanted gear for half its value", bodyStyle);
            if (GUI.Button(new Rect(r.x + 20, r.y + 70, 270, 42), "Buy HP Potion — " + Tuning.HpPotionPrice + " EM")) commands.BuyPotion(true);
            if (GUI.Button(new Rect(r.x + 320, r.y + 70, 270, 42), "Buy MP Potion — " + Tuning.MpPotionPrice + " EM")) commands.BuyPotion(false);
            GUI.Label(new Rect(r.x + 20, r.y + 123, 560, 25), "Merchant equipment (reliable Common gear)", smallStyle);
            for (int i = 0; i < 4; i++)
            {
                Slot slot = (Slot)i;
                int cost = Loot.ShopGearCost(slot, ch.level);
                if (GUI.Button(new Rect(r.x + 20 + (i % 2) * 300, r.y + 154 + (i / 2) * 49, 270, 40), "Buy Common " + Loot.SlotName(slot, me.Hero) + " — " + cost + " EM"))
                    commands.BuyGear(slot);
            }
            GUI.Label(new Rect(r.x + 20, r.y + 255, 280, 25), "Sell gear", smallStyle);
            for (int i = 0; i < ch.inventory.Count && i < 7; i++)
            {
                Item item = ch.inventory[i];
                if (GUI.Button(new Rect(r.x + 20, r.y + 280 + i * 28, 280, 26), "Sell " + item.DisplayName + "  +" + Loot.SellPrice(item) + " EM"))
                { commands.Sell(i); break; }
            }
            if (ch.inventory.Count > 7) GUI.Label(new Rect(r.x + 20, r.y + 280 + 7 * 28, 280, 22), "...and " + (ch.inventory.Count - 7) + " more (sell these first)", smallStyle);
            GUI.Label(new Rect(r.x + 320, r.y + 255, 280, 25), "Sell monster parts", smallStyle);
            int shown = 0;
            for (int i = 0; i < ch.parts.Length && shown < 7; i++)
            {
                if (ch.parts[i] <= 0) continue;
                float y = r.y + 280 + shown * 28;
                var part = Parts.All[i];
                var icon = Sprites.PartIcon(i);
                if (icon != null) GUI.DrawTexture(new Rect(r.x + 320, y + 1, 24, 24), icon.texture, ScaleMode.ScaleToFit, true);
                GUI.Label(new Rect(r.x + (icon != null ? 348 : 320), y, icon != null ? 100 : 125, 26), part.name + " x" + ch.parts[i], partNameText);
                if (GUI.Button(new Rect(r.x + 452, y, 66, 26), "1  +" + part.price + " EM")) { commands.SellPart(i, 1); break; }
                if (GUI.Button(new Rect(r.x + 522, y, 70, 26), "All +" + part.price * ch.parts[i] + " EM")) { commands.SellPart(i, 0); break; }
                shown++;
            }
            if (shown == 0) GUI.Label(new Rect(r.x + 320, r.y + 283, 280, 22), "No parts to sell yet.", smallStyle);
        }
    
        // Waystone: travel to any town you have found (walking into a town finds it).
        void DrawWaystone(PlayerState me)
        {
            Rect r = new Rect((Screen.width - 420) / 2f, Mathf.Max(120, (Screen.height - 410) / 2f), 420, 410);
            GUI.Box(r, "WAYSTONE — [" + Keys.Name(GameAction.Interact) + "] Close");
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
            GUI.Label(new Rect(frame.x + 8, y0 + h + 6, frame.width - 16, 18), Keys.Name(GameAction.WorldMap) + "  WORLD MAP   ·   " + Keys.Name(GameAction.Minimap) + "  HIDE", minimapHint);
        }

        // M: the world as far as you know it. Places you have been, "???" for the places next door, the roads
        // between them, where you are and where the rest of your party is. Hover a place to see its monsters.
        void DrawWorldMap(PlayerState me)
        {
            var ch = me.ch;
            var art = Sprites.Art(WorldMapArt);
            // With the painted atlas: the picture at its own 3:2 shape, never stretched, places spread over all of it,
            // and the text (close hint, footer) in the dark frame around it so nothing covers the paint. Without a
            // picture: the parchment panel with a title, and the places over an inset area.
            const float frameTop = 30, frameBottom = 72;
            float w, h;
            if (art != null)
            {
                float aspect = art.texture.width / (float)art.texture.height;
                h = Mathf.Min(Screen.height - 60 - frameTop - frameBottom, 900); w = h * aspect;
                if (w > Screen.width - 40) { w = Screen.width - 40; h = w / aspect; }
            }
            else { w = Mathf.Min(Screen.width - 40, 1000); h = Mathf.Min(Screen.height - 150, 580); }
            Rect r = new Rect((Screen.width - w) / 2f, Mathf.Max(frameTop + 10, (Screen.height - h) / 2f), w, h);
            float scale = Mathf.Clamp(w / 1000f, .8f, 1.8f);        // markers and names grow with the window
            atlasName.fontSize = Mathf.RoundToInt(10 * scale);
            atlasSub.fontSize = Mathf.RoundToInt(8.5f * scale);
            Rect area, footerRect;
            if (art != null)
            {
                Fill(new Rect(r.x - 6, r.y - frameTop, r.width + 12, r.height + frameTop + frameBottom), new Color(.23f, .15f, .09f, .97f));
                GUI.DrawTexture(r, art.texture, ScaleMode.StretchToFill);
                area = r;
                GUI.Label(new Rect(r.x, r.y - frameTop + 6, r.width - 8, 20), "[" + Keys.Name(GameAction.WorldMap) + "] CLOSE", atlasCornerLight);
                footerRect = new Rect(r.x + 10, r.yMax + 8, r.width - 20, frameBottom - 8);
            }
            else
            {
                Fill(new Rect(r.x - 5, r.y - 5, r.width + 10, r.height + 10), new Color(.23f, .15f, .09f, .97f));
                Fill(r, new Color(.91f, .84f, .68f, .98f));
                Outline(new Rect(r.x + 8, r.y + 8, r.width - 16, r.height - 16), new Color(.55f, .4f, .25f, .6f));
                area = new Rect(r.x + 70, r.y + 72, r.width - 140, r.height - 160);
                GUI.Label(new Rect(r.x, r.y + 14, r.width, 34), "WORLD MAP", atlasTitle);
                GUI.Label(new Rect(r.x + 20, r.y + 20, r.width - 44, 20), "[" + Keys.Name(GameAction.WorldMap) + "] CLOSE", atlasCorner);
                footerRect = new Rect(r.x + 26, r.yMax - 70, r.width - 52, 66);
            }

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
                AtlasMarkerArt.Draw(at, scale, seen, town, def.hasBoss, here, DangerColor(def, ch.level),
                    (mouse - at).sqrMagnitude < 16f * 16f * scale * scale);
                AtlasText(area, at.x, at.y + 17 * scale, seen ? def.name : "???", atlasName);
                AtlasText(area, at.x, at.y + 29 * scale, !seen ? "unexplored" : town ? "Town · waystone" : def.LevelText, atlasSub);
                if ((mouse - at).sqrMagnitude < 16f * 16f * scale * scale) hovered = i;
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

            var small = art != null ? atlasSmallLight : atlasSmall;
            GUI.Label(new Rect(footerRect.x, footerRect.y, footerRect.width, 20), "Discovered " + found + " of " + Maps.All.Length + " places", small);
            GUI.Label(new Rect(footerRect.x, footerRect.y + 20, footerRect.width, 20),
                      "Castle: town   ·   Leaf: hunting ground   ·   Skull: boss   ·   Compass: unexplored   ·   Small gem: level difficulty", small);
            if (party.Length > 0) GUI.Label(new Rect(footerRect.x, footerRect.y + 40, footerRect.width, 20), "PARTY   " + party, small);
            if (hovered >= 0) AtlasTooltip(me, hovered, mouse);
        }

        // A world map label centred on (x, y), on a patch of parchment so roads don't run through the letters;
        // kept inside the map area so places near an edge don't spill off the picture.
        static void AtlasText(Rect area, float x, float y, string text, GUIStyle style)
        {
            var size = style.CalcSize(new GUIContent(text));
            x = Mathf.Clamp(x, area.x + size.x / 2f + 6f, area.xMax - size.x / 2f - 6f);
            y = Mathf.Clamp(y, area.y + size.y / 2f + 2f, area.yMax - size.y / 2f - 2f);
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
                    string entry = (m.boss ? "BOSS " : "") + m.name + " (Lv " + m.level + ")";
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

        // "PARTS  Shrew Tuft 12 · Boar Tusk 3" (only the ones you carry).
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
            GUI.Box(r, def.name.ToUpperInvariant() + " SMITHY — [" + Keys.Name(GameAction.Interact) + "] Close");
            GUI.Label(new Rect(r.x + 18, r.y + 26, w - 36, 22), "Upgrades always succeed, up to +" + Tuning.MaxUpgrade + ". Embermarks: " + ch.gold, smallStyle);
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
            price += "  +  " + cost.gold + " EM";
            GUI.Label(new Rect(340, y + 4, width - 350, 22), price, smallStyle);
            GUI.enabled = Smithing.CanPay(ch, cost);
            if (GUI.Button(new Rect(width - 160, y + 26, 150, 24), "UPGRADE TO +" + (item.upgrade + 1))) commands.Upgrade(target);
            GUI.enabled = true;
        }

    }
}


