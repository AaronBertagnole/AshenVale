using UnityEngine;
using UnityEngine.UI;

namespace AshenVale
{
    // Vector UI symbols remain sharp at CanvasScaler resolutions. Points are in a 64 x 64 design space.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudGlyph : MaskableGraphic
    {
        public enum Symbol { Blade, Burst, Dash, Portal, Bottle, Bag, Atlas, Menu, Diamond }
        public Symbol symbol;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            switch (symbol)
            {
                case Symbol.Blade: Line(vh, 15, 13, 49, 49, 7); Line(vh, 10, 22, 24, 8, 4); Triangle(vh, new Vector2(49, 49), new Vector2(55, 57), new Vector2(44, 51)); break;
                case Symbol.Burst:
                    for (int i = 0; i < 6; i++) { float a = i * Mathf.PI / 3; Line(vh, 32, 32, 32 + Mathf.Cos(a) * 23, 32 + Mathf.Sin(a) * 23, 4); }
                    Circle(vh, new Vector2(32, 32), 8); break;
                case Symbol.Dash: Line(vh, 12, 17, 36, 32, 5); Line(vh, 36, 32, 12, 47, 5); Line(vh, 29, 17, 53, 32, 5); Line(vh, 53, 32, 29, 47, 5); break;
                case Symbol.Portal:
                    for (int i = 0; i < 24; i++) { float a = i * Mathf.PI / 12, b = (i + 1) * Mathf.PI / 12; Line(vh, 32 + Mathf.Cos(a) * 16, 30 + Mathf.Sin(a) * 23, 32 + Mathf.Cos(b) * 16, 30 + Mathf.Sin(b) * 23, 3); }
                    Line(vh, 17, 6, 47, 6, 3); Circle(vh, new Vector2(32, 32), 5); break;
                case Symbol.Bottle: Line(vh, 26, 52, 38, 52, 7); Line(vh, 27, 47, 27, 40, 3); Line(vh, 37, 47, 37, 40, 3); Line(vh, 27, 40, 18, 29, 3); Line(vh, 37, 40, 46, 29, 3); Line(vh, 18, 29, 18, 14, 3); Line(vh, 46, 29, 46, 14, 3); Line(vh, 18, 14, 46, 14, 3); Quad(vh, new Vector2(20, 16), new Vector2(44, 27)); break;
                case Symbol.Bag: Line(vh, 20, 43, 20, 51, 3); Line(vh, 20, 51, 44, 51, 3); Line(vh, 44, 51, 44, 43, 3); Line(vh, 13, 41, 51, 41, 3); Line(vh, 13, 41, 10, 12, 3); Line(vh, 51, 41, 54, 12, 3); Line(vh, 10, 12, 54, 12, 3); Line(vh, 16, 31, 48, 31, 3); Quad(vh, new Vector2(29, 27), new Vector2(35, 36)); break;
                case Symbol.Atlas: Line(vh, 11, 14, 11, 50, 3); Line(vh, 11, 50, 31, 46, 3); Line(vh, 31, 46, 53, 52, 3); Line(vh, 53, 52, 53, 16, 3); Line(vh, 53, 16, 31, 10, 3); Line(vh, 31, 10, 11, 14, 3); Line(vh, 31, 10, 31, 46, 2); break;
                case Symbol.Menu: for (int i = 0; i < 3; i++) Line(vh, 13, 18 + i * 14, 51, 18 + i * 14, 4); break;
                case Symbol.Diamond: Triangle(vh, new Vector2(32, 54), new Vector2(12, 32), new Vector2(32, 10)); Triangle(vh, new Vector2(32, 54), new Vector2(32, 10), new Vector2(52, 32)); break;
            }
        }
        Vector2 Point(Vector2 p) { var r = rectTransform.rect; return new Vector2(r.xMin + p.x / 64 * r.width, r.yMin + p.y / 64 * r.height); }
        void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c)
        { int n = vh.currentVertCount; vh.AddVert(Point(a), color, Vector2.zero); vh.AddVert(Point(b), color, Vector2.zero); vh.AddVert(Point(c), color, Vector2.zero); vh.AddTriangle(n, n + 1, n + 2); }
        void Quad(VertexHelper vh, Vector2 a, Vector2 b) { Triangle(vh, a, new Vector2(a.x, b.y), b); Triangle(vh, a, b, new Vector2(b.x, a.y)); }
        void Line(VertexHelper vh, float x1, float y1, float x2, float y2, float width)
        { var a = new Vector2(x1, y1); var b = new Vector2(x2, y2); Vector2 d = (b - a).normalized; var o = new Vector2(-d.y, d.x) * width * .5f; Triangle(vh, a - o, a + o, b + o); Triangle(vh, a - o, b + o, b - o); }
        void Circle(VertexHelper vh, Vector2 center, float radius) { for (int i = 0; i < 16; i++) { float a = i * Mathf.PI / 8, b = (i + 1) * Mathf.PI / 8; Triangle(vh, center, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius); } }
    }
}

