using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AshenVale
{
    // Reads and writes hero files: <persistentDataPath>/Characters/<id>.json
    // (Windows: %USERPROFILE%\AppData\LocalLow\<company>\<product>\Characters).
    public static class SaveStore
    {
        public static string Folder { get { return Path.Combine(Application.persistentDataPath, "Characters"); } }

        public static string NewId() { return Guid.NewGuid().ToString("N"); }

        static string PathFor(string id) { return Path.Combine(Folder, id + ".json"); }

        // Every readable hero, most recently saved first. Unreadable files are skipped with a warning.
        public static List<CharacterData> LoadAll()
        {
            var list = new List<CharacterData>();
            if (!Directory.Exists(Folder)) return list;
            foreach (var file in Directory.GetFiles(Folder, "*.json"))
            {
                try
                {
                    var data = JsonUtility.FromJson<CharacterData>(File.ReadAllText(file));
                    if (data == null) continue;
                    data.id = Path.GetFileNameWithoutExtension(file);   // the file name is the id
                    list.Add(data);
                }
                catch (Exception e) { Debug.LogWarning("Ashen Vale: could not read save " + file + ": " + e.Message); }
            }
            list.Sort((a, b) => string.CompareOrdinal(b.savedAt ?? "", a.savedAt ?? ""));
            return list;
        }

        // Writes to a temporary file first, so a crash mid-save never leaves a half-written hero.
        public static bool Save(CharacterData data)
        {
            if (data == null || string.IsNullOrEmpty(data.id)) return false;
            try
            {
                Directory.CreateDirectory(Folder);
                string path = PathFor(data.id), temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data, true));
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Ashen Vale: could not save " + data.name + ": " + e.Message);
                return false;
            }
        }

        public static void Delete(string id)
        {
            try { if (File.Exists(PathFor(id))) File.Delete(PathFor(id)); }
            catch (Exception e) { Debug.LogWarning("Ashen Vale: could not delete save " + id + ": " + e.Message); }
        }
    }
}
