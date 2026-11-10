using System;
using UnityEngine;

namespace AshenVale
{
    public enum GameAction { Left, Right, Up, Down, Jump, Attack, Skill, Dash, Interact, TownPortal, HpPotion, MpPotion, Inventory, WorldMap, Minimap, Loot, Abilities }

    // Key bindings: two keys per action (either works), saved on this PC (PlayerPrefs) and changed from the
    // Controls window in the Esc menu. Everything that reads the keyboard or names a key goes through here.
    public static class Keys
    {
        public const int Slots = 2;
        static readonly KeyCode[,] defaults =
        {
            { KeyCode.A, KeyCode.LeftArrow }, { KeyCode.D, KeyCode.RightArrow }, { KeyCode.W, KeyCode.UpArrow }, { KeyCode.S, KeyCode.DownArrow },
            { KeyCode.Space, KeyCode.None }, { KeyCode.J, KeyCode.None }, { KeyCode.K, KeyCode.None }, { KeyCode.L, KeyCode.LeftShift },
            { KeyCode.E, KeyCode.None }, { KeyCode.T, KeyCode.None }, { KeyCode.Q, KeyCode.None }, { KeyCode.F, KeyCode.None },
            { KeyCode.I, KeyCode.None }, { KeyCode.M, KeyCode.None }, { KeyCode.Tab, KeyCode.None }, { KeyCode.E, KeyCode.None },
            { KeyCode.P, KeyCode.None },
        };
        static readonly string[] names =
        {
            "Move left", "Move right", "Up / climb / enter portal", "Down / climb / crouch", "Jump (Down + Jump drops through)", "Attack", "Special attack",
            "Dash", "Talk / use", "Portal back to town", "HP potion", "MP potion", "Inventory", "World map", "Show / hide minimap", "Loot",
            "Abilities",
        };
        static KeyCode[,] bound;
        public static int Count { get { return defaults.GetLength(0); } }

        static void Load()
        {
            if (bound != null) return;
            bound = new KeyCode[Count, Slots];
            for (int a = 0; a < Count; a++)
                for (int s = 0; s < Slots; s++)
                {
                    string saved = PlayerPrefs.GetString(Pref(a, s), "");
                    KeyCode key;
                    bound[a, s] = saved.Length > 0 && Enum.TryParse(saved, out key) ? key : defaults[a, s];
                    // Loot used to share the old "Loot / talk / use" key: until it is set on its own, it follows Talk / use.
                    if (a == (int)GameAction.Loot && saved.Length == 0) bound[a, s] = bound[(int)GameAction.Interact, s];
                }
        }

        static string Pref(int action, int slot) { return "key." + (GameAction)action + "." + slot; }

        public static KeyCode Get(GameAction action, int slot) { Load(); return bound[(int)action, slot]; }

        public static void Set(GameAction action, int slot, KeyCode key)
        {
            Load();
            // A key does one thing: take it away from wherever else it was bound.
            if (key != KeyCode.None)
                for (int a = 0; a < Count; a++)
                    for (int s = 0; s < Slots; s++)
                        if (bound[a, s] == key && (a != (int)action || s != slot)) { bound[a, s] = KeyCode.None; PlayerPrefs.SetString(Pref(a, s), "None"); }
            bound[(int)action, slot] = key;
            PlayerPrefs.SetString(Pref((int)action, slot), key.ToString());
            PlayerPrefs.Save();
        }

        public static void ResetAll()
        {
            Load();
            for (int a = 0; a < Count; a++)
                for (int s = 0; s < Slots; s++) { bound[a, s] = defaults[a, s]; PlayerPrefs.DeleteKey(Pref(a, s)); }
            PlayerPrefs.Save();
        }

        public static bool Held(GameAction action, bool ignoreMouse = false)
        {
            Load();
            for (int s = 0; s < Slots; s++) { var k = bound[(int)action, s]; if (k != KeyCode.None && (!ignoreMouse || !IsMouse(k)) && Input.GetKey(k)) return true; }
            return false;
        }

        public static bool Pressed(GameAction action, bool ignoreMouse = false)
        {
            Load();
            for (int s = 0; s < Slots; s++) { var k = bound[(int)action, s]; if (k != KeyCode.None && (!ignoreMouse || !IsMouse(k)) && Input.GetKeyDown(k)) return true; }
            return false;
        }

        static bool IsMouse(KeyCode key) { return (int)key >= (int)KeyCode.Mouse0 && (int)key <= (int)KeyCode.Mouse6; }

        public static string Describe(GameAction action) { return names[(int)action]; }

        // "E", "SPACE", "A / LEFT": the keys for an action, for labels and hints.
        public static string Name(GameAction action)
        {
            Load();
            string text = "";
            for (int s = 0; s < Slots; s++)
            {
                var k = bound[(int)action, s];
                if (k == KeyCode.None) continue;
                text += (text.Length > 0 ? " / " : "") + KeyName(k);
            }
            return text.Length > 0 ? text : "unbound";
        }

        public static string KeyName(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.None: return "-";
                case KeyCode.LeftArrow: return "LEFT"; case KeyCode.RightArrow: return "RIGHT"; case KeyCode.UpArrow: return "UP"; case KeyCode.DownArrow: return "DOWN";
                case KeyCode.LeftShift: return "L SHIFT"; case KeyCode.RightShift: return "R SHIFT"; case KeyCode.LeftControl: return "L CTRL"; case KeyCode.RightControl: return "R CTRL";
                case KeyCode.LeftAlt: return "L ALT"; case KeyCode.RightAlt: return "R ALT"; case KeyCode.Space: return "SPACE"; case KeyCode.Return: return "ENTER";
                case KeyCode.Mouse0: return "L MOUSE"; case KeyCode.Mouse1: return "R MOUSE"; case KeyCode.Mouse2: return "M MOUSE";
            }
            string s = key.ToString();
            if (s.StartsWith("Alpha") && s.Length == 6) return s.Substring(5);
            if (s.StartsWith("Keypad")) return "NUM " + s.Substring(6).ToUpperInvariant();
            return s.ToUpperInvariant();
        }

        // The key just pressed this frame, for rebinding (Escape cancels, so it is never returned).
        public static KeyCode AnyKeyDown()
        {
            if (!Input.anyKeyDown) return KeyCode.None;
            foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
                if (k != KeyCode.None && k != KeyCode.Escape && Input.GetKeyDown(k)) return k;
            return KeyCode.None;
        }
    }
}

