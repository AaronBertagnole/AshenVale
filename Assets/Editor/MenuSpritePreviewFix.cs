using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AshenVale.EditorTools
{
    public static class MenuSpritePreviewFix
    {
        const string Source = "Assets/Scripts/UI/FrontMenu.cs";
        const string Before = @"        static void DrawSprite(Rect target, Sprite sprite)
        {
            Rect tex = sprite.textureRect;
            var t = sprite.texture;
            GUI.DrawTextureWithTexCoords(target, t, new Rect(tex.x / t.width, tex.y / t.height, tex.width / t.width, tex.height / t.height), true);
        }";
        const string After = @"        static void DrawSprite(Rect target, Sprite sprite)
        {
            if (sprite == null) return;
            Rect tex = sprite.textureRect, canvas = sprite.rect;
            if (canvas.width <= 0f || canvas.height <= 0f) return;
            Vector2 offset = sprite.textureRectOffset;
            // target fits the ORIGINAL canvas, not the changing trimmed texture region.
            float scale = Mathf.Min(target.width / canvas.width, target.height / canvas.height);
            float left = target.x + (target.width - canvas.width * scale) * .5f;
            float top = target.y + (target.height - canvas.height * scale) * .5f;
            Rect draw = new Rect(left + offset.x * scale,
                top + (canvas.height - offset.y - tex.height) * scale,
                tex.width * scale, tex.height * scale);
            var t = sprite.texture;
            GUI.DrawTextureWithTexCoords(draw, t, new Rect(tex.x / t.width, tex.y / t.height,
                tex.width / t.width, tex.height / t.height), true);
        }";
        public static string Patch(string text)
        {
            if (text.Contains(After)) return text;
            int index = text.IndexOf(Before, StringComparison.Ordinal);
            if (index < 0 || text.IndexOf(Before, index + Before.Length, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException("The menu sprite drawing method differs from the reviewed version. No source changed.");
            return text.Substring(0,index) + After + text.Substring(index + Before.Length);
        }
        static void Write(string original,string result)
        {
            if (original.Replace("\r\n","\n") == result) return;
            string folder = "Library/AshenVale/MenuPreviewFix/" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            Directory.CreateDirectory(folder); File.WriteAllText(folder + "/FrontMenu_before.cs.txt",original);
            File.WriteAllText(Source,result.Replace("\n",original.Contains("\r\n") ? "\r\n" : "\n"),new System.Text.UTF8Encoding(false));
            AssetDatabase.Refresh(); Debug.Log("Menu preview source updated. Backup: " + folder);
        }
        [MenuItem("AshenVale/Art/Menu Preview/Fix Frame Alignment")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Stop Play mode first."); return; }
            try { string original=File.ReadAllText(Source); Write(original,Patch(original.Replace("\r\n","\n"))); }
            catch (Exception e) { Debug.LogError("Menu preview fix: " + e.Message); }
        }
        [MenuItem("AshenVale/Art/Menu Preview/Remove Frame Alignment Fix")]
        public static void Remove()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Stop Play mode first."); return; }
            try
            {
                string original=File.ReadAllText(Source), text=original.Replace("\r\n","\n");
                if (!text.Contains(After)) { Debug.Log("Reviewed alignment fix is not present. No changes."); return; }
                Write(original,text.Replace(After,Before));
            }
            catch (Exception e) { Debug.LogError(e.Message); }
        }
        [MenuItem("AshenVale/Art/Menu Preview/Log Idle Sprite Bounds")]
        public static void LogBounds()
        {
            foreach (var hero in Heroes.All)
                foreach (var sprite in Sprites.Hero(hero.folder + "/idle"))
                    Debug.Log(hero.folder + "/" + sprite.name + " canvas=" + sprite.rect + " texture=" + sprite.textureRect + " offset=" + sprite.textureRectOffset);
        }
    }
}
