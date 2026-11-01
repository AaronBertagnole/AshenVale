using System.Collections.Generic;
using UnityEngine;

namespace AshenVale
{
    public enum MapKind { Field, Town }

    // One-way platform: you land on its top when falling from above, and Down + Jump drops you through.
    public struct Platform
    {
        public float left, right, top;
        public Platform(float left, float right, float top) { this.left = left; this.right = right; this.top = top; }
    }

    public struct Ladder
    {
        public float x, bottom, top;
        public Ladder(float x, float bottom, float top) { this.x = x; this.bottom = bottom; this.top = top; }
    }

    // Press Up at a portal to come out of the portal marked targetKey on targetMap.
    public struct Portal
    {
        public char key, targetKey;
        public int targetMap;
        public float x, y;
    }

    // Where one monster lives: it appears at (x, y) and keeps to its own floor, between left and right.
    public struct SpawnPoint
    {
        public int monster;
        public float x, y, left, right;
    }

    // Gameplay layout of one map, read from a text picture (see MapLayout). Its look is built by MapScenery from `theme`.
    public sealed class MapDef
    {
        public int id;
        public string name, theme;
        public MapKind kind;
        public int columns, rows;               // size in cells; one cell is one world unit
        public float minX, maxX;                // how far players can walk
        public float startX, startY;            // @  where you arrive on a new hero, after loading, or after waking up
        public float merchantX, merchantY;      // $  towns
        public float waystoneX, waystoneY;      // ^  towns: travel to other towns you have found
        public float returnX, returnY;          // &  towns: town portals (T) and waking up arrive here; it leads back out
        public Platform[] platforms = new Platform[0];
        public Ladder[] ladders = new Ladder[0];
        public Portal[] portals = new Portal[0];
        public SpawnPoint[] spawns = new SpawnPoint[0];
        public int minLevel, maxLevel;          // levels of the monsters here (0 in towns; chests don't count)

        public float Bottom { get { return Tuning.Ground; } }
        public float Top { get { return Tuning.Ground + rows; } }
        public string LevelText
        {
            get { return kind == MapKind.Town || maxLevel <= 0 ? "Town" : minLevel == maxLevel ? "Lv " + minLevel : "Lv " + minLevel + "-" + maxLevel; }
        }

        public bool FindPortal(char key, out Portal portal)
        {
            foreach (var p in portals) if (p.key == key) { portal = p; return true; }
            portal = new Portal();
            return false;
        }
    }

    // Reads a map from a text picture, one character per cell, bottom line = the ground:
    //   #      ground (the bottom line)
    //   =      one-way platform; a platform on line k (counting the ground as 0) has its top k units above the ground
    //   |      ladder: from the floor under its lowest cell up to the floor above its highest cell
    //   .      empty
    // Markers stand in the cell just above the floor they are on:
    //   @ start   $ merchant   ^ waystone   & town return portal
    //   A-Z portal (linked by letter, see Link)   a-z monster spawn point (see Spawn)
    // A jump reaches about 2.4 units, so a platform 2 lines up can be jumped to; 3 or more needs a ladder.
    // Layouts were checked with a script (every floor reachable, every portal two-way) before going in.
    public static class MapLayout
    {
        public struct Spawn { public char mark; public int monster; }
        public struct Link { public char key, targetKey; public int targetMap; }

        public static Spawn S(char mark, int monster) { return new Spawn { mark = mark, monster = monster }; }
        public static Link L(char key, int targetMap, char targetKey) { return new Link { key = key, targetMap = targetMap, targetKey = targetKey }; }

        public static MapDef Build(int id, string name, string theme, MapKind kind, string text, Spawn[] legend, Link[] links)
        {
            var lines = new List<string>();
            foreach (var raw in text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length > 0) lines.Add(line);
            }
            int rows = lines.Count, cols = 0;
            foreach (var line in lines) cols = Mathf.Max(cols, line.Length);
            var grid = new char[rows, cols];                 // grid[row, col], row 0 = ground
            for (int r = 0; r < rows; r++)
            {
                string line = lines[rows - 1 - r];
                for (int c = 0; c < cols; c++) grid[r, c] = c < line.Length ? line[c] : '.';
            }

            const float G = Tuning.Ground;
            var def = new MapDef
            {
                id = id, name = name, theme = theme, kind = kind, columns = cols, rows = rows,
                minX = .5f, maxX = cols - .5f, startX = cols * .5f, startY = G,
            };

            var platforms = new List<Platform>();
            for (int r = 1; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    if (grid[r, c] != '=') continue;
                    int start = c;
                    while (c < cols && grid[r, c] == '=') c++;
                    platforms.Add(new Platform(start, c, G + r));
                }
            def.platforms = platforms.ToArray();

            var ladders = new List<Ladder>();
            for (int c = 0; c < cols; c++)
                for (int r = 1; r < rows; r++)
                {
                    if (grid[r, c] != '|') continue;
                    int low = r;
                    while (r < rows && grid[r, c] == '|') r++;
                    ladders.Add(new Ladder(c + .5f, G + low - 1, G + r));
                }
            def.ladders = ladders.ToArray();

            var portals = new List<Portal>();
            var spawns = new List<SpawnPoint>();
            for (int r = 1; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    char ch = grid[r, c];
                    if (ch == '.' || ch == '=' || ch == '#' || ch == '|') continue;
                    int floor = r - 1;
                    while (floor > 0 && grid[floor, c] != '=') floor--;
                    float x = c + .5f, y = G + floor;
                    if (ch == '@') { def.startX = x; def.startY = y; }
                    else if (ch == '$') { def.merchantX = x; def.merchantY = y; }
                    else if (ch == '^') { def.waystoneX = x; def.waystoneY = y; }
                    else if (ch == '&') { def.returnX = x; def.returnY = y; }
                    else if (ch >= 'A' && ch <= 'Z')
                    {
                        bool linked = false;
                        foreach (var link in links)
                            if (link.key == ch)
                            {
                                portals.Add(new Portal { key = ch, targetKey = link.targetKey, targetMap = link.targetMap, x = x, y = y });
                                linked = true;
                            }
                        if (!linked) Debug.LogWarning(name + ": portal " + ch + " leads nowhere");
                    }
                    else if (ch >= 'a' && ch <= 'z')
                    {
                        int monster = -1;
                        foreach (var entry in legend) if (entry.mark == ch) monster = entry.monster;
                        if (monster < 0) { Debug.LogWarning(name + ": no monster for spawn mark " + ch); continue; }
                        var point = new SpawnPoint { monster = monster, x = x, y = y };
                        if (floor == 0)
                        {
                            // Ground monsters roam a stretch around their spawn point, so they stay spread out.
                            point.left = Mathf.Max(def.minX, x - Tuning.GroundRoam);
                            point.right = Mathf.Min(def.maxX, x + Tuning.GroundRoam);
                        }
                        else
                        {
                            point.left = x - .5f; point.right = x + .5f;
                            foreach (var p in def.platforms)
                                if (Mathf.Abs(p.top - y) < .01f && x >= p.left && x <= p.right) { point.left = p.left; point.right = p.right; }
                        }
                        spawns.Add(point);
                        var m = Monsters.All[monster];
                        if (!m.harmless)
                        {
                            def.minLevel = def.minLevel == 0 ? m.level : Mathf.Min(def.minLevel, m.level);
                            def.maxLevel = Mathf.Max(def.maxLevel, m.level);
                        }
                    }
                }
            // Keep ground monsters a little way from portals on the ground, so arriving on a map is calm.
            for (int i = 0; i < spawns.Count; i++)
            {
                var point = spawns[i];
                if (point.y > G + .01f) continue;
                foreach (var portal in portals)
                {
                    if (portal.y > G + .01f) continue;
                    if (portal.x < point.x) point.left = Mathf.Min(point.x, Mathf.Max(point.left, portal.x + Tuning.PortalCalm));
                    else point.right = Mathf.Max(point.x, Mathf.Min(point.right, portal.x - Tuning.PortalCalm));
                }
                spawns[i] = point;
            }
            def.portals = portals.ToArray();
            def.spawns = spawns.ToArray();
            return def;
        }
    }

    public static class Maps
    {
        // Map ids are stored in save files: never renumber, only add.
        public const int Emberfields = 0, Mosswood = 1, Emberhaven = 2, HollowDen = 3, Kingsroad = 4, Cinderford = 5, AshenMarsh = 6;
        public const int FirstTown = Emberhaven;    // every hero knows it; where you wake if nothing else is known

        // The world so far (west to east):
        //   Emberhaven (town) - Emberfields - Mosswood - Kingsroad - Cinderford (town) - Ashen Marsh
        //                                        '- secret ledge, top left: Hollow Den
        // In id order: All[i].id == i.
        public static readonly MapDef[] All =
        {
            MapLayout.Build(Emberfields, "Emberfields", "meadow", MapKind.Field, EmberfieldsLayout,
                new[] { MapLayout.S('a', Monsters.Emberling), MapLayout.S('b', Monsters.AshSprout),
                        MapLayout.S('c', Monsters.EliteEmberling), MapLayout.S('k', Monsters.OldChest) },
                new[] { MapLayout.L('A', Emberhaven, 'A'), MapLayout.L('B', Mosswood, 'B') }),
            MapLayout.Build(Mosswood, "Mosswood", "forest", MapKind.Field, MosswoodLayout,
                new[] { MapLayout.S('a', Monsters.MossCrawler), MapLayout.S('b', Monsters.BrambleBoar), MapLayout.S('k', Monsters.MossyChest) },
                new[] { MapLayout.L('B', Emberfields, 'B'), MapLayout.L('C', Kingsroad, 'C'), MapLayout.L('S', HollowDen, 'S') }),
            MapLayout.Build(Emberhaven, "Emberhaven", "town", MapKind.Town, EmberhavenLayout,
                new MapLayout.Spawn[0],
                new[] { MapLayout.L('A', Emberfields, 'A') }),
            MapLayout.Build(HollowDen, "Hollow Den", "cave", MapKind.Field, HollowDenLayout,
                new[] { MapLayout.S('d', Monsters.DenShade), MapLayout.S('k', Monsters.MossyChest) },
                new[] { MapLayout.L('S', Mosswood, 'S') }),
            MapLayout.Build(Kingsroad, "Kingsroad", "road", MapKind.Field, KingsroadLayout,
                new[] { MapLayout.S('b', Monsters.BrambleBoar), MapLayout.S('v', Monsters.VioletWraith) },
                new[] { MapLayout.L('C', Mosswood, 'C'), MapLayout.L('D', Cinderford, 'D') }),
            MapLayout.Build(Cinderford, "Cinderford", "rivertown", MapKind.Town, CinderfordLayout,
                new MapLayout.Spawn[0],
                new[] { MapLayout.L('D', Kingsroad, 'D'), MapLayout.L('E', AshenMarsh, 'E') }),
            MapLayout.Build(AshenMarsh, "Ashen Marsh", "marsh", MapKind.Field, AshenMarshLayout,
                new[] { MapLayout.S('t', Monsters.MarshToad), MapLayout.S('l', Monsters.BogLurker),
                        MapLayout.S('w', Monsters.AshWisp), MapLayout.S('k', Monsters.SunkenChest) },
                new[] { MapLayout.L('E', Cinderford, 'E') }),
        };

        public static bool IsTown(int map) { return map >= 0 && map < All.Length && All[map].kind == MapKind.Town; }

        const string EmberfieldsLayout = @"
            ..........................................................................
            ..........................................................................
            ................................c.......c.................................
            .......c....k...............=================................c............
            ....===========...................|.....................===========.......
            .........|...................b....|......................b................
            .........|..............=============...............===========...........
            .........|.........b............................a.........................
            .........|....===========...................==========....................
            .........|..b.......................a........................b............
            .........|========............============..............==========........
            .A..@....|..a..........a............a..........a............a...........B.
            ##########################################################################";

        const string MosswoodLayout = @"
            ..................................................................................
            .....S............................................................................
            ...=========......................................................................
            ..........|...................b.........b.........................................
            ..........|...............===================.....................................
            ..........|.....b.........................|.............b......k..................
            ........===============...................|.........===============...............
            ............|.............................|.b...............|.....................
            ............|.........................=============.........|.....................
            ..........a.|.....................b.........................|...a.................
            ....===========.............=============.................=============...........
            ........|...............a.........................b...............................
            ........|.........=============.............=============.........................
            ........|......a........................a..........................a..............
            ........|.===========.............=============...............===========.........
            .B..@...|.....a...........a.............b...........a.............a.........b...C.
            ##################################################################################";

        const string EmberhavenLayout = @"
            ..........................................................
            ..........................................................
            ..........................................................
            ..........................................................
            ..........................................................
            ........................^.........$.......................
            ..................=======================.................
            ...@....&..............................................A..
            ##########################################################";

        const string HollowDenLayout = @"
            ..................................................
            ......................d....k......................
            ..................=============...................
            ........k...............|...................k.....
            ....=========...........|...........===========...
            ........|...........d...|.....d...........|.......
            ........|.......=================.........|.......
            ......====..d.....................d.....=====.....
            ........===========...........===========.........
            ..S..@........d...........d.........d.......d.....
            ##################################################";

        const string KingsroadLayout = @"
            ............................................................................................
            ............................................v......v........................................
            ........................................===============.....................................
            ....................................b.............................v.........................
            ..............................=============.................=============...................
            ..........................v.............................b...................................
            ....................=============.................=============.............................
            .................b............................v............................b................
            ............===========.................=============.................===========...........
            .C..@...........b.............b..............v............b...............v.........b.....D.
            ############################################################################################";

        const string CinderfordLayout = @"
            ..............................................................
            ..............................................................
            ..............................................................
            ..............................................................
            .................................^............................
            ............................===========.......................
            .........................$....................................
            ......................=======================.................
            ..D...@....&...............................................E..
            ##############################################################";

        const string AshenMarshLayout = @"
            ....................................................................................
            ........................w.......w...................................................
            ....................===============...............................w...l.............
            ..............................|...........................=================.........
            .......k......................|...........w...................|.....................
            ....===========...............|.....=============.............|.....................
            ..........|...................|.w.........................l...|.....................
            ..........|...............=============.............=============...................
            ..........|...........l.........................t........................w..........
            ..........|.....=============.............=============.............===========.....
            ..........|..t........................t..........................l..................
            ..........|========.............=============...............===========.............
            .E..@.....|.t...........t.............l...........t.............l.............t.....
            ####################################################################################";
    }
}
