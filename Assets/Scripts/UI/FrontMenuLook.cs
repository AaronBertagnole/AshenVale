using UnityEngine;

namespace AshenVale
{
    // Only menu presentation. No world-space scenery or gameplay camera changes.
    public static class FrontMenuLook
    {
        public static readonly Color Gold = new Color(.94f, .73f, .39f);
        public static readonly Color Ivory = new Color(.98f, .93f, .81f);
        public static readonly Color Quiet = new Color(.68f, .78f, .76f);
        public static GUIStyle Title, Heading, Subhead, Body, Small, Tiny, Button, Primary, Field, Panel, Menu, Track, Thumb, Choice, ChoiceShadow;
        static Texture2D background, glow, flame, logoArt, illustratedFlame;
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
        // A menu picture as a texture, whether Unity imported it as a texture or a sprite; says so in the Console if it is missing.
        static Texture2D LoadPicture(string path)
        {
            var tex=Resources.Load<Texture2D>(path);
            if(tex==null) { var sprite=Resources.Load<Sprite>(path); if(sprite!=null) tex=sprite.texture; }
            if(tex==null) Debug.LogWarning("Ashen Vale menu: picture missing at Assets/Resources/"+path+".png");
            return tex;
        }
        public static void Init()
        {
            if (Heading != null) return;
            Title = Label(62, Ivory, TextAnchor.MiddleLeft, true);
            var serif=Resources.Load<Font>("Fonts/AshenValeMenuSerif");
            if(serif!=null) Title.font=serif;
            Choice=new GUIStyle(Title){fontSize=31,fontStyle=FontStyle.Normal,alignment=TextAnchor.MiddleLeft};
            ChoiceShadow=new GUIStyle(Choice); ChoiceShadow.normal.textColor=new Color(.07f,.04f,.02f,.8f);
            logoArt=LoadPicture("Art/UI/AshenValeLogo");
            illustratedFlame=LoadPicture("Art/UI/AmberFlame");
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
        public static void Logo(Rect r)
        {
            if(logoArt!=null) GUI.DrawTexture(r,logoArt,ScaleMode.ScaleToFit);
            else GUI.Label(r,"ASHENVALE",Title);
        }
        public static void Flame(Rect r)
        {
            Color old=GUI.color; GUI.color=new Color(1f,1f,1f,.92f+Mathf.Sin(Time.unscaledTime*4f)*.06f);
            GUI.DrawTexture(r,illustratedFlame!=null ? illustratedFlame : flame,ScaleMode.ScaleToFit);
            GUI.color=old;
        }
        static Texture2D crestTexture, speakerTexture, mutedTexture, goldLine;
        static bool Inside(Vector2 p,Vector2 a,Vector2 b,Vector2 c)
        {
            float s=(a.x-c.x)*(p.y-c.y)-(a.y-c.y)*(p.x-c.x);
            float t=(b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x);
            float u=(c.x-b.x)*(p.y-b.y)-(c.y-b.y)*(p.x-b.x);
            return (s>=0f && t>=0f && u>=0f) || (s<=0f && t<=0f && u<=0f);
        }
        static void PaintRect(Texture2D t,int x,int y,int w,int h,Color color)
        {
            for(int yy=Mathf.Max(0,y);yy<Mathf.Min(t.height,y+h);yy++)
                for(int xx=Mathf.Max(0,x);xx<Mathf.Min(t.width,x+w);xx++) t.SetPixel(xx,yy,color);
        }
        static void Triangle(Texture2D t,Vector2 a,Vector2 b,Vector2 c,Color color)
        {
            for(int y=0;y<t.height;y++) for(int x=0;x<t.width;x++)
                if(Inside(new Vector2(x+.5f,y+.5f),a,b,c)) t.SetPixel(x,y,color);
        }
        static Texture2D ClearIcon(int w,int h)
        {
            var t=new Texture2D(w,h,TextureFormat.RGBA32,false); t.hideFlags=HideFlags.HideAndDontSave;
            t.SetPixels(new Color[w*h]); return t;
        }
        public static void Crest(Rect r)
        {
            if(crestTexture==null)
            {
                crestTexture=ClearIcon(256,72); Color c=Ivory;
                // A small vector-like settlement silhouette, not baked menu lettering.
                for(int x=18;x<239;x++)
                {
                    int y=Mathf.RoundToInt(7f+8f*Mathf.Sin((x-18)/221f*Mathf.PI));
                    PaintRect(crestTexture,x,y,1,2,c);
                }
                int[] trees={31,53,202,225};
                foreach(int x in trees)
                {
                    PaintRect(crestTexture,x-2,12,4,20,c);
                    Triangle(crestTexture,new Vector2(x,61),new Vector2(x-11,35),new Vector2(x+11,35),c);
                    Triangle(crestTexture,new Vector2(x,50),new Vector2(x-15,23),new Vector2(x+15,23),c);
                }
                PaintRect(crestTexture,95,17,77,25,c);
                PaintRect(crestTexture,91,17,15,32,c); PaintRect(crestTexture,158,17,15,34,c);
                PaintRect(crestTexture,122,17,22,40,c);
                Triangle(crestTexture,new Vector2(98,62),new Vector2(86,46),new Vector2(110,46),c);
                Triangle(crestTexture,new Vector2(133,70),new Vector2(118,54),new Vector2(148,54),c);
                Triangle(crestTexture,new Vector2(166,65),new Vector2(153,48),new Vector2(179,48),c);
                PaintRect(crestTexture,76,17,12,18,c); Triangle(crestTexture,new Vector2(82,44),new Vector2(72,33),new Vector2(92,33),c);
                PaintRect(crestTexture,128,17,10,16,Color.clear); crestTexture.Apply();
            }
            GUI.DrawTexture(r,crestTexture,ScaleMode.ScaleToFit);
        }
        public static void GoldLine(Rect r)
        {
            if(goldLine==null)
            {
                goldLine=ClearIcon(256,16);
                for(int y=0;y<16;y++) for(int x=0;x<256;x++)
                {
                    float end=Mathf.Clamp01(x/20f)*Mathf.Clamp01((255-x)/70f);
                    float core=Mathf.Exp(-Mathf.Abs(y-7.5f)*1.4f);
                    goldLine.SetPixel(x,y,new Color(1f,.73f,.25f,end*Mathf.Clamp01(core*2.2f)));
                }
                goldLine.Apply();
            }
            Color old=GUI.color; GUI.color=new Color(1f,1f,1f,.88f+.12f*Mathf.Sin(Time.unscaledTime*3f));
            GUI.DrawTexture(r,goldLine); GUI.color=old;
        }
        public static void Speaker(Rect r,bool muted)
        {
            if(speakerTexture!=null && speakerTexture.width>=128) { GUI.DrawTexture(r,muted ? mutedTexture : speakerTexture,ScaleMode.ScaleToFit); return; }
            if(speakerTexture==null)
            {
                speakerTexture=Resources.Load<Texture2D>("Art/UI/speaker"); mutedTexture=Resources.Load<Texture2D>("Art/UI/speaker_muted");
                if(speakerTexture!=null && mutedTexture!=null) { GUI.DrawTexture(r,muted ? mutedTexture : speakerTexture,ScaleMode.ScaleToFit); return; }
                speakerTexture=ClearIcon(36,36); mutedTexture=ClearIcon(36,36);
                foreach(var t in new[]{speakerTexture,mutedTexture})
                {
                    PaintRect(t,3,13,9,10,Ivory);
                    Triangle(t,new Vector2(10,13),new Vector2(20,5),new Vector2(20,31),Ivory);
                    Triangle(t,new Vector2(10,13),new Vector2(20,31),new Vector2(10,23),Ivory);
                }
                for(int y=0;y<36;y++) for(int x=22;x<36;x++)
                {
                    float dx=x-17f,dy=y-18f,d=Mathf.Sqrt(dx*dx+dy*dy);
                    if(Mathf.Abs(dy)<dx*1.15f && (Mathf.Abs(d-11f)<1.2f || Mathf.Abs(d-17f)<1.2f)) speakerTexture.SetPixel(x,y,Ivory);
                    if(x>=24 && x<=33 && (Mathf.Abs(y-(x-13))<1.5f || Mathf.Abs(y-(49-x))<1.5f)) mutedTexture.SetPixel(x,y,Ivory);
                }
                speakerTexture.Apply(); mutedTexture.Apply();
            }
            GUI.DrawTexture(r,muted ? mutedTexture : speakerTexture);
        }
        public static void Rule(float x,float y,float width) { Fill(new Rect(x,y,width,1),new Color(.8f,.64f,.36f,.65f)); }
    }
}
