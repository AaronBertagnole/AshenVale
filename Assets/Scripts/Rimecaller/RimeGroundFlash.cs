using UnityEngine;

namespace AshenVale
{
    public sealed partial class WorldView
    {
        bool SpawnPolishedStorm(SimEvent strike)
        {
            if (!RimeGroundFlash.LoadArt()) return false;
            var map = world.maps[strike.map].def;
            float left = map.minX, right = map.maxX;
            foreach (var ledge in map.platforms)
            {
                if (strike.x < ledge.left || strike.x > ledge.right ||
                    Mathf.Abs(ledge.top - strike.y) > .025f) continue;
                left = ledge.left;
                right = ledge.right;
                break;
            }
            var root = new GameObject("Stormcall ground strike");
            root.transform.position = new Vector3(strike.x, strike.y, 0f);
            root.AddComponent<RimeGroundFlash>().Begin(strike.x-left, right-strike.x,
                strike.heavy, ArrowOrder+2);
            rimeFx.Add(root);
            return true;
        }
    }

    public sealed class RimeGroundFlash : MonoBehaviour
    {
        static readonly Sprite[,] frames = new Sprite[4,2];
        static readonly int[] contacts = {280,277,271,271};
        readonly SpriteRenderer[] sides = new SpriteRenderer[2];
        readonly float[] room = new float[2];
        float age, strength;

        public static bool LoadArt()
        {
            for (int i=0; i<4; i++)
            {
                if (frames[i,0]) continue;
                var tex = RimeEffectArt.Load("Storm"+i);
                if (!tex) return false;
                int x = contacts[i];
                for (int side=0; side<2; side++)
                {
                    var rect = new Rect(side==0 ? 0 : x, 0, side==0 ? x : tex.width-x, tex.height);
                    frames[i,side] = Sprite.Create(tex, rect,
                        new Vector2(side==0 ? 1f : 0f, (tex.height-551f)/tex.height),
                        125f, 0, SpriteMeshType.FullRect);
                }
            }
            return true;
        }

        public void Begin(float left, float right, bool finale, int order)
        {
            room[0]=left;
            room[1]=right;
            strength=finale ? 1f : .78f;
            for (int i=0;i<2;i++)
            {
                var part=new GameObject(i==0 ? "Left arc" : "Right arc");
                part.transform.SetParent(transform,false);
                sides[i]=part.AddComponent<SpriteRenderer>();
                sides[i].sortingOrder=order;
            }
            Draw();
        }

        void Update()
        {
            age+=Time.deltaTime;
            if (age>=.21f) Destroy(gameObject);
            else Draw();
        }

        void Draw()
        {
            int frame=age<.025f ? 0 : age<.075f ? 1 : age<.14f ? 2 : 3;
            for (int i=0;i<2;i++)
            {
                var sprite=frames[frame,i];
                sides[i].sprite=sprite;
                float width=Mathf.Min(strength,room[i]/sprite.bounds.size.x);
                sides[i].transform.localScale=new Vector3(width,1f,1f);
                sides[i].color=new Color(1f,1f,1f,strength*(1f-Mathf.InverseLerp(.14f,.21f,age)));
            }
        }
    }
}
