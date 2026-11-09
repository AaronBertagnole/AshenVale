using UnityEngine;

namespace AshenVale
{
    static class RollingGroundArt
    {
        // Tile only the road. Each column is shifted vertically as a unit, so
        // the pixels below the walking line are never pulled into long streaks.
        public static void BuildRoad(Transform parent, float width, Sprite road, Sprite back,
            float surfaceOffset, Color earth, Color surface, int order)
        {
            BuildStrip(parent, width, null, 0f, 10f, earth, surface, order - 2);
            if (back != null)
                BuildStrip(parent, width, back, surfaceOffset, back.bounds.size.y, Color.white, Color.white, order - 1);
            if (road != null)
                BuildStrip(parent, width, road, surfaceOffset, road.bounds.size.y, Color.white, Color.white, order);
        }

        static void BuildStrip(Transform parent, float width, Sprite sprite, float offset,
            float depth, Color bottomColor, Color topColor, int order)
        {
            float tileWidth = sprite != null ? sprite.bounds.size.x : width;
            int tiles = Mathf.CeilToInt(width / tileWidth);
            int segments = Mathf.CeilToInt(tileWidth / .25f);
            int stride = (segments + 1) * 2;
            var vertices = new Vector3[tiles * stride];
            var uv = new Vector2[vertices.Length];
            var colors = new Color32[vertices.Length];
            var triangles = new int[tiles * segments * 6];
            var texture = sprite != null ? sprite.texture : Texture2D.whiteTexture;
            var rect = sprite != null ? sprite.textureRect : new Rect(0, 0, 1, 1);
            int index = 0;
            for (int tile = 0; tile < tiles; tile++)
            {
                float start = tile * tileWidth;
                float span = Mathf.Min(tileWidth, width - start);
                for (int i = 0; i <= segments; i++)
                {
                    int v = tile * stride + i * 2;
                    float x = start + span * i / segments;
                    float y = RollingGround.HeightAt(x) + offset;
                    float u = (rect.x + .5f + (x - start) / tileWidth * (rect.width - 1f)) / texture.width;
                    vertices[v] = new Vector3(x, y - depth, 0);
                    vertices[v + 1] = new Vector3(x, y, 0);
                    uv[v] = new Vector2(u, (rect.y + .5f) / texture.height);
                    uv[v + 1] = new Vector2(u, (rect.yMax - .5f) / texture.height);
                    colors[v] = bottomColor;
                    colors[v + 1] = topColor;
                    if (i == segments) continue;
                    triangles[index++] = v;
                    triangles[index++] = v + 1;
                    triangles[index++] = v + 2;
                    triangles[index++] = v + 2;
                    triangles[index++] = v + 1;
                    triangles[index++] = v + 3;
                }
            }

            var go = new GameObject(sprite != null ? sprite.name + " slope strip" : "Slope earth");
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = go.name };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.colors32 = colors;
            mesh.triangles = triangles;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            var material = new Material(Shader.Find("Sprites/Default"));
            material.mainTexture = texture;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            var lifetime = go.AddComponent<RollingGroundMeshLifetime>();
            lifetime.OwnedMesh = mesh;
            lifetime.OwnedMaterial = material;
        }

        // Fit a picture uniformly to the full vertical span that the camera can see.
        public static void FitBackdrop(MapDef def, Sprite sprite, ref float width, out float floor)
        {
            WorldView.CameraRange(def, 0f, out _, out _, out float minY, out float maxY);
            float bottom = minY - WorldView.ViewHalfHeight - .2f;
            float required = maxY + WorldView.ViewHalfHeight + .2f - bottom;
            float aspect = sprite.rect.width / sprite.rect.height;
            width = Mathf.Max(width, required * aspect);
            floor = (Tuning.Ground - bottom) / (width / aspect);
        }
    }
}
