using UnityEngine;

namespace AshenVale
{
    // Bend the existing painted road to the same surface as the movement simulation.
    sealed class RollingGroundArt
    {
        readonly Transform parent;
        readonly Mesh mesh;
        readonly Vector3[] vertices;
        readonly float[] rows, weights;
        readonly int columns;
        readonly float width;
        float lastX = float.NaN;

        public RollingGroundArt(Transform parent, SpriteRenderer original, float width, float height, float road)
        {
            this.parent = parent;
            this.width = width;
            var sprite = original.sprite;
            var uvRect = sprite.textureRect;
            float shoulder = Mathf.Min(road + 1.2f, height);
            float blendEnd = Mathf.Min(road + 5f, height);
            rows = new[] { -10f, 0f, road, shoulder, blendEnd, height };
            weights = new[] { 1f, 1f, 1f, .78f, 0f, 0f };
            columns = Mathf.CeilToInt(width / .25f) + 1;
            vertices = new Vector3[columns * rows.Length];
            var uv = new Vector2[vertices.Length];
            var colors = new Color32[vertices.Length];
            var indices = new int[(columns - 1) * (rows.Length - 1) * 6];
            int index = 0;
            for (int r = 0; r < rows.Length; r++)
                for (int c = 0; c < columns; c++)
                {
                    int v = r * columns + c;
                    float u = (float)c / (columns - 1);
                    float sampleY = Mathf.Clamp01(rows[r] / height);
                    uv[v] = new Vector2(
                        (uvRect.x + .5f + u * (uvRect.width - 1f)) / sprite.texture.width,
                        (uvRect.y + .5f + sampleY * (uvRect.height - 1f)) / sprite.texture.height);
                    colors[v] = new Color32(255, 255, 255, 255);
                    if (r == rows.Length - 1 || c == columns - 1) continue;
                    indices[index++] = v;
                    indices[index++] = v + columns;
                    indices[index++] = v + 1;
                    indices[index++] = v + 1;
                    indices[index++] = v + columns;
                    indices[index++] = v + columns + 1;
                }

            var go = new GameObject("Rolling Emberfields road");
            go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = original.sharedMaterial;
            renderer.sortingLayerID = original.sortingLayerID;
            renderer.sortingOrder = original.sortingOrder;
            var properties = new MaterialPropertyBlock();
            properties.SetTexture("_MainTex", sprite.texture);
            renderer.SetPropertyBlock(properties);
            mesh = new Mesh { name = "Emberfields road mesh" };
            mesh.MarkDynamic();
            filter.sharedMesh = mesh;
            go.AddComponent<RollingGroundMeshLifetime>().OwnedMesh = mesh;
            Refresh();
            mesh.uv = uv;
            mesh.colors32 = colors;
            mesh.triangles = indices;
            original.enabled = false;
        }

        public static void BuildFallback(Transform parent, float width, Color body, Color surface, int order)
        {
            int count = Mathf.CeilToInt(width / .25f) + 1;
            var vertices = new Vector3[count * 3];
            var colors = new Color32[vertices.Length];
            var triangles = new int[(count - 1) * 12];
            int index = 0;
            for (int c = 0; c < count; c++)
            {
                float x = width * c / (count - 1);
                float y = RollingGround.HeightAt(x);
                int v = c * 3;
                vertices[v] = new Vector3(x, y - 10f, 0);
                vertices[v + 1] = new Vector3(x, y - .25f, 0);
                vertices[v + 2] = new Vector3(x, y, 0);
                colors[v] = colors[v + 1] = body;
                colors[v + 2] = surface;
                if (c == count - 1) continue;
                for (int row = 0; row < 2; row++)
                {
                    int a = v + row;
                    triangles[index++] = a;
                    triangles[index++] = a + 1;
                    triangles[index++] = a + 3;
                    triangles[index++] = a + 3;
                    triangles[index++] = a + 1;
                    triangles[index++] = a + 4;
                }
            }
            var go = new GameObject("Rolling ground fallback");
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = "Rolling ground fallback mesh" };
            mesh.vertices = vertices;
            mesh.colors32 = colors;
            mesh.triangles = triangles;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            var material = new Material(Shader.Find("Sprites/Default"));
            material.mainTexture = Texture2D.whiteTexture;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            var lifetime = go.AddComponent<RollingGroundMeshLifetime>();
            lifetime.OwnedMesh = mesh;
            lifetime.OwnedMaterial = material;
        }

        public void Refresh()
        {
            float left = parent.position.x;
            if (left == lastX) return;
            lastX = left;
            for (int c = 0; c < columns; c++)
            {
                float x = width * c / (columns - 1);
                float offset = RollingGround.HeightAt(left + x) - Tuning.Ground;
                for (int r = 0; r < rows.Length; r++)
                    vertices[r * columns + c] = new Vector3(x, rows[r] + offset * weights[r], 0f);
            }
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }
    }
}
