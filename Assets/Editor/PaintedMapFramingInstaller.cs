using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AshenVale.EditorTools
{
    public static class PaintedMapFramingInstaller
    {
        [MenuItem("AshenVale/Art/Apply Background Framing Fix")]
        public static void Apply()
        {
            const string path = "Assets/Scripts/View/MapScenery.cs";
            if (!File.Exists(path)) { Debug.LogError("Missing " + path); return; }
            string original = File.ReadAllText(path);
            string newline = original.Contains("\r\n") ? "\r\n" : "\n";
            string text = original.Replace("\r\n", "\n");
            if (text.Contains("PaintedMapFraming.Apply(def, sprite, ref width, ref floor);") && text.Contains("PaintedMapFraming.UsesOwnGround(def)"))
            { Debug.Log("Background framing fix is already installed."); return; }
            string[] before = {
                "                if (!BuildBackdrop(visual, def, look, out front, out back))",
                "                Ground(root, def, theme, art, rng);",
                "            if (sprite == null || width <= 0f) return false;"
            };
            string[] after = {
                "                bool hasBackdrop = BuildBackdrop(visual, def, look, out front, out back);\n                if (!hasBackdrop)",
                "                if (!hasBackdrop || !PaintedMapFraming.UsesOwnGround(def)) Ground(root, def, theme, art, rng);",
                "            if (sprite == null || width <= 0f) return false;\n            PaintedMapFraming.Apply(def, sprite, ref width, ref floor);"
            };
            // Check all anchors before writing. Never replace an entire potentially newer scenery file.
            for (int i = 0; i < before.Length; i++)
            {
                int first = text.IndexOf(before[i], StringComparison.Ordinal);
                if (first < 0 || text.IndexOf(before[i], first + before[i].Length, StringComparison.Ordinal) >= 0)
                { Debug.LogError("MapScenery framing code differs from the supported version. No changes made. Share the current MapScenery.cs to adapt the patch."); return; }
            }
            for (int i = 0; i < before.Length; i++) text = text.Replace(before[i], after[i]);
            string backup = "Library/AshenVale/BackgroundFramingBackups/" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + "_MapScenery.cs.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            File.Copy(path, backup, false);
            File.WriteAllText(path, text.Replace("\n", newline), new System.Text.UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
            Debug.Log("Background framing fix installed. Stop and restart Play mode to rebuild maps. Backup: " + backup);
        }
    }
}
