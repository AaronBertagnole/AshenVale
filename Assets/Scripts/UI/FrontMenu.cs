using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshenVale
{
    // Title, your saved heroes, how to play (solo / host / join), new-hero creation and the controls screen
    // (IMGUI, drawn in a fixed 900 x 610 reference space).
    public sealed class FrontMenu
    {
        public enum Screen { Title, Characters, Select, Tutorial, Mode }

        public Screen screen = Screen.Title;
        public int pendingHero;
        public string heroName = "";
        List<CharacterData> saved = new List<CharacterData>();
        int selected;
        string pendingDelete;           // id of the hero whose Delete was clicked once (click again to confirm)
        Vector2 scroll;
        bool nameFocused, textFocused;

        // How-to-play screen
        CharacterData modeHero;
        public string joinCode = "";
        public string joinAddress = "127.0.0.1";
        public string status = "";      // connection progress or the last error, shown on the hero screens
        public bool busy;               // connecting: buttons are disabled

        readonly MusicPlayer music;
        readonly Action<int, string> createHero;
        readonly Action<CharacterData> playSolo, deleteSaved;
        readonly Action<CharacterData, bool> hostGame;            // hero, online
        readonly Action<CharacterData, bool, string> joinGame;    // hero, online, join code or IP address
        GUIStyle heading, subtitle, body, small, button, card, selectedCard, rowName, rowDetail, nameField, statusText, sectionText;

        static readonly string[] NameIdeas = { "Rowan", "Ember", "Wren", "Kestrel", "Bram", "Sable", "Lark", "Fen", "Orin", "Thistle", "Corvin", "Isolde" };

        public FrontMenu(MusicPlayer music, Action<int, string> createHero, Action<CharacterData> playSolo,
                         Action<CharacterData, bool> hostGame, Action<CharacterData, bool, string> joinGame, Action<CharacterData> deleteSaved)
        {
            this.music = music;
            this.createHero = createHero;
            this.playSolo = playSolo;
            this.hostGame = hostGame;
            this.joinGame = joinGame;
            this.deleteSaved = deleteSaved;
        }

        void OpenMode(CharacterData hero)
        {
            modeHero = hero;
            status = "";
            screen = Screen.Mode;
        }

        public void SetSaved(List<CharacterData> list)
        {
            saved = list ?? new List<CharacterData>();
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, saved.Count - 1));
            pendingDelete = null;
        }

        void OpenHeroes() { screen = saved.Count > 0 ? Screen.Characters : Screen.Select; if (screen == Screen.Select) PrepareNewHero(); }

        void PrepareNewHero()
        {
            screen = Screen.Select;
            if (string.IsNullOrEmpty(heroName)) heroName = NameIdeas[UnityEngine.Random.Range(0, NameIdeas.Length)];
        }

        void Create()
        {
            string name = (heroName ?? "").Trim();
            if (name.Length == 0) name = Heroes.All[pendingHero].name;
            heroName = "";
            createHero(pendingHero, name);
        }

        public void HandleKeys()
        {
            switch (screen)
            {
                case Screen.Title:
                    if (Input.GetKeyDown(KeyCode.M)) music.ToggleMute();
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) OpenHeroes();
                    break;
                case Screen.Characters:
                    int last = Mathf.Max(0, saved.Count - 1);
                    if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) { selected = Mathf.Clamp(selected - 1, 0, last); pendingDelete = null; }
                    if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) { selected = Mathf.Clamp(selected + 1, 0, last); pendingDelete = null; }
                    if (Input.GetKeyDown(KeyCode.Return) && selected >= 0 && selected < saved.Count) OpenMode(saved[selected]);
                    else if (Input.GetKeyDown(KeyCode.N)) PrepareNewHero();
                    else if (Input.GetKeyDown(KeyCode.Escape)) screen = Screen.Title;
                    break;
                case Screen.Select:
                    if (!nameFocused && Input.GetKeyDown(KeyCode.Alpha1)) pendingHero = 0;
                    if (!nameFocused && Input.GetKeyDown(KeyCode.Alpha2)) pendingHero = 1;
                    if (Input.GetKeyDown(KeyCode.Return)) screen = Screen.Tutorial;
                    else if (Input.GetKeyDown(KeyCode.Escape)) screen = saved.Count > 0 ? Screen.Characters : Screen.Title;
                    break;
                case Screen.Tutorial:
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) Create();
                    else if (Input.GetKeyDown(KeyCode.Escape)) screen = Screen.Select;
                    break;
                case Screen.Mode:
                    if (!busy && !textFocused && Input.GetKeyDown(KeyCode.Escape)) screen = Screen.Characters;
                    break;
            }
        }

        void InitStyles()
        {
            if (heading != null) return;
            heading = new GUIStyle(GUI.skin.label) { fontSize = 39, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            heading.normal.textColor = new Color(1f, .84f, .58f);
            subtitle = new GUIStyle(GUI.skin.label) { fontSize = 19, alignment = TextAnchor.MiddleCenter };
            subtitle.normal.textColor = new Color(.78f, .85f, .91f);
            body = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            body.normal.textColor = new Color(.93f, .94f, .96f);
            small = new GUIStyle(body) { fontSize = 14 };
            button = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            card = new GUIStyle(GUI.skin.box) { fontSize = 17, alignment = TextAnchor.UpperCenter, wordWrap = true };
            selectedCard = new GUIStyle(card);
            selectedCard.normal.textColor = new Color(1f, .84f, .58f);
            rowName = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            rowName.normal.textColor = new Color(1f, .9f, .72f);
            rowDetail = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
            rowDetail.normal.textColor = new Color(.82f, .86f, .92f);
            statusText = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            statusText.normal.textColor = new Color(1f, .8f, .55f);
            sectionText = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            sectionText.normal.textColor = new Color(.62f, .9f, 1f);
            nameField = new GUIStyle(GUI.skin.textField) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        }

        public void Draw()
        {
            InitStyles();
            // Reference-space layout scales as one unit, keeping labels and buttons aligned.
            float scale = Mathf.Max(.1f, Mathf.Min(UnityEngine.Screen.width / 900f, UnityEngine.Screen.height / 610f));
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float sw = UnityEngine.Screen.width / scale, sh = UnityEngine.Screen.height / scale;
            Rect panel = new Rect((sw - 840) / 2, (sh - 550) / 2, 840, 550);
            GUI.Box(panel, GUIContent.none);
            float left = panel.x, top = panel.y;
            nameFocused = false;
            textFocused = false;

            switch (screen)
            {
                case Screen.Title: DrawTitle(left, top); break;
                case Screen.Characters: DrawCharacters(left, top); break;
                case Screen.Select: DrawSelect(left, top); break;
                case Screen.Mode: DrawMode(left, top); break;
                default: DrawTutorial(left, top); break;
            }
            GUI.matrix = oldMatrix;
        }

        void DrawTitle(float left, float top)
        {
            GUI.Label(new Rect(left + 35, top + 40, 770, 65), "ASHEN VALE", heading);
            GUI.Label(new Rect(left + 35, top + 110, 770, 35), "Where the Maps End", subtitle);
            GUI.Label(new Rect(left + 95, top + 174, 650, 72), "Beyond the last lantern, the wilds remember.\nA new journey begins in Emberhaven.", body);
            if (GUI.Button(new Rect(left + 295, top + 278, 250, 49), "START ADVENTURE", button)) OpenHeroes();
            GUI.Label(new Rect(left + 265, top + 354, 310, 27), "MUSIC VOLUME", subtitle);
            music.SetVolume(GUI.HorizontalSlider(new Rect(left + 287, top + 400, 215, 20), music.Volume, 0f, 1f));
            GUI.Label(new Rect(left + 510, top + 388, 65, 30), Mathf.RoundToInt(music.Volume * 100) + "%", small);
            if (GUI.Button(new Rect(left + 322, top + 432, 196, 33), music.Muted ? "UNMUTE  [M]" : "MUTE  [M]")) music.ToggleMute();
            GUI.Label(new Rect(left + 40, top + 500, 760, 24), "A whimsical journey into the unknown", small);
        }

        void DrawCharacters(float left, float top)
        {
            GUI.Label(new Rect(left + 40, top + 20, 760, 54), "YOUR HEROES", heading);
            GUI.Label(new Rect(left + 40, top + 76, 760, 28), "Continue a journey, or begin a new one.", subtitle);

            const float rowHeight = 66f;
            Rect area = new Rect(left + 60, top + 115, 720, 300);
            Rect content = new Rect(0, 0, 700, Mathf.Max(area.height, saved.Count * rowHeight));
            scroll = GUI.BeginScrollView(area, scroll, content);
            for (int i = 0; i < saved.Count; i++)
            {
                var d = saved[i];
                var hero = Heroes.All[Mathf.Clamp(d.hero, 0, Heroes.All.Length - 1)];
                Rect row = new Rect(0, i * rowHeight, 700, rowHeight - 6);
                Color previous = GUI.color;
                GUI.color = i == selected ? new Color(1f, .85f, .57f) : new Color(.72f, .78f, .87f);
                GUI.Box(row, GUIContent.none, i == selected ? selectedCard : card);
                GUI.color = previous;

                Sprite[] idle = Sprites.Hero(hero.folder + "/idle");
                if (idle.Length > 0 && idle[0] != null) DrawSprite(new Rect(row.x + 8, row.y + 4, 52, 52), idle[0]);
                string where = d.map >= 0 && d.map < Maps.All.Length ? Maps.All[d.map].name : "?";
                GUI.Label(new Rect(row.x + 70, row.y + 4, 360, 28), d.name, rowName);
                GUI.Label(new Rect(row.x + 70, row.y + 30, 400, 24),
                          hero.name + "  ·  Level " + d.level + "  ·  " + where + "  ·  " + SaveCodec.FormatPlayTime(d.playSeconds), rowDetail);

                if (GUI.Button(new Rect(row.x + 480, row.y + 12, 100, 36), "PLAY", button)) { OpenMode(d); break; }
                bool confirming = pendingDelete == d.id;
                if (GUI.Button(new Rect(row.x + 590, row.y + 12, 100, 36), confirming ? "SURE?" : "DELETE"))
                {
                    if (confirming) { pendingDelete = null; deleteSaved(d); break; }
                    pendingDelete = d.id;
                    selected = i;
                }
                // Clicking the rest of the row selects it.
                if (Event.current.type == EventType.MouseDown && row.Contains(Event.current.mousePosition) && Event.current.mousePosition.x < row.x + 470)
                { selected = i; pendingDelete = null; }
            }
            GUI.EndScrollView();
            if (saved.Count == 0) GUI.Label(new Rect(left + 60, top + 230, 720, 40), "No heroes yet. Create your first one.", body);
            if (!string.IsNullOrEmpty(status)) GUI.Label(new Rect(left + 40, top + 410, 760, 20), status, statusText);

            if (GUI.Button(new Rect(left + 290, top + 432, 260, 47), "NEW HERO  [N]", button)) PrepareNewHero();
            if (GUI.Button(new Rect(left + 342, top + 492, 156, 32), "BACK  [ESC]")) screen = Screen.Title;
        }

        void DrawMode(float left, float top)
        {
            if (modeHero == null) { screen = Screen.Characters; return; }
            var hero = Heroes.All[Mathf.Clamp(modeHero.hero, 0, Heroes.All.Length - 1)];
            GUI.Label(new Rect(left + 40, top + 18, 760, 50), "HOW DO YOU WANT TO PLAY?", heading);
            GUI.Label(new Rect(left + 40, top + 70, 760, 26), modeHero.name + "  ·  " + hero.name + "  ·  Level " + modeHero.level, subtitle);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = !busy;
            float x = left + 150, w = 540;
            if (GUI.Button(new Rect(x, top + 108, w, 42), "PLAY SOLO", button)) playSolo(modeHero);

            GUI.Label(new Rect(x, top + 160, w, 22), "ONLINE  —  friends anywhere join with a code (needs Unity Cloud set up, see README)", sectionText);
            if (GUI.Button(new Rect(x, top + 186, w, 40), "HOST ONLINE GAME", button)) hostGame(modeHero, true);
            GUI.SetNextControlName("JoinCode");
            joinCode = GUI.TextField(new Rect(x, top + 234, 200, 36), joinCode ?? "", 12, nameField);
            if (GUI.Button(new Rect(x + 210, top + 234, w - 210, 36), "JOIN WITH CODE", button) && (joinCode ?? "").Trim().Length > 0)
                joinGame(modeHero, true, joinCode);

            GUI.Label(new Rect(x, top + 284, w, 22), "THIS PC / LOCAL NETWORK  —  for testing with Multiplayer Play Mode, or LAN", sectionText);
            if (GUI.Button(new Rect(x, top + 310, w, 40), "HOST ON THIS PC", button)) hostGame(modeHero, false);
            GUI.SetNextControlName("JoinAddress");
            joinAddress = GUI.TextField(new Rect(x, top + 358, 200, 36), joinAddress ?? "", 40, nameField);
            if (GUI.Button(new Rect(x + 210, top + 358, w - 210, 36), "JOIN BY IP ADDRESS", button)) joinGame(modeHero, false, joinAddress);
            string focused = GUI.GetNameOfFocusedControl();
            textFocused = focused == "JoinCode" || focused == "JoinAddress";

            if (!string.IsNullOrEmpty(status)) GUI.Label(new Rect(left + 40, top + 404, 760, 44), status, statusText);
            if (GUI.Button(new Rect(left + 342, top + 470, 156, 32), "BACK  [ESC]")) screen = Screen.Characters;
            GUI.enabled = wasEnabled;
        }

        void DrawSelect(float left, float top)
        {
            GUI.Label(new Rect(left + 40, top + 20, 760, 54), "CHOOSE YOUR HERO", heading);
            GUI.Label(new Rect(left + 40, top + 76, 760, 28), "Every legend begins with a choice.", subtitle);
            DrawHeroCard(new Rect(left + 80, top + 112, 325, 270), 0);
            DrawHeroCard(new Rect(left + 435, top + 112, 325, 270), 1);
            GUI.Label(new Rect(left + 150, top + 393, 140, 30), "HERO NAME", subtitle);
            GUI.SetNextControlName("HeroName");
            heroName = GUI.TextField(new Rect(left + 300, top + 392, 240, 32), heroName ?? "", 16, nameField);
            nameFocused = GUI.GetNameOfFocusedControl() == "HeroName";
            if (GUI.Button(new Rect(left + 290, top + 440, 260, 44), "CONTINUE  [ENTER]", button)) screen = Screen.Tutorial;
            if (GUI.Button(new Rect(left + 342, top + 494, 156, 30), "BACK  [ESC]")) screen = saved.Count > 0 ? Screen.Characters : Screen.Title;
        }

        void DrawTutorial(float left, float top)
        {
            GUI.Label(new Rect(left + 30, top + 19, 780, 54), "YOUR FIRST ADVENTURE", heading);
            GUI.Label(new Rect(left + 55, top + 78, 730, 30), "A few essentials before you enter Emberfields", subtitle);
            string[] keys = { "A / D  or  ← / →", "SPACE", "W / S", "S / DOWN", "J / K", "Q / F", "E / I", "T / W", "L / ESC" };
            string[] actions = { "Move", "Jump", "Climb ladders", "Crouch", "Attack / Special attack", "HP / MP potions", "Collect loot / Inventory", "Create / Enter portal", "Dash / Menu" };
            for (int i = 0; i < keys.Length; i++)
            {
                float y = top + 125 + i * 33;
                if (i % 2 == 0) GUI.Box(new Rect(left + 105, y, 630, 31), GUIContent.none);
                GUI.Label(new Rect(left + 124, y + 2, 275, 27), keys[i], small);
                GUI.Label(new Rect(left + 405, y + 2, 305, 27), actions[i], small);
            }
            GUI.Label(new Rect(left + 60, top + 432, 720, 29), "Hunt monsters, collect equipment, and visit Emberhaven to restock. Your hero saves automatically.", small);
            if (GUI.Button(new Rect(left + 282, top + 468, 276, 44), "BEGIN ADVENTURE", button)) Create();
            if (GUI.Button(new Rect(left + 362, top + 517, 116, 25), "BACK  [ESC]")) screen = Screen.Select;
        }

        void DrawHeroCard(Rect r, int index)
        {
            var hero = Heroes.All[index];
            bool isSelected = pendingHero == index;
            Color previous = GUI.color;
            GUI.color = isSelected ? new Color(1f, .85f, .57f) : new Color(.72f, .78f, .87f);
            GUI.Box(r, GUIContent.none, isSelected ? selectedCard : card);
            GUI.color = previous;
            GUI.Label(new Rect(r.x + 10, r.y + 10, r.width - 20, 29), hero.name.ToUpperInvariant(), subtitle);
            Sprite[] preview = Sprites.Hero(hero.folder + "/idle");
            if (preview.Length > 0 && preview[0] != null)
            {
                Rect tex = preview[0].textureRect;
                float aspect = tex.width / Mathf.Max(1f, tex.height);
                float height = 91f, width = Mathf.Min(110f, height * aspect);
                DrawSprite(new Rect(r.center.x - width / 2, r.y + 43, width, height), preview[0]);
            }
            else GUI.Label(new Rect(r.x + 15, r.y + 64, r.width - 30, 55), hero.roleLabel, body);
            GUI.Label(new Rect(r.x + 12, r.y + 140, r.width - 24, 44), hero.tagline, small);
            GUI.Label(new Rect(r.x + 12, r.y + 181, r.width - 24, 25), hero.baseHp + " HP     " + hero.baseMp + " MP", small);
            if (GUI.Button(new Rect(r.x + 20, r.y + r.height - 45, r.width - 40, 34), isSelected ? "SELECTED" : "SELECT HERO", button)) pendingHero = index;
        }

        static void DrawSprite(Rect target, Sprite sprite)
        {
            Rect tex = sprite.textureRect;
            var t = sprite.texture;
            GUI.DrawTextureWithTexCoords(target, t, new Rect(tex.x / t.width, tex.y / t.height, tex.width / t.width, tex.height / t.height), true);
        }
    }
}
