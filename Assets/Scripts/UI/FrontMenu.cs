using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshenVale
{
    // Emberhaven Overlook menu refresh v1.
    // Menu-only presentation; public callbacks and save data contracts are unchanged.
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
        bool tutorialCreatesHero = true, showLan;
        int titleChoice;
        Vector2 lastPointer;
        Vector2 rosterScroll;

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
            if (busy) return; // AshenValeGame still handles Escape to cancel a connection.
            switch (screen)
            {
                case Screen.Title:
                    if (Input.GetKeyDown(KeyCode.M)) music.ToggleMute();
                    if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) titleChoice = (titleChoice + 3) % 4;
                    if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) titleChoice = (titleChoice + 1) % 4;
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) TitleAction(titleChoice);
                    break;
                case Screen.Characters:
                    int last = Mathf.Max(0, saved.Count - 1);
                    if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) { selected = Mathf.Clamp(selected - 1, 0, last); pendingDelete = null; RevealSelected(); }
                    if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) { selected = Mathf.Clamp(selected + 1, 0, last); pendingDelete = null; RevealSelected(); }
                    if (Input.GetKeyDown(KeyCode.Return) && selected >= 0 && selected < saved.Count) OpenMode(saved[selected]);
                    else if (Input.GetKeyDown(KeyCode.N)) PrepareNewHero();
                    else if (Input.GetKeyDown(KeyCode.Escape)) screen = Screen.Title;
                    break;
                case Screen.Select:
                    if (!nameFocused && Input.GetKeyDown(KeyCode.Alpha1)) pendingHero = 0;
                    if (!nameFocused && Input.GetKeyDown(KeyCode.Alpha2)) pendingHero = Mathf.Min(1, Heroes.All.Length - 1);
                    if (Input.GetKeyDown(KeyCode.Return)) { tutorialCreatesHero = true; screen = Screen.Tutorial; }
                    else if (Input.GetKeyDown(KeyCode.Escape)) screen = saved.Count > 0 ? Screen.Characters : Screen.Title;
                    break;
                case Screen.Tutorial:
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) FinishTutorial();
                    else if (Input.GetKeyDown(KeyCode.Escape)) screen = tutorialCreatesHero ? Screen.Select : Screen.Title;
                    break;
                case Screen.Mode:
                    if (!textFocused && Input.GetKeyDown(KeyCode.Escape)) screen = Screen.Characters;
                    break;
            }
        }

        void InitStyles()
        {
            FrontMenuLook.Init();
            heading = FrontMenuLook.Heading; subtitle = FrontMenuLook.Subhead; body = FrontMenuLook.Body;
            small = FrontMenuLook.Small; button = FrontMenuLook.Button; card = selectedCard = FrontMenuLook.Panel;
            rowName = FrontMenuLook.Subhead; rowDetail = FrontMenuLook.Small; nameField = FrontMenuLook.Field;
            statusText = FrontMenuLook.Body; sectionText = FrontMenuLook.Subhead;
        }

        public void Draw()
        {
            InitStyles();
            float scale = Mathf.Min(UnityEngine.Screen.width / 1280f, UnityEngine.Screen.height / 720f);
            if (scale <= 0f) return;
            Matrix4x4 oldMatrix = GUI.matrix; Color oldColor = GUI.color;
            bool oldEnabled = GUI.enabled;
            try
            {
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1)); GUI.color = Color.white;
                float sw = UnityEngine.Screen.width / scale, sh = UnityEngine.Screen.height / scale;
                float left = (sw - 1280f) * .5f, top = (sh - 720f) * .5f;
                FrontMenuLook.Backdrop(sw, sh, left, top);
                nameFocused = textFocused = false;
                switch (screen)
                {
                    case Screen.Title: DrawTitle(left, top); break;
                    case Screen.Characters: DrawCharacters(left, top); break;
                    case Screen.Select: DrawSelect(left, top); break;
                    case Screen.Mode: DrawMode(left, top); break;
                    default: DrawTutorial(left, top); break;
                }
                DrawVolume(left + (screen == Screen.Title ? 80f : 465f), top + 673f);
            }
            finally { GUI.matrix = oldMatrix; GUI.color = oldColor; GUI.enabled = oldEnabled; }
        }

        void DrawTitle(float left, float top)
        {
            FrontMenuLook.Fill(new Rect(left+48,top+94,530,516),new Color(.025f,.065f,.065f,.76f));
            GUI.Label(new Rect(left+80,top+123,620,100), "ASHEN VALE", FrontMenuLook.Title);
            FrontMenuLook.Rule(left+84,top+238,332);
            string[] labels = { saved.Count > 0 ? "Continue journey" : "Begin adventure", "New hero", "How to play", "Quit game" };
            Vector2 pointer = Event.current.mousePosition;
            bool moved = (pointer - lastPointer).sqrMagnitude > 1f;
            for (int i=0;i<labels.Length;i++)
            {
                Rect r = new Rect(left+80,top+286+i*66,358,54);
                if (moved && r.Contains(pointer)) titleChoice=i;
                if (GUI.Button(r,labels[i],FrontMenuLook.Menu)) TitleAction(i);
                if (i==titleChoice) FrontMenuLook.Flame(new Rect(r.x+16,r.y+12,18,28));
            }
            lastPointer=pointer;
            GUI.Label(new Rect(left+82,top+589,360,26),"ENTER  Select     /     ↑ ↓  Navigate",small);
        }

        void TitleAction(int choice)
        {
            switch(choice)
            {
                case 0: OpenHeroes(); break;
                case 1: PrepareNewHero(); break;
                case 2: tutorialCreatesHero=false; screen=Screen.Tutorial; break;
                default:
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
        }
        void FinishTutorial() { if(tutorialCreatesHero) Create(); else screen=Screen.Title; }
        void RevealSelected()
        {
            float y=selected*82f;
            if(y<rosterScroll.y) rosterScroll.y=y;
            if(y+76>rosterScroll.y+330) rosterScroll.y=y+76-330;
        }
        void DrawVolume(float x,float y)
        {
            GUI.Label(new Rect(x,y-1,70,22),music.Muted ? "MUTED" : "MUSIC",FrontMenuLook.Tiny);
            float value=GUI.HorizontalSlider(new Rect(x+68,y,142,20),music.Volume,0f,1f,FrontMenuLook.Track,FrontMenuLook.Thumb);
            if(Mathf.Abs(value-music.Volume)>.001f) music.SetVolume(Mathf.Round(value*20f)/20f);
            GUI.Label(new Rect(x+221,y-2,48,24),Mathf.RoundToInt(music.Volume*100)+"%",small);
            if(GUI.Button(new Rect(x+271,y-6,88,29),music.Muted ? "Unmute" : "Mute",FrontMenuLook.Button)) music.ToggleMute();
        }
        void Header(float x,float y,string title,string detail)
        {
            FrontMenuLook.Flame(new Rect(x,y+9,18,28));
            GUI.Label(new Rect(x+32,y,1100,48),title,heading);
            if(!string.IsNullOrEmpty(detail)) GUI.Label(new Rect(x+33,y+51,1100,28),detail,small);
        }
        void HeroStage(int index,float left,float top)
        {
            var hero=Heroes.All[Mathf.Clamp(index,0,Heroes.All.Length-1)];
            var frames=Sprites.Hero(hero.folder+"/idle");
            if(frames.Length==0) return;
            Sprite sprite=frames[Mathf.FloorToInt(Time.unscaledTime*5f)%frames.Length];
            if(sprite==null) return;
            float baseline=hero.folder=="DragonKnight" ? 114f/128f : 119f/128f;
            const float size=252f;
            DrawSprite(new Rect(left+643-size*.5f,top+602-size*baseline,size,size),sprite);
        }
        void Details(int index,CharacterData savedHero,float left,float top)
        {
            var h=Heroes.All[Mathf.Clamp(index,0,Heroes.All.Length-1)];
            Rect panel=new Rect(left+922,top+149,306,445); FrontMenuLook.Box(panel);
            float x=panel.x+24,w=panel.width-48;
            GUI.Label(new Rect(x,panel.y+24,w,24),"CLASS",FrontMenuLook.Tiny);
            GUI.Label(new Rect(x,panel.y+55,w,66),h.name,heading);
            FrontMenuLook.Rule(x,panel.y+132,w);
            GUI.Label(new Rect(x,panel.y+153,w,32),h.roleLabel,subtitle);
            GUI.Label(new Rect(x,panel.y+202,w,70),h.tagline,body);
            GUI.Label(new Rect(x,panel.y+290,w,50),h.baseHp+" BASE HP     "+h.baseMp+" BASE MP",small);
            if(savedHero!=null)
            {
                string where=savedHero.map>=0 && savedHero.map<Maps.All.Length ? Maps.All[savedHero.map].name : "Unknown region";
                GUI.Label(new Rect(x,panel.y+346,w,74),"Level "+savedHero.level+"  /  "+where+"\n"+SaveCodec.FormatPlayTime(savedHero.playSeconds)+" played",body);
            }
            else GUI.Label(new Rect(x,panel.y+352,w,62),h.dragonKnight ? "Hold the front line with sweeping spear strikes." : "Keep your distance and strike with precise arrows.",small);
        }

        void DrawCharacters(float left, float top)
        {
            Header(left+54,top+41,"YOUR HEROES","EMBERHAVEN OVERLOOK");
            FrontMenuLook.Box(new Rect(left+50,top+149,306,445));
            GUI.Label(new Rect(left+72,top+173,255,25),"YOUR JOURNEYS",subtitle);
            Rect area=new Rect(left+64,top+215,278,330);
            rosterScroll=GUI.BeginScrollView(area,rosterScroll,new Rect(0,0,256,Mathf.Max(330,saved.Count*82)));
            for(int i=0;i<saved.Count;i++)
            {
                var d=saved[i]; var hero=Heroes.All[Mathf.Clamp(d.hero,0,Heroes.All.Length-1)];
                Rect row=new Rect(0,i*82,252,76);
                if(GUI.Button(row,GUIContent.none,i==selected ? FrontMenuLook.Primary : button)) { selected=i; pendingDelete=null; }
                if(i==selected) FrontMenuLook.Flame(new Rect(10,row.y+27,13,22));
                var nameStyle=new GUIStyle(subtitle) { fontSize=18 }; nameStyle.normal.textColor=i==selected ? new Color(.10f,.09f,.065f) : FrontMenuLook.Ivory;
                while(nameStyle.fontSize>12 && nameStyle.CalcSize(new GUIContent(d.name)).x>205) nameStyle.fontSize--;
                GUI.Label(new Rect(35,row.y+12,205,26),d.name,nameStyle);
                var detailStyle=new GUIStyle(small); detailStyle.normal.textColor=i==selected ? new Color(.16f,.17f,.12f) : FrontMenuLook.Quiet;
                GUI.Label(new Rect(35,row.y+41,205,28),"Lv "+d.level+"  ·  "+hero.name,detailStyle);
            }
            GUI.EndScrollView();
            if(saved.Count==0) GUI.Label(new Rect(left+74,top+265,250,110),"Your first journey starts here. Create a hero to take a place by the fire.",body);
            if(GUI.Button(new Rect(left+72,top+551,260,30),"New hero  [N]",button)) PrepareNewHero();
            CharacterData d0=selected>=0 && selected<saved.Count ? saved[selected] : null;
            if(d0!=null)
            {
                HeroStage(d0.hero,left,top); Details(d0.hero,d0,left,top);
                var heroNameStyle=new GUIStyle(heading){ alignment=TextAnchor.MiddleCenter };
                while(heroNameStyle.fontSize>18 && heroNameStyle.CalcSize(new GUIContent(d0.name)).x>330) heroNameStyle.fontSize--;
                GUI.Label(new Rect(left+483,top+311,330,43),d0.name,heroNameStyle);
                bool old=GUI.enabled; GUI.enabled=!busy;
                if(GUI.Button(new Rect(left+922,top+622,306,48),"ENTER ASHENVALE",FrontMenuLook.Primary)) OpenMode(d0);
                GUI.enabled=old;
                if(GUI.Button(new Rect(left+541,top+628,220,30),pendingDelete==d0.id ? "Confirm deletion" : "Delete hero",button))
                {
                    if(pendingDelete==d0.id) { pendingDelete=null; deleteSaved(d0); }
                    else pendingDelete=d0.id;
                }
                if(pendingDelete==d0.id) GUI.Label(new Rect(left+415,top+592,470,30),"Click again to permanently delete this hero.",new GUIStyle(small){alignment=TextAnchor.MiddleCenter});
            }
            if(GUI.Button(new Rect(left+50,top+622,200,44),"Back  [ESC]",button)) screen=Screen.Title;
            DrawStatus(left+390,top+116,490);
        }

        void DrawMode(float left, float top)
        {
            if(modeHero==null) { screen=Screen.Characters; return; }
            var hero=Heroes.All[Mathf.Clamp(modeHero.hero,0,Heroes.All.Length-1)];
            Header(left+54,top+41,"ENTER ASHENVALE",modeHero.name+"  /  "+hero.name+"  /  Level "+modeHero.level);
            FrontMenuLook.Box(new Rect(left+54,top+161,548,350)); FrontMenuLook.Box(new Rect(left+628,top+161,598,350));
            bool old=GUI.enabled; GUI.enabled=!busy;
            GUI.Label(new Rect(left+80,top+188,490,31),"YOUR OWN ADVENTURE",subtitle);
            GUI.Label(new Rect(left+80,top+235,490,68),"Pick up your journey from where you left it.",body);
            if(GUI.Button(new Rect(left+80,top+319,494,52),"Play solo",FrontMenuLook.Primary)) playSolo(modeHero);
            GUI.Label(new Rect(left+654,top+188,546,31),"WITH FRIENDS",subtitle);
            if(GUI.Button(new Rect(left+654,top+245,546,48),"Host online game",button)) hostGame(modeHero,true);
            GUI.Label(new Rect(left+654,top+320,546,26),"JOIN WITH A CODE",small);
            GUI.SetNextControlName("JoinCode"); joinCode=GUI.TextField(new Rect(left+654,top+357,290,44),joinCode ?? "",12,nameField);
            bool beforeJoin=GUI.enabled; GUI.enabled=beforeJoin && !string.IsNullOrWhiteSpace(joinCode);
            if(GUI.Button(new Rect(left+958,top+357,242,44),"Join game",button)) joinGame(modeHero,true,joinCode.Trim()); GUI.enabled=beforeJoin;
            if(GUI.Button(new Rect(left+80,top+414,494,39),showLan ? "Hide local network" : "Local network options",button)) showLan=!showLan;
            if(showLan)
            {
                FrontMenuLook.Box(new Rect(left+54,top+524,1172,80));
                if(GUI.Button(new Rect(left+74,top+544,282,42),"Host on this PC",button)) hostGame(modeHero,false);
                GUI.SetNextControlName("JoinAddress"); joinAddress=GUI.TextField(new Rect(left+379,top+544,520,42),joinAddress ?? "",40,nameField);
                bool beforeAddress=GUI.enabled; GUI.enabled=beforeAddress && !string.IsNullOrWhiteSpace(joinAddress);
                if(GUI.Button(new Rect(left+922,top+544,280,42),"Join by IP",button)) joinGame(modeHero,false,joinAddress.Trim()); GUI.enabled=beforeAddress;
            }
            string focused=GUI.GetNameOfFocusedControl(); textFocused=focused=="JoinCode" || focused=="JoinAddress";
            // Isolated from the LAN panel; long text scrolls instead of overlapping it.
            DrawStatus(left+654,top+420,546);
            if(GUI.Button(new Rect(left+54,top+622,200,44),"Back  [ESC]",button)) screen=Screen.Characters;
            GUI.enabled=old;
            if(busy) GUI.Label(new Rect(left+656,top+623,544,38),"Connecting…  Press ESC to cancel.",body);
        }

        void DrawSelect(float left, float top)
        {
            pendingHero=Mathf.Clamp(pendingHero,0,Heroes.All.Length-1);
            Header(left+54,top+41,"A NEW JOURNEY","CHOOSE YOUR CLASS");
            FrontMenuLook.Box(new Rect(left+50,top+149,306,445));
            for(int i=0;i<Heroes.All.Length;i++) DrawHeroCard(new Rect(left+70,top+173+i*116,266,101),i);
            GUI.Label(new Rect(left+74,top+437,250,25),"HERO NAME",subtitle);
            GUI.SetNextControlName("HeroName");
            heroName=GUI.TextField(new Rect(left+72,top+473,260,44),heroName ?? "",16,nameField);
            nameFocused=GUI.GetNameOfFocusedControl()=="HeroName";
            GUI.Label(new Rect(left+74,top+535,250,40),"1 / 2  Choose class",small);
            HeroStage(pendingHero,left,top); Details(pendingHero,null,left,top);
            if(GUI.Button(new Rect(left+922,top+622,306,48),"CONTINUE",FrontMenuLook.Primary)) { tutorialCreatesHero=true; screen=Screen.Tutorial; GUI.FocusControl(""); }
            if(GUI.Button(new Rect(left+50,top+622,200,44),"Back  [ESC]",button)) screen=saved.Count>0 ? Screen.Characters : Screen.Title;
            DrawStatus(left+390,top+116,490);
        }

        // Connection progress or the last error, wrapped so long messages stay readable, with a button that copies
        // the full text (errors are easier to send to someone than to read off a screen).
        void DrawStatus(float x, float y, float width)
        {
            if(string.IsNullOrEmpty(status)) return;
            // Scrolling keeps a long connection error from covering controls.
            FrontMenuLook.Box(new Rect(x,y,width,78));
            float h=Mathf.Max(62,body.CalcHeight(new GUIContent(status),width-150));
            scroll=GUI.BeginScrollView(new Rect(x+12,y+8,width-138,62),scroll,new Rect(0,0,width-158,h));
            GUI.Label(new Rect(0,0,width-158,h),status,body); GUI.EndScrollView();
            if(GUI.Button(new Rect(x+width-116,y+23,104,32),"Copy text",button)) GUIUtility.systemCopyBuffer=status;
        }

        void DrawTutorial(float left, float top)
        {
            Header(left+54,top+41,"YOUR FIRST ADVENTURE","THE ESSENTIALS, AT YOUR OWN PACE");
            string[][] titles={new[]{"Move","Jump / drop through","Climb / crouch"},new[]{"Attack","Special attack","Dash"},new[]{"Potions","Loot / interact","Inventory / world map"}};
            string[][] keys={
                new[]{Keys.Name(GameAction.Left)+" / "+Keys.Name(GameAction.Right),Keys.Name(GameAction.Jump)+" / "+Keys.Name(GameAction.Down)+" + "+Keys.Name(GameAction.Jump),Keys.Name(GameAction.Up)+" / "+Keys.Name(GameAction.Down)},
                new[]{Keys.Name(GameAction.Attack),Keys.Name(GameAction.Skill),Keys.Name(GameAction.Dash)},
                new[]{Keys.Name(GameAction.HpPotion)+" / "+Keys.Name(GameAction.MpPotion),Keys.Name(GameAction.Loot)+" / "+Keys.Name(GameAction.Interact),Keys.Name(GameAction.Inventory)+" / "+Keys.Name(GameAction.WorldMap)}};
            string[] headings={"MOVEMENT","COMBAT","YOUR PACK"};
            for(int col=0;col<3;col++)
            {
                float x=left+54+col*397; FrontMenuLook.Box(new Rect(x,top+161,378,352));
                GUI.Label(new Rect(x+26,top+186,326,31),headings[col],subtitle); FrontMenuLook.Rule(x+26,top+235,326);
                float y=top+258;
                for(int row=0;row<titles[col].Length;row++)
                {
                    GUI.Label(new Rect(x+26,y,326,26),titles[col][row],body);
                    float kh=Mathf.Max(27,small.CalcHeight(new GUIContent(keys[col][row]),326));
                    GUI.Label(new Rect(x+26,y+30,326,kh),keys[col][row],small); y+=Mathf.Max(76,kh+40);
                }
            }
            FrontMenuLook.Box(new Rect(left+54,top+535,1172,65));
            GUI.Label(new Rect(left+78,top+550,1124,44),"Use "+Keys.Name(GameAction.Up)+" at portals. "+Keys.Name(GameAction.TownPortal)+" returns to town. Discover waystones to travel between towns. Your hero saves automatically.",body);
            if(GUI.Button(new Rect(left+922,top+622,306,48),tutorialCreatesHero ? "START ADVENTURE" : "BACK TO MENU",FrontMenuLook.Primary)) FinishTutorial();
            if(GUI.Button(new Rect(left+54,top+622,200,44),"Back  [ESC]",button)) screen=tutorialCreatesHero ? Screen.Select : Screen.Title;
        }

        void DrawHeroCard(Rect r, int index)
        {
            var h=Heroes.All[index]; bool chosen=pendingHero==index;
            if(GUI.Button(r,GUIContent.none,chosen ? FrontMenuLook.Primary : button)) pendingHero=index;
            var label=new GUIStyle(subtitle){fontSize=21}; label.normal.textColor=chosen ? new Color(.1f,.09f,.065f) : FrontMenuLook.Ivory;
            GUI.Label(new Rect(r.x+36,r.y+18,r.width-48,30),h.name,label);
            var detail=new GUIStyle(small); detail.normal.textColor=chosen ? new Color(.16f,.17f,.12f) : FrontMenuLook.Quiet;
            GUI.Label(new Rect(r.x+36,r.y+56,r.width-48,28),h.roleLabel,detail);
            if(chosen) FrontMenuLook.Flame(new Rect(r.x+11,r.y+33,15,25));
        }

        static void DrawSprite(Rect target, Sprite sprite)
        {
            Rect tex = sprite.textureRect;
            var t = sprite.texture;
            GUI.DrawTextureWithTexCoords(target, t, new Rect(tex.x / t.width, tex.y / t.height, tex.width / t.width, tex.height / t.height), true);
        }
    }
}

