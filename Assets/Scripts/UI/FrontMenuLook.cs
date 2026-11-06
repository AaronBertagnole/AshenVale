using UnityEngine;

namespace AshenVale
{
    // Only menu presentation. No world-space scenery or gameplay camera changes.
    public static class FrontMenuLook
    {
        public static readonly Color Gold = new Color(.94f, .73f, .39f);
        public static readonly Color Ivory = new Color(.98f, .93f, .81f);
        public static readonly Color Quiet = new Color(.68f, .78f, .76f);
        public static GUIStyle Title, Heading, Subhead, Body, Small, Tiny, Button, Primary, Field, Panel, Menu, Track, Thumb;
        static Texture2D background, glow, flame;
        static bool loaded;

        static Texture2D Tile(Color fill, Color edge)
        {
            const int n = 32; var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            t.hideFlags = HideFlags.HideAndDontSave;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float dx = x - Mathf.Clamp(x, 6, n-7), dy = y - Mathf.Clamp(y, 6, n-7);
                float radius = Mathf.Sqrt(dx*dx+dy*dy);
                bool border = x==0 || y==0 || x==n-1 || y==n-1 || radius>5f;
                t.SetPixel(x,y,radius>6f ? Color.clear : border ? edge : fill);
            }
            t.Apply(); return t;
        }
        static GUIStyle Label(int size, Color color, TextAnchor align = TextAnchor.UpperLeft, bool bold = false)
        {
            var s = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = align, wordWrap = true,
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal, padding = new RectOffset(0, 0, 0, 0) };
            s.normal.textColor = color; return s;
        }
        public static void Init()
        {
            if (Heading != null) return;
            Title = Label(62, Ivory, TextAnchor.MiddleLeft, true);
            try
            {
                var serif = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "DejaVu Serif", "Times New Roman" }, 62);
                if (serif != null) Title.font = serif;
            }
            catch { /* The current GUI font remains a valid fallback. */ }
            Heading = Label(34, Ivory, TextAnchor.MiddleLeft, true);
            Subhead = Label(20, Gold, TextAnchor.UpperLeft, true);
            Body = Label(17, Ivory); Small = Label(14, Quiet); Tiny = Label(12, Quiet);
            Panel = new GUIStyle(GUI.skin.box) { border = new RectOffset(8, 8, 8, 8) };
            Panel.normal.background = Tile(new Color(.035f, .105f, .105f, .92f), new Color(.57f, .47f, .28f, .7f));
            Button = new GUIStyle(GUI.skin.button) { fontSize = 17, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter, border = new RectOffset(8, 8, 8, 8), padding = new RectOffset(12,12,6,6) };
            Button.normal.background = Tile(new Color(.035f,.13f,.135f,.96f), new Color(.59f,.49f,.30f));
            Button.hover.background = Tile(new Color(.10f,.23f,.22f), Gold);
            Button.active.background = Tile(new Color(.20f,.28f,.22f), Gold);
            Button.focused.background = Button.hover.background;
            Button.normal.textColor = Ivory; Button.hover.textColor = Color.white;
            Button.active.textColor = Ivory; Button.focused.textColor = Color.white;
            Primary = new GUIStyle(Button); Primary.normal.background = Tile(new Color(.71f,.42f,.15f), Gold);
            Primary.hover.background = Tile(new Color(.84f,.56f,.22f), Ivory);
            Primary.active.background = Primary.focused.background = Primary.hover.background;
            Primary.normal.textColor = new Color(.10f,.09f,.065f); Primary.hover.textColor = Primary.normal.textColor;
            Primary.active.textColor = Primary.normal.textColor; Primary.focused.textColor = Primary.normal.textColor;
            Menu = new GUIStyle(Button) { alignment = TextAnchor.MiddleLeft, fontSize = 21, padding = new RectOffset(46,16,6,6) };
            Field = new GUIStyle(GUI.skin.textField) { fontSize = 18, padding = new RectOffset(12,12,9,5), border = new RectOffset(8,8,8,8) };
            Field.normal.background = Tile(new Color(.025f,.075f,.08f), new Color(.4f,.47f,.41f));
            Field.focused.background = Tile(new Color(.04f,.12f,.13f), Gold);
            Field.normal.textColor = Ivory; Field.focused.textColor = Color.white;
            Track = new GUIStyle(GUI.skin.horizontalSlider); Track.normal.background = Tile(new Color(.04f,.12f,.12f), Quiet);
            Track.fixedHeight = 5; Track.margin = new RectOffset(0,0,7,0);
            Thumb = new GUIStyle(GUI.skin.horizontalSliderThumb); Thumb.normal.background = Tile(Gold, Ivory);
            Thumb.hover.background = Thumb.active.background = Thumb.normal.background;
            Thumb.fixedWidth = 12; Thumb.fixedHeight = 19;
            glow = new Texture2D(64,64,TextureFormat.RGBA32,false); glow.hideFlags = HideFlags.HideAndDontSave;
            flame = new Texture2D(32,48,TextureFormat.RGBA32,false); flame.hideFlags = HideFlags.HideAndDontSave;
            for (int y=0;y<64;y++) for(int x=0;x<64;x++)
            { float r = Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))/32f; glow.SetPixel(x,y,new Color(1f,.52f,.13f,Mathf.Pow(Mathf.Clamp01(1-r),2))); }
            for(int y=0;y<48;y++) for(int x=0;x<32;x++)
            {
                float v=y/47f, center=15.5f+6f*v*v, half=13f*Mathf.Pow(Mathf.Sin(v*Mathf.PI),.8f);
                float a=Mathf.Clamp01(half-Mathf.Abs(x-center));
                flame.SetPixel(x,y,new Color(1f,Mathf.Lerp(.56f,.91f,v),Mathf.Lerp(.12f,.50f,v),a));
            }
            glow.Apply(); flame.Apply();
        }
        public static void Backdrop(float sw, float sh, float left, float top)
        {
            if (!loaded) { background=Resources.Load<Texture2D>("Art/UI/EmberhavenOverlook"); loaded=true; }
            Fill(new Rect(0,0,sw,sh),new Color(.025f,.07f,.08f));
            if(background != null)
            {
                Color old=GUI.color; GUI.color=new Color(.35f,.42f,.43f,1f);
                GUI.DrawTexture(new Rect(0,0,sw,sh),background,ScaleMode.ScaleAndCrop);
                GUI.color=old; GUI.DrawTexture(new Rect(left,top,1280,720),background,ScaleMode.StretchToFill);
            }
            // Atmospheric embers are live GUI decoration, separate from the painted plate.
            float t=Time.unscaledTime;
            for(int i=0;i<18;i++)
            {
                float phase=Mathf.Repeat(t*.06f+i*.137f,1f);
                float x=left+804+Mathf.Sin(i*4.7f+t*.4f)*25f+phase*12f;
                float y=top+568-phase*128f;
                Color old=GUI.color; GUI.color=new Color(1f,.8f,.35f,Mathf.Sin(phase*Mathf.PI)*.8f);
                GUI.DrawTexture(new Rect(x,y,3,3),Texture2D.whiteTexture); GUI.color=old;
            }
            Color previous=GUI.color; GUI.color=new Color(1f,.74f,.35f,.20f+Mathf.Sin(t*5f)*.035f);
            GUI.DrawTexture(new Rect(left+752,top+513,120,115),glow); GUI.color=previous;
        }
        public static void Fill(Rect r, Color c) { Color old=GUI.color; GUI.color=c; GUI.DrawTexture(r,Texture2D.whiteTexture); GUI.color=old; }
        public static void Box(Rect r) { GUI.Box(r,GUIContent.none,Panel); }
        public static void Flame(Rect r)
        {
            Color old=GUI.color; GUI.color=new Color(1f,1f,1f,.82f+Mathf.Sin(Time.unscaledTime*4f)*.15f);
            GUI.DrawTexture(r,flame); GUI.color=old;
        }
        public static void Rule(float x,float y,float width) { Fill(new Rect(x,y,width,1),new Color(.8f,.64f,.36f,.65f)); }
    }
}
