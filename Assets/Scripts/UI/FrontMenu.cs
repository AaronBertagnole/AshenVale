using System;
using UnityEngine;

namespace AshenVale
{
    // Title, hero select and the controls screen (IMGUI, drawn in a fixed 900 x 610 reference space).
    public sealed class FrontMenu
    {
        public enum Screen { Title, Select, Tutorial }

        public Screen screen = Screen.Title;
        public int pendingHero;

        readonly MusicPlayer music;
        readonly Action<int> startHero;
        GUIStyle heading, subtitle, body, small, button, card, selectedCard;

        public FrontMenu(MusicPlayer music, Action<int> startHero)
        {
            this.music = music;
            this.startHero = startHero;
        }

        public void HandleKeys()
        {
            switch (screen)
            {
                case Screen.Title:
                    if (Input.GetKeyDown(KeyCode.M)) music.ToggleMute();
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) screen = Screen.Select;
                    break;
                case Screen.Select:
                    if (Input.GetKeyDown(KeyCode.Alpha1)) pendingHero = 0;
                    if (Input.GetKeyDown(KeyCode.Alpha2)) pendingHero = 1;
                    if (Input.GetKeyDown(KeyCode.Return)) screen = Screen.Tutorial;
                    else if (Input.GetKeyDown(KeyCode.Escape)) screen = Screen.Title;
                    break;
                case Screen.Tutorial:
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) startHero(pendingHero);
                    else if (Input.GetKeyDown(KeyCode.Escape)) screen = Screen.Select;
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

            if (screen == Screen.Title)
            {
                GUI.Label(new Rect(left + 35, top + 40, 770, 65), "ASHEN VALE", heading);
                GUI.Label(new Rect(left + 35, top + 110, 770, 35), "Where the Maps End", subtitle);
                GUI.Label(new Rect(left + 95, top + 174, 650, 72), "Beyond the last lantern, the wilds remember.\nA new journey begins in Emberhaven.", body);
                if (GUI.Button(new Rect(left + 295, top + 278, 250, 49), "START ADVENTURE", button)) screen = Screen.Select;
                GUI.Label(new Rect(left + 265, top + 354, 310, 27), "MUSIC VOLUME", subtitle);
                music.SetVolume(GUI.HorizontalSlider(new Rect(left + 287, top + 400, 215, 20), music.Volume, 0f, 1f));
                GUI.Label(new Rect(left + 510, top + 388, 65, 30), Mathf.RoundToInt(music.Volume * 100) + "%", small);
                if (GUI.Button(new Rect(left + 322, top + 432, 196, 33), music.Muted ? "UNMUTE  [M]" : "MUTE  [M]")) music.ToggleMute();
                GUI.Label(new Rect(left + 40, top + 500, 760, 24), "A whimsical journey into the unknown", small);
            }
            else if (screen == Screen.Select)
            {
                GUI.Label(new Rect(left + 40, top + 20, 760, 54), "CHOOSE YOUR HERO", heading);
                GUI.Label(new Rect(left + 40, top + 76, 760, 28), "Every legend begins with a choice.", subtitle);
                DrawHeroCard(new Rect(left + 80, top + 115, 325, 280), 0);
                DrawHeroCard(new Rect(left + 435, top + 115, 325, 280), 1);
                if (GUI.Button(new Rect(left + 290, top + 424, 260, 47), "CONTINUE  [ENTER]", button)) screen = Screen.Tutorial;
                if (GUI.Button(new Rect(left + 342, top + 486, 156, 32), "BACK  [ESC]")) screen = Screen.Title;
            }
            else
            {
                GUI.Label(new Rect(left + 30, top + 19, 780, 54), "YOUR FIRST ADVENTURE", heading);
                GUI.Label(new Rect(left + 55, top + 78, 730, 30), "A few essentials before you enter Emberfields", subtitle);
                string[] keys = { "A / D  or  ← / →", "SPACE", "W / S", "S / DOWN", "J / K", "Q / F", "E / I", "T / W", "L" };
                string[] actions = { "Move", "Jump", "Climb ladders", "Crouch", "Attack / Special attack", "HP / MP potions", "Collect loot / Inventory", "Create / Enter portal", "Dash" };
                for (int i = 0; i < keys.Length; i++)
                {
                    float y = top + 125 + i * 33;
                    if (i % 2 == 0) GUI.Box(new Rect(left + 105, y, 630, 31), GUIContent.none);
                    GUI.Label(new Rect(left + 124, y + 2, 275, 27), keys[i], small);
                    GUI.Label(new Rect(left + 405, y + 2, 305, 27), actions[i], small);
                }
                GUI.Label(new Rect(left + 60, top + 432, 720, 29), "Hunt monsters, collect equipment, and visit Emberhaven to restock.", small);
                if (GUI.Button(new Rect(left + 282, top + 468, 276, 44), "BEGIN ADVENTURE", button)) startHero(pendingHero);
                if (GUI.Button(new Rect(left + 362, top + 517, 116, 25), "BACK  [ESC]")) screen = Screen.Select;
            }
            GUI.matrix = oldMatrix;
        }

        void DrawHeroCard(Rect r, int index)
        {
            var hero = Heroes.All[index];
            bool selected = pendingHero == index;
            Color previous = GUI.color;
            GUI.color = selected ? new Color(1f, .85f, .57f) : new Color(.72f, .78f, .87f);
            GUI.Box(r, GUIContent.none, selected ? selectedCard : card);
            GUI.color = previous;
            GUI.Label(new Rect(r.x + 10, r.y + 10, r.width - 20, 29), hero.name.ToUpperInvariant(), subtitle);
            Sprite[] preview = Sprites.Hero(hero.folder + "/idle");
            if (preview.Length > 0 && preview[0] != null)
            {
                Sprite sprite = preview[0];
                Rect tex = sprite.textureRect;
                float aspect = tex.width / Mathf.Max(1f, tex.height);
                float height = 91f, width = Mathf.Min(110f, height * aspect);
                Rect target = new Rect(r.center.x - width / 2, r.y + 43, width, height);
                var t = sprite.texture;
                GUI.DrawTextureWithTexCoords(target, t, new Rect(tex.x / t.width, tex.y / t.height, tex.width / t.width, tex.height / t.height), true);
            }
            else GUI.Label(new Rect(r.x + 15, r.y + 64, r.width - 30, 55), hero.roleLabel, body);
            GUI.Label(new Rect(r.x + 12, r.y + 140, r.width - 24, 44), hero.tagline, small);
            GUI.Label(new Rect(r.x + 12, r.y + 181, r.width - 24, 25), hero.baseHp + " HP     " + hero.baseMp + " MP", small);
            if (GUI.Button(new Rect(r.x + 20, r.y + r.height - 45, r.width - 40, 34), selected ? "SELECTED" : "SELECT HERO", button)) pendingHero = index;
        }
    }
}
