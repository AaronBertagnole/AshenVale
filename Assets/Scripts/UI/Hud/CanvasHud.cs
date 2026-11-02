using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AshenVale
{
    // Retained Canvas HUD; the existing GameHud continues to own authoritative inventory/shop/travel windows.
    // Build only once at runtime. No scene edits or manual Inspector references are needed.
    public sealed class CanvasHud : MonoBehaviour
    {
        sealed class Meter { public Image fill; public Text text; }
        sealed class ActionSlot
        {
            public GameAction action; public Button button; public Image cooldown; public Text key, value, timer;
        }
        sealed class Entry { public string text; public Color color; }
        readonly List<Entry> activity = new List<Entry>();
        readonly List<ActionSlot> slots = new List<ActionSlot>();
        readonly List<Text> lines = new List<Text>();
        World world; GameHud legacy; Action<GameAction> queue;
        Canvas canvas; RectTransform safeRoot; CanvasGroup group; HudTheme theme; GameObject ownedEventSystem;
        Text heroName, heroClass, level, location, region, gold, network, journeyTitle, journeyBody, mapHint, xpText, interaction;
        Button copyCode; Image portrait; Meter health, mana, xp; HudMinimap minimap;
        GameObject mapPanel, interactionPanel; string characterId; int mapId = -1, seenLevel = -1, portraitHero = -1;
        readonly List<RaycastResult> hoverHits = new List<RaycastResult>();
        PointerEventData hoverData;
        Rect safeArea; int screenWidth, screenHeight;

        public void Bind(World state, GameHud hud, Action<GameAction> enqueue)
        {
            world = state; legacy = hud; queue = enqueue; theme = new HudTheme();
            Build(); legacy.canvasActive = true; legacy.canvasTheme = theme; legacy.onPickup = AddActivity;
            SetVisible(false);
        }

        void Build()
        {
            var c = new GameObject("Ashen Vale Canvas HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            c.transform.SetParent(transform, false); canvas = c.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 5;
            var scaler = c.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            group = c.GetComponent<CanvasGroup>(); safeRoot = Rect("Safe area", c.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
            if (!EventSystem.current)
            {
                ownedEventSystem = new GameObject("HUD Event System", typeof(EventSystem), typeof(StandaloneInputModule));
                ownedEventSystem.transform.SetParent(transform, false);
            }

            var status = Panel("Hero", safeRoot, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(344, 112));
            var avatar = Panel("Portrait frame", status, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -11), new Vector2(80, 88));
            portrait = Graphic<Image>("Hero sprite", avatar, new Vector2(0, 1), new Vector2(0, 1), new Vector2(5, -4), new Vector2(70, 78)); portrait.preserveAspect = true;
            heroName = Label("Name", status, 102, 10, 230, 25, 19, HudTheme.Ivory, FontStyle.Bold);
            heroClass = Label("Class", status, 104, 36, 228, 18, 10, HudTheme.Muted);
            health = MeterAt(status, 104, 60, 228, 17, new Color(.88f, .26f, .24f));
            mana = MeterAt(status, 104, 83, 228, 15, new Color(.13f, .64f, .82f));
            level = Label("Level", avatar, 0, 67, 80, 18, 11, HudTheme.Ivory, FontStyle.Bold); level.alignment = TextAnchor.MiddleCenter;
            network = Label("Connection", safeRoot, 22, 140, 340, 38, 11, HudTheme.Muted); network.horizontalOverflow = HorizontalWrapMode.Wrap;
            copyCode = ButtonAt("Copy join code", safeRoot, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -184), new Vector2(150, 25), () => { GUIUtility.systemCopyBuffer = legacy.joinCode; AddActivity("Join code copied.", HudTheme.Teal); });
            CenterLabel(copyCode.transform, "COPY JOIN CODE", 10, HudTheme.Ivory);

            var heading = Rect("Location", safeRoot, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -17), new Vector2(350, 55)); heading.pivot = new Vector2(.5f, 1);
            location = Label("Map", heading, 0, 0, 350, 30, 23, HudTheme.Ivory, FontStyle.Bold); location.alignment = TextAnchor.MiddleCenter;
            region = Label("Region", heading, 0, 33, 350, 18, 11, HudTheme.Copper); region.alignment = TextAnchor.MiddleCenter;

            var map = Panel("Minimap", safeRoot, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(270, 142)); mapPanel = map.gameObject;
            Label("Map header", map, 12, 8, 180, 18, 10, HudTheme.Copper, FontStyle.Bold).text = "LOCAL MAP";
            var compass = Label("Compass", map, 238, 8, 20, 18, 12, HudTheme.Ivory); compass.text = "N";
            minimap = Graphic<HudMinimap>("Terrain", map, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -28), new Vector2(250, 88));
            mapHint = Label("Map keys", map, 12, 119, 246, 16, 9, HudTheme.Muted); mapHint.alignment = TextAnchor.MiddleRight;
            var purse = Panel("Gold", safeRoot, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -170), new Vector2(165, 29));
            gold = Label("Gold value", purse, 10, 3, 145, 23, 14, HudTheme.Copper, FontStyle.Bold); gold.alignment = TextAnchor.MiddleCenter;

            var journey = Panel("Journey", safeRoot, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -216), new Vector2(270, 145));
            Label("Journey heading", journey, 14, 10, 242, 18, 11, HudTheme.Copper, FontStyle.Bold).text = "YOUR JOURNEY";
            var paper = Panel("Guidance", journey, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -35), new Vector2(250, 99), theme.parchment);
            Color ink = new Color(.22f, .18f, .12f);
            journeyTitle = Label("Guidance title", paper, 11, 8, 228, 22, 14, ink, FontStyle.Bold);
            journeyBody = Label("Guidance body", paper, 11, 33, 228, 59, 12, ink); journeyBody.horizontalOverflow = HorizontalWrapMode.Wrap;

            var log = Panel("Adventure log", safeRoot, new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 20), new Vector2(270, 95));
            Label("Log heading", log, 12, 7, 246, 16, 10, HudTheme.Copper, FontStyle.Bold).text = "ADVENTURE LOG";
            for (int i = 0; i < 3; i++) lines.Add(Label("Activity " + i, log, 12, 27 + i * 19, 246, 18, 11, HudTheme.Muted));

            var actions = Rect("Action bar", safeRoot, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 20), new Vector2(440, 105)); actions.pivot = new Vector2(.5f, 0);
            GameAction[] actionsList = { GameAction.Attack, GameAction.Skill, GameAction.Dash, GameAction.TownPortal, GameAction.HpPotion, GameAction.MpPotion };
            HudGlyph.Symbol[] glyphs = { HudGlyph.Symbol.Blade, HudGlyph.Symbol.Burst, HudGlyph.Symbol.Dash, HudGlyph.Symbol.Portal, HudGlyph.Symbol.Bottle, HudGlyph.Symbol.Bottle };
            Color[] colors = { new Color(1,.65f,.24f), HudTheme.Teal, new Color(.7f,.89f,.8f), new Color(.65f,.8f,1), new Color(1,.4f,.35f), new Color(.3f,.75f,1) };
            for (int i = 0; i < actionsList.Length; i++)
            {
                GameAction action = actionsList[i];
                var button = ButtonAt(action.ToString(), actions, new Vector2(0, 1), new Vector2(0, 1), new Vector2(i * 74, -3), new Vector2(66, 66), () => Request(action));
                var icon = Graphic<HudGlyph>("Icon", button.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -7), new Vector2(46, 46)); icon.symbol = glyphs[i]; icon.color = colors[i];
                var mask = Graphic<Image>("Cooldown", button.transform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(66, 66)); mask.sprite = theme.white; mask.type = Image.Type.Filled; mask.fillMethod = Image.FillMethod.Radial360; mask.color = new Color(.015f,.05f,.07f,.82f); mask.fillAmount = 0;
                var timer = Label("Cooldown seconds", button.transform, 2, 18, 62, 25, 17, HudTheme.Ivory, FontStyle.Bold); timer.alignment = TextAnchor.MiddleCenter;
                var keyFrame = Panel("Key", button.transform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, -5), new Vector2(63, 22));
                var key = CenterLabel(keyFrame, "", 10, HudTheme.Ivory); key.resizeTextForBestFit = true; key.resizeTextMinSize = 6; key.resizeTextMaxSize = 10;
                var count = Label("Count", button.transform, 3, 3, 58, 16, 10, HudTheme.Ivory); count.alignment = TextAnchor.MiddleRight;
                slots.Add(new ActionSlot { action = action, button = button, key = key, value = count, timer = timer, cooldown = mask });
            }
            xp = MeterAt(actions, 0, 91, 436, 7, new Color(.95f,.68f,.22f)); xp.text.gameObject.SetActive(false);
            xpText = Label("Experience", actions, 0, 101, 436, 15, 9, HudTheme.Muted); xpText.alignment = TextAnchor.MiddleCenter;

            var shortcuts = Rect("Shortcuts", safeRoot, new Vector2(1,0), new Vector2(1,0), new Vector2(-20,20), new Vector2(218,78)); shortcuts.pivot = new Vector2(1,0);
            string[] labels = { "Bag", "World", "Menu" };
            HudGlyph.Symbol[] menuIcons = { HudGlyph.Symbol.Bag, HudGlyph.Symbol.Atlas, HudGlyph.Symbol.Menu };
            for (int i=0; i<3; i++)
            {
                int which=i; var button=ButtonAt(labels[i],shortcuts,new Vector2(0,1),new Vector2(0,1),new Vector2(i*74,0),new Vector2(64,53),()=>Open(which));
                var icon=Graphic<HudGlyph>("Icon",button.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(13,-7),new Vector2(38,38));icon.symbol=menuIcons[i];icon.color=HudTheme.Copper;
                Label("Label",shortcuts,i*74,58,64,18,11,HudTheme.Ivory).text=labels[i];
            }
            var prompt=Panel("Interaction",safeRoot,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,155),new Vector2(340,34));interactionPanel=prompt.gameObject;
            interaction=CenterLabel(prompt,"",12,HudTheme.Ivory);
        }

        // Raycast at the current pointer rather than depending on EventSystem's Update ordering.
        // Only Buttons are raycast targets; the decorative HUD never blocks world clicks.
        public bool PointerOverHud()
        {
            if (!canvas || !canvas.gameObject.activeInHierarchy || !group.blocksRaycasts || !EventSystem.current) return false;
            if (hoverData == null) hoverData = new PointerEventData(EventSystem.current);
            hoverData.Reset(); hoverData.position = Input.mousePosition; hoverHits.Clear();
            canvas.GetComponent<GraphicRaycaster>().Raycast(hoverData, hoverHits);
            return hoverHits.Count > 0;
        }

        public void SetVisible(bool visible)
        {
            if (!canvas) return;
            if (!visible && canvas.gameObject.activeSelf) { characterId = null; mapId = -1; activity.Clear(); }
            canvas.gameObject.SetActive(visible);
        }

        public void Refresh(PlayerState me)
        {
            if (me == null || world == null) { SetVisible(false); return; }
            SetVisible(true); UpdateSafeArea();
            bool blocked=legacy.WindowOpen || me.dead;
            group.interactable=!blocked; group.blocksRaycasts=!blocked; group.alpha = legacy.WindowOpen ? 0f : 1f;
            var ch=me.ch; var def=world.maps[me.map].def;
            if(characterId!=ch.id){characterId=ch.id;mapId=-1;seenLevel=ch.level;portraitHero=-1;activity.Clear();AddActivity("Your adventure continues.",HudTheme.Muted);}
            if(mapId!=me.map){mapId=me.map;AddActivity("Arrived in "+def.name+".",HudTheme.Ivory);}
            if(ch.level>seenLevel){AddActivity("Level "+ch.level+" reached!",HudTheme.Copper);seenLevel=ch.level;}
            if(portraitHero!=ch.hero){portraitHero=ch.hero;var frames=Sprites.Hero(me.Hero.folder+"/idle");portrait.sprite=frames.Length>0?frames[0]:null;portrait.enabled=portrait.sprite!=null;}
            heroName.text=string.IsNullOrEmpty(ch.name)?me.Hero.name:ch.name;heroClass.text=me.Hero.name;level.text="LEVEL "+ch.level;
            SetMeter(health,me.hp,me.MaxHp,"HP");SetMeter(mana,me.mp,me.MaxMp,"MP");SetMeter(xp,ch.xp,Tuning.XpNeeded(ch.level),"");
            xpText.text="LEVEL "+ch.level+"  /  "+ch.xp+" of "+Tuning.XpNeeded(ch.level)+" XP";
            location.text=def.name.ToUpperInvariant();region.text=def.kind==MapKind.Town?"A HAVEN ON THE ROAD":def.LevelText.ToUpperInvariant();
            gold.text=ch.gold.ToString("N0")+" GOLD";
            network.text=legacy.status??"";copyCode.gameObject.SetActive(!string.IsNullOrEmpty(legacy.joinCode));
            mapPanel.SetActive(legacy.minimapShown);if(legacy.minimapShown)minimap.Refresh(world,me);
            mapHint.text=Keys.Name(GameAction.WorldMap)+"  WORLD  /  "+Keys.Name(GameAction.Minimap)+"  HIDE";
            journeyTitle.text=def.kind==MapKind.Town?"A moment to prepare":"Beyond the walls";
            if(def.kind==MapKind.Town)journeyBody.text=(def.hasSmithy?"Trade, improve your gear, or visit the waystone.":"Restock supplies or visit the waystone.")+"\n"+KnownTowns(ch)+" towns discovered";
            else journeyBody.text=def.LevelText+"\nFind the next road. "+Keys.Name(GameAction.WorldMap)+" opens your explored world.";
            foreach(var slot in slots)
            {
                slot.key.text=Keys.Name(slot.action);slot.value.text="";float time=0,duration=1;bool ready=!blocked;
                switch(slot.action)
                {
                    case GameAction.Attack: time=me.attackCooldown;duration=me.lastWasSkill?me.Hero.skillCooldown:me.Hero.attackCooldown;break;
                    case GameAction.Skill: time=me.attackCooldown;duration=me.lastWasSkill?me.Hero.skillCooldown:me.Hero.attackCooldown;ready &= me.mp>=me.Hero.skillManaCost;slot.value.text=me.Hero.skillManaCost+" MP";break;
                    case GameAction.Dash:time=me.dashCooldown;duration=Tuning.DashCooldown;ready &= !me.climbing;break;
                    case GameAction.TownPortal:time=me.portalCast;duration=Tuning.PortalCastTime;ready &= def.kind!=MapKind.Town&&!me.portalOpen&&me.portalCast<=0;break;
                    case GameAction.HpPotion:slot.value.text="x"+ch.hpPotions;ready &= ch.hpPotions>0&&me.hp<me.MaxHp;break;
                    case GameAction.MpPotion:slot.value.text="x"+ch.mpPotions;ready &= ch.mpPotions>0&&me.mp<me.MaxMp;break;
                }
                slot.cooldown.fillAmount=Mathf.Clamp01(time/Mathf.Max(.01f,duration));slot.timer.text=time>0?time.ToString("0.0"):"";
                slot.button.interactable=ready&&time<=0;
            }
            string prompt="";
            if(!blocked)
            {
                string key="["+Keys.Name(GameAction.Interact)+"] ";
                if(world.NearMerchant(me,2))prompt=key+"Browse the merchant's wares";
                else if(world.NearWaystone(me,2))prompt=key+"Open waystone travel";
                else if(world.NearSmithy(me,2))prompt=key+"Visit the blacksmith";
                else if(me.portalCast>0)prompt="Opening a town portal...";
            }
            interactionPanel.SetActive(prompt.Length>0);interaction.text=prompt;
            for(int i=0;i<lines.Count;i++){int index=activity.Count-1-i;lines[i].text=index>=0?activity[index].text:"";lines[i].color=index>=0?activity[index].color:HudTheme.Muted;}
        }

        static int KnownTowns(Character ch){int count=0;for(int i=0;i<Maps.All.Length;i++)if(Maps.IsTown(i)&&ch.KnowsTown(i))count++;return count;}
        void Request(GameAction action){if(legacy.WindowOpen)return;if(queue!=null)queue(action);}
        void Open(int which){legacy.ToggleCanvasWindow(which);}
        public void AddActivity(string message,Color tint){activity.Add(new Entry{text=message,color=tint});if(activity.Count>12)activity.RemoveAt(0);}
        void UpdateSafeArea()
        {
            if(safeArea==Screen.safeArea&&screenWidth==Screen.width&&screenHeight==Screen.height)return;
            safeArea=Screen.safeArea;screenWidth=Screen.width;screenHeight=Screen.height;
            safeRoot.anchorMin=new Vector2(safeArea.xMin/Mathf.Max(1,Screen.width),safeArea.yMin/Mathf.Max(1,Screen.height));
            safeRoot.anchorMax=new Vector2(safeArea.xMax/Mathf.Max(1,Screen.width),safeArea.yMax/Mathf.Max(1,Screen.height));
        }
        static void SetMeter(Meter meter,int current,int maximum,string prefix){meter.fill.fillAmount=maximum>0?Mathf.Clamp01((float)current/maximum):0;meter.text.text=(prefix.Length>0?prefix+"  ":"")+current+" / "+maximum;}

        RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
        {var g=new GameObject(name,typeof(RectTransform));var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=position;r.sizeDelta=size;return r;}
        T Graphic<T>(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size) where T:Graphic
        {var r=Rect(name,parent,anchor,pivot,position,size);var graphic=r.gameObject.AddComponent<T>();graphic.raycastTarget=false;return graphic;}
        RectTransform Panel(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size,Sprite surface=null)
        {var image=Graphic<Image>(name,parent,anchor,pivot,position,size);image.sprite=surface?surface:theme.panel;image.type=Image.Type.Sliced;return image.rectTransform;}
        Text Label(string name,Transform parent,float x,float y,float width,float height,int fontSize,Color color,FontStyle style=FontStyle.Normal)
        {var t=Graphic<Text>(name,parent,new Vector2(0,1),new Vector2(0,1),new Vector2(x,-y),new Vector2(width,height));t.font=theme.font;t.fontSize=fontSize;t.fontStyle=style;t.color=color;t.alignment=TextAnchor.MiddleLeft;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.supportRichText=false;return t;}
        Text CenterLabel(Transform parent,string value,int fontSize,Color color)
        {var rt=(RectTransform)parent;var t=Label("Text",parent,3,0,rt.rect.width-6,rt.rect.height,fontSize,color);t.alignment=TextAnchor.MiddleCenter;t.text=value;return t;}
        Button ButtonAt(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size,Action callback)
        {
            var r=Panel(name,parent,anchor,pivot,position,size,theme.slot);var image=r.GetComponent<Image>();image.raycastTarget=true;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.75f,1,.94f);colors.pressedColor=new Color(.45f,.7f,.66f);colors.disabledColor=new Color(.5f,.58f,.58f,.6f);b.colors=colors;
            // Navigation must not claim arrows / Space that the side-scroller uses for movement and jumping.
            b.navigation=new Navigation{mode=Navigation.Mode.None};b.onClick.AddListener(()=>{callback();if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);});return b;
        }
        Meter MeterAt(Transform parent,float x,float y,float width,float height,Color color)
        {
            var r=Panel("Meter",parent,new Vector2(0,1),new Vector2(0,1),new Vector2(x,-y),new Vector2(width,height),theme.track);
            var fill=Graphic<Image>("Fill",r,new Vector2(0,1),new Vector2(0,1),new Vector2(2,-2),new Vector2(width-4,height-4));fill.sprite=theme.white;fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.color=color;
            var text=Label("Value",r,4,0,width-8,height,10,HudTheme.Ivory,FontStyle.Bold);text.alignment=TextAnchor.MiddleCenter;return new Meter{fill=fill,text=text};
        }
        void OnDestroy(){if(legacy!=null){legacy.canvasActive=false;legacy.onPickup=null;legacy.canvasTheme=null;legacy.ReleaseCanvasSkin();}if(theme!=null)theme.Dispose();if(ownedEventSystem)Destroy(ownedEventSystem);}
    }
}
