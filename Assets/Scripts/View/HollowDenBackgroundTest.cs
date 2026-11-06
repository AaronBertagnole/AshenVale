using UnityEngine;

namespace AshenVale
{
    // Isolated scenery experiment. Playable terrain and all actor offsets stay authored.
    public static class HollowDenBackgroundTest
    {
        public const bool Enabled = true;
        public const string ArtPath = "Art/MapTests/hollow_den";
        const float WalkingRow = 558f, SourceHeight = 724f;
        const float ReferenceHeight = 38f / 3f;

        public static bool TryBuild(MapVisual visual, MapDef def, out PictureFrame front, out PictureFrame back)
        {
            front = back = new PictureFrame();
            if (!Enabled || def.id != Maps.HollowDen) return false;
            var sprite = Sprites.Art(ArtPath);
            if (sprite == null) return false;
            float aspect = sprite.rect.width / sprite.rect.height;
            if (Mathf.Abs(aspect - 3f) > .02f)
            { Debug.LogWarning("Hollow Den test needs the supplied 2172 x 724 painting."); return false; }

            float groundSurface = Sprites.Art("Art/Themes/" + def.theme + "/ground") != null
                ? MapScenery.PaintedGroundSurface : 0f;
            float imageFloor = (SourceHeight - WalkingRow) / SourceHeight;
            float minX, maxX, minY, maxY;
            WorldView.CameraRange(def, 0f, out minX, out maxX, out minY, out maxY);
            float top = maxY + WorldView.ViewHalfHeight - Tuning.Ground;
            float bottom = minY - WorldView.ViewHalfHeight - Tuning.Ground;
            // Fit the ENTIRE vertical camera envelope once, then keep the floor fixed.
            // Coverage is explicit; no solid-color extension and no vertical camera drift.
            float height = Mathf.Max(ReferenceHeight,
                Mathf.Max((top - groundSurface + .15f) / (1f - imageFloor),
                          (groundSurface - bottom + .15f) / imageFloor));
            if (Camera.main != null)
                height = Mathf.Max(height, (2f * WorldView.ViewHalfHeight * Camera.main.aspect + .2f) / aspect);
            float width = height * aspect;
            float floor = imageFloor - groundSurface / height;
            var layer = new GameObject("Hollow Den test panorama").transform;
            layer.SetParent(visual.root.transform, false);
            var art = Sprites.Picture("Hollow Den test scenery", sprite, MapScenery.SkyOrder, layer, 0, 0);
            float scale = width / sprite.bounds.size.x;
            art.transform.localScale = new Vector3(scale, scale, 1f);
            art.transform.localPosition = new Vector3(-sprite.bounds.min.x * scale, -sprite.bounds.min.y * scale, 0f);
            layer.localPosition = new Vector3(0f, Tuning.Ground - floor * height, 0f);
            visual.layers.Add(new ParallaxLayer { transform = layer, panorama = true, floorAnchored = true,
                width = width, height = height, floor = floor, travel = 1f });
            // No PictureFrame: existing pixel-addressed decorative effects have not been
            // calibrated to this test image. Existing general map ambience remains.
            Debug.Log("Hollow Den test: scenery " + width.ToString("F2") + " x " + height.ToString("F2") +
                " world units; source walking row " + WalkingRow + "; playable ground unchanged.");
            return true;
        }
    }
}
