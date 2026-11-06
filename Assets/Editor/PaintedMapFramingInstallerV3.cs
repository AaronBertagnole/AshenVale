using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AshenVale.EditorTools
{
    public static class PaintedMapFramingInstallerV3
    {
        static string Edit(string text, string before, string after)
        {
            if (text.Contains(after)) return text;
            int index = text.IndexOf(before, StringComparison.Ordinal);
            if (index < 0 || text.IndexOf(before, index + before.Length, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException("Unsupported source near: " + before.Trim());
            return text.Substring(0, index) + after + text.Substring(index + before.Length);
        }

        public static string PatchScenery(string text)
        {
            text = Edit(text,
                "                if (!BuildBackdrop(visual, def, look, out front, out back))",
                "                bool hasBackdrop = BuildBackdrop(visual, def, look, out front, out back);\n                if (!hasBackdrop)");
            text = Edit(text, "                Ground(root, def, theme, art, rng);",
                "                if (!hasBackdrop || !PaintedMapFraming.UsesOwnGround(def)) Ground(root, def, theme, art, rng);");
            text = Edit(text, "            if (sprite == null || width <= 0f) return false;",
                "            if (sprite == null || width <= 0f) return false;\n            PaintedMapFraming.Apply(def, sprite, ref width, ref floor);");
            text = Edit(text, "        public bool panorama;", "        public bool panorama;\n        public bool floorAnchored;");
            text = Edit(text, "            BuildEffects(look, front, back, painted);",
                "            PaintedMapFraming.AddSkyFill(def, root);\n            BuildEffects(look, front, back, painted);");
            text = Edit(text, "BuildWaystone(root, def.waystoneX, def.waystoneY, !painted);",
                "BuildWaystone(root, def.waystoneX, def.waystoneY, PaintedMapFraming.ShowPlaceholderProps(def, painted, front.Valid));");
            text = Edit(text, "BuildMerchant(root, stand.x, stand.y, !painted);",
                "BuildMerchant(root, stand.x, stand.y, PaintedMapFraming.ShowPlaceholderProps(def, painted, front.Valid));");
            text = Edit(text, "BuildSmithy(root, def.smithyX, def.smithyY, !painted);",
                "BuildSmithy(root, def.smithyX, def.smithyY, PaintedMapFraming.ShowPlaceholderProps(def, painted, front.Valid));");
            text = Edit(text,
                "static PictureFrame Panorama(MapVisual visual, string name, Sprite sprite, float width, float floor, bool flip, float travel, int order)",
                "static PictureFrame Panorama(MapVisual visual, string name, Sprite sprite, float width, float floor, bool flip, float travel, int order, bool floorAnchored = false)");
            text = Edit(text, "floor = floor, travel = travel });", "floor = floor, travel = travel, floorAnchored = floorAnchored });");
            text = Edit(text, "Panorama(visual, \"Distance\", far, width, floor, flip, look.backTravel, SkyOrder);",
                "Panorama(visual, \"Distance\", far, width, floor, flip, look.backTravel, SkyOrder, PaintedMapFraming.LocksFloor(def));");
            text = Edit(text, "Panorama(visual, \"Backdrop\", sprite, width, floor, flip, 1f, far != null ? FarOrder : SkyOrder);",
                "Panorama(visual, \"Backdrop\", sprite, width, floor, flip, 1f, far != null ? FarOrder : SkyOrder, PaintedMapFraming.LocksFloor(def));");
            return text;
        }

        public static string PatchView(string text)
        {
            text = Edit(text, "float high = Mathf.Max(low, maxY + ViewHalfHeight - layer.height);",
                "float high = layer.floorAnchored ? low : Mathf.Max(low, maxY + ViewHalfHeight - layer.height);");
            if (text.Contains("heroOffset = PaintedSpriteContact.HeroOffset(")) return text;
            text = Edit(text,
                "                Vector3 basePosition = new Vector3(x, y + (p.crouching ? .33f : .48f) + v.climbOffset, 0);",
                "                float heroOffset = p.crouching ? .33f : .48f;\n" +
                "                if (!p.climbing && !p.dead && PaintedMapFraming.UsesOwnGround(Maps.All[p.map]))\n" +
                "                    heroOffset = PaintedSpriteContact.HeroOffset(p.Hero.folder, v.sr.sprite, v.go.transform.localScale.y, heroOffset);\n" +
                "                Vector3 basePosition = new Vector3(x, y + heroOffset + v.climbOffset, 0);");
            return text;
        }

        [MenuItem("AshenVale/Art/Apply Background Framing Correction V3")]
        public static void Apply()
        {
            // This calibration is for the supplied 3:1 art, not the older padded files.
            var configText = Resources.Load<TextAsset>("Art/painted_map_framing");
            if (configText == null) { Debug.LogError("Missing V3 framing JSON. No source files changed."); return; }
            var config = JsonUtility.FromJson<PaintedMapFraming.Settings>(configText.text);
            if (config == null || config.maps == null || config.maps.Length != 41)
            { Debug.LogError("Expected the 41-entry V3 calibration. No source files changed."); return; }
            foreach (var entry in config.maps)
            {
                var art = Resources.Load<Sprite>("Art/Backdrops/" + entry.key);
                if (art == null || Mathf.Abs(art.rect.width / art.rect.height - 3f) > .02f)
                { Debug.LogError("Missing or incompatible V3 image: " + entry.key + ". Copy the supplied Backdrops PNGs to Assets/Resources/Art/Backdrops, imported as single sprites. No source files changed."); return; }
            }
            string[] paths = { "Assets/Scripts/View/MapScenery.cs", "Assets/Scripts/View/WorldView.cs" };
            var originals = new string[2]; var outputs = new string[2];
            try
            {
                for (int i = 0; i < paths.Length; i++) originals[i] = File.ReadAllText(paths[i]);
                outputs[0] = PatchScenery(originals[0].Replace("\r\n", "\n"));
                outputs[1] = PatchView(originals[1].Replace("\r\n", "\n"));
            }
            catch (Exception e) { Debug.LogError("No source files changed. " + e.Message + ". Share the current MapScenery.cs and WorldView.cs to adapt this patch."); return; }
            string backup = "Library/AshenVale/BackgroundFramingV3/" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            Directory.CreateDirectory(backup);
            for (int i = 0; i < paths.Length; i++) File.Copy(paths[i], backup + "/" + Path.GetFileName(paths[i]) + ".txt", false);
            try
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    string newline = originals[i].Contains("\r\n") ? "\r\n" : "\n";
                    File.WriteAllText(paths[i], outputs[i].Replace("\n", newline), new System.Text.UTF8Encoding(false));
                }
            }
            catch
            {
                for (int i = 0; i < paths.Length; i++) File.Copy(backup + "/" + Path.GetFileName(paths[i]) + ".txt", paths[i], true);
                throw;
            }
            AssetDatabase.Refresh();
            Debug.Log("Framing V3 installed. Stop and restart Play mode. Source backups: " + backup);
        }
    }
}
