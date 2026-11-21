using UnityEngine;

namespace AshenVale
{
    public sealed class FlameDragonCast : MonoBehaviour
    {
        static Sprite art;
        static bool loaded;
        public static Sprite Art
        {
            get
            {
                if (loaded) return art;
                loaded = true;
                var data = Resources.Load<TextAsset>("DragonfireVfx/FlameDragon");
                if (!data) return null;
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(data.bytes, true))
                {
                    Destroy(texture);
                    return null;
                }
                texture.wrapMode = TextureWrapMode.Clamp;
                art = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(.37f, .42f), 100f, 0, SpriteMeshType.FullRect);
                return art;
            }
        }

        const int Columns = 20, Rows = 10;
        MeshRenderer visual;
        Material flameMaterial;
        Mesh flameMesh;
        Vector3[] vertices;
        int[] triangles;
        Vector2 canvas;

        Vector3 origin;
        float age, facing, size;

        public void Begin(Sprite sprite, float casterX, float floor, int direction, int order)
        {
            facing = direction < 0 ? -1f : 1f;
            origin = new Vector3(casterX + facing * 1.45f, floor + 1.15f, 0f);
            size = Mathf.Min(2.25f / sprite.bounds.size.y,
                (Abilities.DragonfireReach - 2.25f) / sprite.bounds.max.x);
            canvas = sprite.rect.size;
            var filter = gameObject.AddComponent<MeshFilter>();
            visual = gameObject.AddComponent<MeshRenderer>();
            flameMaterial = new Material(Shader.Find("Sprites/Default"));
            flameMaterial.mainTexture = sprite.texture;
            visual.sharedMaterial = flameMaterial;
            visual.sortingOrder = order;
            flameMesh = new Mesh { name = "Dragonfire flame" };
            flameMesh.MarkDynamic();
            filter.sharedMesh = flameMesh;
            vertices = new Vector3[(Columns + 1) * (Rows + 1)];
            triangles = new int[Columns * Rows * 6];
            var uv = new Vector2[vertices.Length];
            for (int row = 0; row <= Rows; row++)
            for (int col = 0; col <= Columns; col++)
                uv[row * (Columns + 1) + col] = new Vector2(col / (float)Columns, row / (float)Rows);
            flameMesh.vertices = vertices;
            flameMesh.uv = uv;
            var colors = new Color[vertices.Length];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
            flameMesh.colors = colors;
            int index = 0;
            for (int row = 0; row < Rows; row++)
            for (int col = 0; col < Columns; col++)
            {
                int corner = row * (Columns + 1) + col;
                triangles[index++] = corner;
                triangles[index++] = (corner + Columns + 1);
                triangles[index++] = (corner + 1);
                triangles[index++] = (corner + 1);
                triangles[index++] = (corner + Columns + 1);
                triangles[index++] = (corner + Columns + 2);
            }
            visual.sortingOrder = order;
            flameMesh.triangles = triangles;
            Draw();
        }

        void OnDestroy()
        {
            if (flameMesh) Destroy(flameMesh);
            if (flameMaterial) Destroy(flameMaterial);
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= 1.2f) Destroy(gameObject);
            else Draw();
        }

        void Draw()
        {
            float launch = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / .18f));
            float surge = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / .5f));
            float scale = size * Mathf.Lerp(.64f, 1f, launch);
            transform.localScale = new Vector3(facing * scale, scale, 1f);
            transform.position = origin + new Vector3(facing * .8f * surge, .12f * surge, 0f);
            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.43f, 1.2f, age));
            float flicker = .94f + .06f * Mathf.Sin(age * 46f);
            flameMaterial.color = new Color(1f, flicker, .87f + .13f * flicker,
                fade * Mathf.Lerp(.45f, 1f, launch));

            for (int row = 0; row <= Rows; row++)
            for (int col = 0; col <= Columns; col++)
            {
                float u = col / (float)Columns, v = row / (float)Rows;
                float tail = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.12f, .66f, u));
                float ripple = Mathf.Sin(age * 32f + u * 19f + v * 4f) * tail * .045f;
                float lunge = Mathf.Sin(surge * Mathf.PI) * Mathf.SmoothStep(0f, 1f, u) * .045f;
                vertices[row * (Columns + 1) + col] = new Vector3(
                    (u - .37f + lunge) * canvas.x / 100f,
                    (v - .42f + ripple) * canvas.y / 100f, 0f);
            }
            flameMesh.vertices = vertices;
            flameMesh.RecalculateBounds();
        }
    }
}
