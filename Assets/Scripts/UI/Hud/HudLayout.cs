using UnityEngine;

namespace AshenVale
{
    // Where the player has put the HUD's panels and which ones they have closed, saved in PlayerPrefs.
    // CanvasHud applies it; the pause menu's HUD window shows closed panels again and resets the layout.
    public static class HudLayout
    {
        // The panels that can be moved. The last three can also be closed.
        public static readonly string[] Ids = { "status", "actions", "shortcuts", "purse", "minimap", "journey", "log" };
        public static readonly string[] Closable = { "minimap", "journey", "log" };
        public static readonly string[] ClosableNames = { "Local map", "Your journey", "Adventure log" };

        public static int Version { get; private set; }     // goes up whenever something changes: CanvasHud re-applies

        static string Key(string id, string what) { return "AshenVale.Hud." + id + "." + what; }

        public static bool HasPosition(string id) { return PlayerPrefs.HasKey(Key(id, "x")); }
        public static Vector2 Position(string id) { return new Vector2(PlayerPrefs.GetFloat(Key(id, "x")), PlayerPrefs.GetFloat(Key(id, "y"))); }
        public static void SetPosition(string id, Vector2 position)
        {
            PlayerPrefs.SetFloat(Key(id, "x"), position.x); PlayerPrefs.SetFloat(Key(id, "y"), position.y);
            PlayerPrefs.Save(); Version++;
        }

        public static bool Hidden(string id) { return PlayerPrefs.GetInt(Key(id, "hidden"), 0) == 1; }
        public static void SetHidden(string id, bool hidden)
        {
            PlayerPrefs.SetInt(Key(id, "hidden"), hidden ? 1 : 0);
            PlayerPrefs.Save(); Version++;
        }

        // Everything back where it started, and every closed panel shown again.
        public static void Reset()
        {
            foreach (var id in Ids)
            {
                PlayerPrefs.DeleteKey(Key(id, "x")); PlayerPrefs.DeleteKey(Key(id, "y")); PlayerPrefs.DeleteKey(Key(id, "hidden"));
            }
            PlayerPrefs.Save(); Version++;
        }
    }
}
