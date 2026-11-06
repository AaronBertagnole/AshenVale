using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AshenVale.EditorTools
{
    public static class ProneAnimationInstaller
    {
        const string Path = "Assets/Scripts/View/WorldView.cs";
        static string Edit(string text, string before, string after)
        {
            if (text.Contains(after)) return text;
            int i = text.IndexOf(before, StringComparison.Ordinal);
            if (i < 0 || text.IndexOf(before, i + before.Length, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException("Unsupported WorldView near: " + before.Trim());
            return text.Substring(0, i) + after + text.Substring(i + before.Length);
        }
        public static string Patch(string text)
        {
            text = Edit(text, "public bool fittedClimb; }",
                "public bool fittedClimb; public readonly PronePosePlayer prone = new PronePosePlayer(); }");
            text = Edit(text,
                "                v.go.transform.localScale = v.fittedClimb ? v.climbScale : new Vector3(HeroScale, p.crouching ? 1.10f : HeroScale, 1f);",
                "                v.go.transform.localScale = v.fittedClimb ? v.climbScale : new Vector3(HeroScale, HeroScale, 1f);");
            text = Edit(text, "                float heroOffset = p.crouching ? .33f : .48f;",
                "                float heroOffset = .48f; // Prone art shares the idle ground-contact baseline.");
            text = Edit(text, "            string clip = ChooseAnimation(p);",
                "            string clip = ChooseAnimation(p);\n" +
                "            if (v.prone.TryDraw(p, clip, v.sr, frameDt))\n" +
                "            {\n" +
                "                v.anim = \"prone\"; v.clock = 0f;\n" +
                "                v.climbOffset = v.climbXOffset = 0f; v.fittedClimb = false;\n" +
                "                return;\n" +
                "            }");
            return text;
        }
        [MenuItem("AshenVale/Art/Install Prone Animations")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Stop Play mode first."); return; }
            try
            {
                string original = File.ReadAllText(Path);
                string output = Patch(original.Replace("\r\n", "\n"));
                foreach (string hero in new[] { "DragonKnight", "ElvenRanger" })
                    foreach (string clip in new[] { "prone_enter", "prone" })
                    {
                        int expected = clip == "prone_enter" ? 4 : 2;
                        var frames = Resources.LoadAll<Sprite>("Heroes/" + hero + "/" + clip);
                        if (frames.Length != expected)
                            throw new InvalidOperationException("Expected " + expected + " single-sprite PNGs in " + hero + "/" + clip + ". No source changed.");
                    }
                if (original.Replace("\r\n", "\n") == output) { Debug.Log("Prone rendering already installed."); return; }
                string backup = "Library/AshenVale/ProneAnimations/" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
                Directory.CreateDirectory(backup);
                File.WriteAllText(backup + "/WorldView.cs.txt", original);
                File.WriteAllText(Path, output.Replace("\n", original.Contains("\r\n") ? "\r\n" : "\n"), new System.Text.UTF8Encoding(false));
                AssetDatabase.Refresh();
                Debug.Log("Prone installed. Restart Play mode. WorldView backup: " + backup);
            }
            catch (Exception e) { Debug.LogError("Prone installer: " + e.Message); }
        }
    }
}
