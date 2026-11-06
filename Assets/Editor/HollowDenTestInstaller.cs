using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AshenVale.EditorTools
{
    public static class HollowDenTestInstaller
    {
        const string Source = "Assets/Scripts/View/MapScenery.cs";
        const string Image = "Assets/Resources/Art/MapTests/hollow_den.png";
        const string Hook = "            if (HollowDenBackgroundTest.TryBuild(visual, def, out front, out back)) return true;";
        const string Anchor = "            string own = \"Art/Backdrops/\" + def.key, path = own;";

        public static string Patch(string text)
        {
            if (text.Contains(Hook)) return text;
            int i = text.IndexOf(Anchor, StringComparison.Ordinal);
            if (i < 0 || text.IndexOf(Anchor, i + Anchor.Length, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException("MapScenery does not match the supplied version. No source changed.");
            return text.Insert(i, Hook + "\n");
        }
        static void Save(string original, string output)
        {
            if (original.Replace("\r\n", "\n") == output) return;
            string backup = "Library/AshenVale/HollowDenTest/" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            Directory.CreateDirectory(backup);
            File.WriteAllText(backup + "/MapScenery.cs.txt", original);
            File.WriteAllText(Source, output.Replace("\n", original.Contains("\r\n") ? "\r\n" : "\n"), new System.Text.UTF8Encoding(false));
            Debug.Log("Hollow Den source backup: " + backup);
        }
        [MenuItem("AshenVale/Art/Hollow Den Test/Install")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Stop Play mode first."); return; }
            try
            {
                string original = File.ReadAllText(Source);
                string output = Patch(original.Replace("\r\n", "\n"));
                var importer = AssetImporter.GetAtPath(Image) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing test PNG: " + Image);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.maxTextureSize = 4096;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
                if (Resources.Load<Sprite>(HollowDenBackgroundTest.ArtPath) == null)
                    throw new InvalidOperationException("Test image did not import as a sprite. No source changed.");
                Save(original, output);
                AssetDatabase.Refresh();
                Debug.Log("Hollow Den test installed. Restart Play mode and enter Hollow Den from Mosswood. Other maps use their original rendering.");
            }
            catch (Exception e) { Debug.LogError("Hollow Den test: " + e.Message); }
        }
        [MenuItem("AshenVale/Art/Hollow Den Test/Remove Hook")]
        public static void Remove()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Stop Play mode first."); return; }
            string original = File.ReadAllText(Source);
            Save(original, original.Replace("\r\n", "\n").Replace(Hook + "\n", ""));
            AssetDatabase.Refresh();
            Debug.Log("Hollow Den test hook removed. The test art path is separate from normal map loading.");
        }
    }
}
