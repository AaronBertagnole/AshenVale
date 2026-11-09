using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AshenVale.EditorTools
{
    public static class EmberfieldsRouteInstaller
    {
        const string PathInProject = "Assets/Scripts/Data/MapDefs.cs";
        const string Original = @"        const string EmberfieldsLayout = @""
            ..........................................................................
            ..........................................................................
            ................................c...k...c.................................
            .......c....k...............=================................c..k.........
            ....===========...................|.....................===========.......
            .........|...................b....|......................b................
            .........|..............=============...............===========...........
            .........|.........b............................a.........................
            .........|....===========...................==========....................
            .........|..b.......................a........................b..k.........
            .........|========............============..............==========........
            .A..@....|..a..........a............a..........a............a...........B.
            ##########################################################################"";";
        const string Revised = @"        const string EmberfieldsLayout = @""
            ..........................................................................
            ..........................................................................
            .............................................................c...k........
            ........................................................===========.......
            .....................................c..k............c....................
            .........c...k..................===========......===========..............
            .......========.............b..................b...........|..............
            ..............|.........===========......==========........|..............
            ..............|......a...b.......|.........................|...b...k......
            ..............|============......|...a....................============....
            ..............|b................==========................................
            .A..@.......a======.....a...........a...........a...........a....=======B.
            ##########################################################################"";";

        [MenuItem("AshenVale/Map layouts/Apply Emberfields routes")]
        static void Apply() => ReplaceLayout(Original, Revised, "Emberfields routes applied.");

        [MenuItem("AshenVale/Map layouts/Restore original Emberfields")]
        static void Restore() => ReplaceLayout(Revised, Original, "Original Emberfields restored.");

        static void ReplaceLayout(string before, string after, string success)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                Debug.LogWarning("Exit Play Mode and wait for compilation before changing the layout.");
                return;
            }

            string temporary = null;
            try
            {
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../" + PathInProject));
                string source = File.ReadAllText(path);
                string newline = source.Contains("\r\n") ? "\r\n" : "\n";
                before = before.Replace("\r\n", "\n").Replace("\n", newline);
                after = after.Replace("\r\n", "\n").Replace("\n", newline);
                int index = source.IndexOf(before, StringComparison.Ordinal);
                if (index < 0 && source.Contains(after))
                {
                    Debug.Log("Emberfields already uses the selected layout.");
                    return;
                }
                if (index < 0 || source.Contains(after) ||
                    source.IndexOf(before, index + before.Length, StringComparison.Ordinal) >= 0)
                {
                    Debug.LogError("Emberfields layout differs from the expected version. MapDefs.cs was left intact.");
                    return;
                }

                string updated = source.Substring(0, index) + after + source.Substring(index + before.Length);
                temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllText(temporary, updated);
                File.Replace(temporary, path, null);
                AssetDatabase.Refresh();
                Debug.Log(success);
            }
            catch (Exception exception)
            {
                Debug.LogError("Emberfields layout: " + exception.Message);
            }
            finally
            {
                if (temporary != null && File.Exists(temporary)) File.Delete(temporary);
            }
        }
    }
}
