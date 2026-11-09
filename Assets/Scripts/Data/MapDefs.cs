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
        public float x, y, left, right;          // where it wanders (ground ones keep clear of portals)
        public float chaseLeft, chaseRight;      // where it may go after a player it has noticed: the whole stretch
    }

    // Gameplay layout of one map, read from a text picture (see MapLayout). Its look is built by MapScenery from `theme`.
    public sealed class MapDef
    {
        public int id;
        public string name, theme;
        public string key;                      // file-friendly name, e.g. "amberleaf_trail" (Art/Backdrops/<key>.png is used if present)
        public MapKind kind;
        public int columns, rows;               // size in cells; one cell is one world unit
        public float minX, maxX;                // how far players can walk
        public float startX, startY;            // @  where you arrive on a new hero, after loading, or after waking up
        public Vector2[] merchants = new Vector2[0];    // $  towns: every merchant stand (a town may have several)
        public float waystoneX, waystoneY;      // ^  towns: travel to other towns you have found
        public float returnX, returnY;          // &  towns: town portals (T) and waking up arrive here; it leads back out
        public float smithyX, smithyY;          // %  towns with a smithy: upgrade gear with monster parts
        public bool hasSmithy;
        // Optional painted scene (Resources/<scene>.png) drawn behind the whole map instead of the theme's
        // placeholder backdrop. It is stretched to the map's width; sceneFloor is how far up the picture
        // (as a share of its height) the walking line is, so feet land on the painted street. If <scene>_back.png
        // exists, the scene is the see-through front and that picture is the distance sliding behind it (MapArt).
        public string scene;
        public float sceneFloor;
        // Optional painted backdrop (Resources/<backdrop>.png) for a map wider than the picture: shown backdropWidth
        // units wide behind everything, it slides slower than the map as the camera moves (always filling the view),
        // and at the lowest camera its painted path (backdropFloor, as for scenes) lines up with the ground.
        // As for scenes, <backdrop>_back.png (if present) is a more distant layer that slides more slowly still.
        public string backdrop;
        public float backdropFloor, backdropWidth;
        public bool backdropFlip;               // show it mirrored (to reuse a neighbour's picture until the map has its own)
        public Platform[] platforms = new Platform[0];
        public Ladder[] ladders = new Ladder[0];
        public Portal[] portals = new Portal[0];
        public SpawnPoint[] spawns = new SpawnPoint[0];  // monster spawn points (lowercase letters in the layouts)
        public int minLevel, maxLevel;          // levels of the monsters here (0 in towns; chests don't count)
        public bool hasBoss;                    // a boss lives here (its glade)

        public float Bottom { get { return Tuning.Ground; } }
        public float Top { get { return Tuning.Ground + rows; } }
        public string LevelText
        {
            get { return kind == MapKind.Town || maxLevel <= 0 ? "Town" : hasBoss ? "Boss Lv " + maxLevel : minLevel == maxLevel ? "Lv " + minLevel : "Lv " + minLevel + "-" + maxLevel; }
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
    //   @ start   $ merchant (one or more)   ^ waystone   & town return portal   % smithy
    //   A-Z portal (linked by letter, see Link)   a-z monster spawn point (see Spawn)
    // A jump reaches about 2.4 units, so a platform 2 lines up can be jumped to; 3 or more needs a ladder.
    // Layouts were checked with a script (every floor reachable, every portal two-way) before going in.
    public static class MapLayout
    {
        public struct Spawn { public char mark; public int monster; }
        public struct Link { public char key, targetKey; public int targetMap; }

        public static Spawn S(char mark, int monster) { return new Spawn { mark = mark, monster = monster }; }
        public static Link L(char key, int targetMap, char targetKey) { return new Link { key = key, targetMap = targetMap, targetKey = targetKey }; }

        // Gives a map a painted backdrop (see MapDef.backdrop).
        public static MapDef WithBackdrop(MapDef def, string backdrop, float floor, float width, bool flip = false)
        {
            def.backdrop = backdrop;
            def.backdropFloor = floor;
            def.backdropWidth = width;
            def.backdropFlip = flip;
            return def;
        }


        // Gives a map a painted scene (see MapDef.scene).
        public static MapDef WithScene(MapDef def, string scene, float floor)
        {
            def.scene = scene;
            def.sceneFloor = floor;
            return def;
        }

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
                key = name.ToLowerInvariant().Replace(' ', '_'),
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
            var merchants = new List<Vector2>();
            for (int r = 1; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    char ch = grid[r, c];
                    if (ch == '.' || ch == '=' || ch == '#' || ch == '|') continue;
                    int floor = r - 1;
                    while (floor > 0 && grid[floor, c] != '=') floor--;
                    float x = c + .5f, y = G + floor;
                    if (ch == '@') { def.startX = x; def.startY = y; }
                    else if (ch == '$') merchants.Add(new Vector2(x, y));
                    else if (ch == '^') { def.waystoneX = x; def.waystoneY = y; }
                    else if (ch == '&') { def.returnX = x; def.returnY = y; }
                    else if (ch == '%') { def.smithyX = x; def.smithyY = y; def.hasSmithy = true; }
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
                            // Ground monsters roam a stretch around their spawn point, so they stay spread out;
                            // a boss has the run of the whole floor, portal included: nowhere to hide in its glade.
                            float roam = Monsters.All[monster].boss ? def.columns : Tuning.GroundRoam;
                            point.left = Mathf.Max(def.minX, x - roam);
                            point.right = Mathf.Min(def.maxX, x + roam);
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
                            if (m.boss) def.hasBoss = true;
                        }
                    }
                }
            // Keep ground monsters a little way from portals on the ground, so arriving on a map is calm (not bosses).
            for (int i = 0; i < spawns.Count; i++)
            {
                var point = spawns[i];
                point.chaseLeft = point.left; point.chaseRight = point.right;
                spawns[i] = point;
                if (point.y > G + .01f || Monsters.All[point.monster].boss) continue;
                foreach (var portal in portals)
                {
                    if (portal.y > G + .01f) continue;
                    if (portal.x < point.x) point.left = Mathf.Min(point.x, Mathf.Max(point.left, portal.x + Tuning.PortalCalm));
                    else point.right = Mathf.Max(point.x, Mathf.Min(point.right, portal.x - Tuning.PortalCalm));
                }
                // Boxed in between two portals (Mosswood's east boar): a stretch that narrow pins the monster to one
                // spot forever, so it gets its whole stretch back instead.
                if (point.right - point.left < 3f) { point.left = point.chaseLeft; point.right = point.chaseRight; }
                spawns[i] = point;
            }
            def.portals = portals.ToArray();
            def.spawns = spawns.ToArray();
            def.merchants = merchants.ToArray();
            return def;
        }
    }

    public static partial class Maps
    {
        // Map ids are stored in save files: never renumber, only add.
        public const int Emberfields = 0, Mosswood = 1, Emberhaven = 2, HollowDen = 3, Kingsroad = 4, Cinderford = 5, AshenMarsh = 6,
                         AmberleafTrail = 7, MossgateHollow = 8, ShaleCliffs = 9, ThornGlade = 10,
                         // Region 3, Highspire (Maps/Highspire.cs): dwarves in the snowy mountains above Shale Cliffs
                         FrostpinePass = 11, Highspire = 12, GlacierSteps = 13, WindwardRidge = 14, IronveinMines = 15,
                         DeepForge = 16, CrystalHollow = 17, ColossusVault = 18,
                         // Region 4, the Saltmarrow coast (Maps/Saltmarrow.cs): pirate beaches east of the Ashen Marsh
                         BrackwaterShore = 19, Saltmarrow = 20, WreckersReef = 21, SmugglersCove = 22, BlackGalleon = 23,
                         GallowsPoint = 24, SirensGrotto = 25, CaptainsHold = 26,
                         // Region 5, the deep forest (Maps/Eldermere.cs): the magicians' woods north of Saltmarrow
                         ThistledownTrail = 27, Eldermere = 28, Glimmerwood = 29, HollowLibrary = 30, Owlkeep = 31,
                         SpellbrokenGlade = 32, Moonwell = 33, ArchmageSanctum = 34,
                         // Region 6, the ruins (Maps/Lastlight.cs): the city under the forest that was only a story
                         ForgottenStair = 35, Lastlight = 36, SunkenAvenue = 37, HallOfIdols = 38, WeepingCatacombs = 39,
                         BlackZiggurat = 40, Oubliette = 41, SleepersThrone = 42;
        public const int FirstTown = Emberhaven;    // every hero knows it; where you wake if nothing else is known

        // The world so far (west to east):
        //   Emberhaven (town) - Emberfields - Amberleaf Trail - Mossgate Hollow - Mosswood - Kingsroad - Cinderford (town) - Ashen Marsh
        //                                                        '- top ledge: Shale Cliffs     '- secret ledge, top left: Hollow Den
        //                                                           (up the mountain)          '- east end: Thornmother's Glade (boss)
        // In id order: All[i].id == i. Later regions live in Data/Maps/<Region>.cs and are appended here.
        public static MapDef[] All { get { return all ?? (all = Assemble()); } }
        static MapDef[] all;

        static MapDef[] Assemble()
        {
            var list = new List<MapDef>(Core);
            list.AddRange(HighspireMaps());
            list.AddRange(SaltmarrowMaps());
            list.AddRange(EldermereMaps());
            list.AddRange(LastlightMaps());
            for (int i = 0; i < list.Count; i++)
                if (list[i].id != i) Debug.LogError("Maps.All is out of order at " + i + ": " + list[i].name + " has id " + list[i].id);
            return list.ToArray();
        }

        static readonly MapDef[] Core =
        {
            MapLayout.WithBackdrop(MapLayout.Build(Emberfields, "Emberfields", "autumn", MapKind.Field, EmberfieldsLayout,
                    new[] { MapLayout.S('a', Monsters.HedgeShrew), MapLayout.S('b', Monsters.AshSprout),
                            MapLayout.S('c', Monsters.ThornbackHedgehog) },
                    new[] { MapLayout.L('A', Emberhaven, 'A'), MapLayout.L('B', AmberleafTrail, 'B') }),
                "Art/Backdrops/emberfields", 116f / 724f, 42f),   // path 116 px above the bottom of the 724 px picture
            MapLayout.WithBackdrop(MapLayout.Build(Mosswood, "Mosswood", "forest", MapKind.Field, MosswoodLayout,
                    new[] { MapLayout.S('a', Monsters.MossCrawler), MapLayout.S('b', Monsters.BrambleBoar) },
                    new[] { MapLayout.L('B', MossgateHollow, 'B'), MapLayout.L('C', Kingsroad, 'C'), MapLayout.L('S', HollowDen, 'S'),
                            MapLayout.L('T', ThornGlade, 'T') }),
                "Art/Backdrops/mosswood", 130f / 724f, 42f),       // its path is a little higher: 130 px above the bottom
            MapLayout.WithScene(MapLayout.Build(Emberhaven, "Emberhaven", "town", MapKind.Town, EmberhavenLayout,
                    new MapLayout.Spawn[0],
                    new[] { MapLayout.L('A', Emberfields, 'A') }),
                "Art/Scenes/emberhaven", 116f / 724f),     // the painted street is 116 px above the bottom of the 724 px picture
            MapLayout.Build(HollowDen, "Hollow Den", "hollowden", MapKind.Field, HollowDenLayout,
                    new[] { MapLayout.S('d', Monsters.DenShade) },
                    new[] { MapLayout.L('S', Mosswood, 'S') }),
            MapLayout.WithBackdrop(MapLayout.Build(Kingsroad, "Kingsroad", "road", MapKind.Field, KingsroadLayout,
                    new[] { MapLayout.S('b', Monsters.BrambleBoar), MapLayout.S('v', Monsters.VioletWraith) },
                    new[] { MapLayout.L('C', Mosswood, 'C'), MapLayout.L('D', Cinderford, 'D') }),
                "Art/Backdrops/kingsroad", 140f / 724f, 42f),      // its path is higher: 140 px above the bottom
            MapLayout.WithScene(MapLayout.Build(Cinderford, "Cinderford", "rivertown", MapKind.Town, CinderfordLayout,
                    new MapLayout.Spawn[0],
                    new[] { MapLayout.L('D', Kingsroad, 'D'), MapLayout.L('E', AshenMarsh, 'E') }),
                "Art/Scenes/cinderford", 144f / 724f),     // two pictures joined into one long street, 144 px above the bottom
            MapLayout.Build(AshenMarsh, "Ashen Marsh", "marsh", MapKind.Field, AshenMarshLayout,
                new[] { MapLayout.S('t', Monsters.MarshToad), MapLayout.S('l', Monsters.BogLurker),
                        MapLayout.S('w', Monsters.AshWisp) },
                new[] { MapLayout.L('E', Cinderford, 'E'), MapLayout.L('P', BrackwaterShore, 'P') }),
            // The path from Emberfields down toward Mosswood. Until Amberleaf Trail has its own picture
            // (Art/Backdrops/amberleaf_trail.png), it shows the Emberfields forest mirrored.
            MapLayout.WithBackdrop(MapLayout.Build(AmberleafTrail, "Amberleaf Trail", "autumn", MapKind.Field, AmberleafTrailLayout,
                    new[] { MapLayout.S('a', Monsters.AshSprout), MapLayout.S('b', Monsters.LeafBeetle),
                            MapLayout.S('f', Monsters.EmberFox) },
                    new[] { MapLayout.L('B', Emberfields, 'B'), MapLayout.L('F', MossgateHollow, 'F') }),
                "Art/Backdrops/emberfields", 116f / 724f, 42f, true),
            MapLayout.Build(MossgateHollow, "Mossgate Hollow", "mossgate", MapKind.Field, MossgateHollowLayout,
                new[] { MapLayout.S('m', Monsters.Mossling), MapLayout.S('f', Monsters.EmberFox),
                        MapLayout.S('c', Monsters.MossCrawler) },
                new[] { MapLayout.L('F', AmberleafTrail, 'F'), MapLayout.L('B', Mosswood, 'B'), MapLayout.L('G', ShaleCliffs, 'G') }),
            // Up the mountain from Mossgate Hollow's top ledge: a steep climb of rock shelves to a summit lookout.
            // The way on to Highspire will open from the summit later.
            MapLayout.Build(ShaleCliffs, "Shale Cliffs", "cliffs", MapKind.Field, ShaleCliffsLayout,
                new[] { MapLayout.S('g', Monsters.CragGoat), MapLayout.S('r', Monsters.RockMite) },
                new[] { MapLayout.L('G', MossgateHollow, 'G'), MapLayout.L('H', FrostpinePass, 'H') }),
            // Region 1's boss glade, off the east end of Mosswood: a bramble-walled clearing with two ledges to
            // shoot from. Thornmother waits in the middle and comes back a few minutes after she falls.
            MapLayout.WithBackdrop(MapLayout.Build(ThornGlade, "Thornmother's Glade", "forest", MapKind.Field, ThornGladeLayout,
                    new[] { MapLayout.S('z', Monsters.Thornmother) },
                    new[] { MapLayout.L('T', Mosswood, 'T') }),
                "Art/Backdrops/mosswood", 130f / 724f, 42f),       // Mosswood's pictures until the glade has its own
        };

        public static bool IsTown(int map) { return map >= 0 && map < All.Length && All[map].kind == MapKind.Town; }

        const string EmberfieldsLayout = @"
            ..........................................................................
            ..........................................................................
            ................................c.......c.................................
            .......c....................=================................c............
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
            ..........|.....b.........................|.............b.........................
            ........===============...................|.........===============...............
            ............|.............................|.b...............|.....................
            ............|.........................=============.........|.....................
            ..........a.|.....................b.........................|...a.................
            ....===========.............=============.................=============...........
            ........|...............a.........................b...............................
            ........|.........=============.............=============.........................
            ........|......a........................a..........................a..............
            ........|.===========.............=============...............===========.........
            .B..@...|.....a...........a.............b...........a.............a...T.....b...C.
            ##################################################################################";

        // Matches the painted scene: merchant at the house on the left, waystone at the arch, smithy at the forge.
        const string EmberhavenLayout = @"
            ......................................
            ......................................
            ......................................
            ......................................
            ......................................
            ......................................
            ......................................
            .&.....$.....@....^............%....A.
            ######################################";

        const string HollowDenLayout = @"
            ..................................................
            ......................d...........................
            ..................=============...................
            ........................|.........................
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

        // Matches the painted street, west to east: the Kingsroad gate, the potion shop and the outfitter (both
        // merchants), the smithy's forge, the waystone arch, and the east gate out to Ashen Marsh.
        const string CinderfordLayout = @"
            ........................................................................
            ........................................................................
            ........................................................................
            ........................................................................
            ........................................................................
            ........................................................................
            ........................................................................
            ....D...........$.......$........%......@...&........^............E.....
            ########################################################################";

        // Shale Cliffs: arrive at the bottom left, climb the shelves and ladders back and forth to the summit at the top
        // right, where a chest may wait. Goats charge along the shelves; mites cling to the high rock.
        const string ShaleCliffsLayout = @"
            ..........................................................
            ........................................r.....g...r....H.
            ....................................=====================.
            ..............................g.....r.....................
            ............................===========...................
            ......................r.....g.............................
            ....................===========...........................
            .................................g........................
            ..............................===========.................
            ...............................|.............g..r.........
            ...............................|........===========.......
            ...............................|.....g...r................
            ...............................|.==========...............
            ............................g..|..........................
            ........................===========.......................
            .....................g....................................
            ................===========...............................
            ...............g..........................................
            ..........===========.....................................
            ..G.@.........g...........g...........g...........g.......
            ##########################################################";

        const string ThornGladeLayout = @"
            ..........................................
            ..........................................
            ..........................................
            ..........................................
            ....=======...................=======.....
            ..........................................
            ........====..............====............
            .T..@.....................z...............
            ##########################################";

        const string AshenMarshLayout = @"
            ....................................................................................
            ........................w.......w...................................................
            ....................===============...............................w...l.............
            ..............................|...........................=================.........
            ..............................|...........w...................|.....................
            ....===========...............|.....=============.............|.....................
            ..........|...................|.w.........................l...|.....................
            ..........|...............=============.............=============...................
            ..........|...........l.........................t........................w..........
            ..........|.....=============.............=============.............===========.....
            ..........|..t........................t..........................l..................
            ..........|========.............=============...............===========.............
            .E..@.....|.t...........t.............l...........t.............l.............t..P.
            ####################################################################################";

        // Downhill: arrive on the high ledge at the top left and walk the terraces down to the exit on the right.
        // A ladder at the left climbs back to the ledge from the lower path; another reaches a lookout with a chest.
        const string AmberleafTrailLayout = @"
            ....................................................................................
            ....................................................................................
            ..........................................f.........................................
            ..B..@..a...........................=============...................................
            ==============........................|.............................................
            .........|......b.....a...............|.............................................
            .........|.================...........|.............................................
            .........|..................b.....f...|...............................f.............
            .........|..............=================.........................=========.........
            .........|.................................f......b............b.............a......
            .........|............................=================.....=========.....=======...
            ........b|..........a..........b..............f..........b......f......a..........F.
            ####################################################################################";

        // A mossy dip: zigzag ledges on the left lead to a rope bridge across the top; the gate ledge on the right
        // (ladder from the floor) holds the portal into Mosswood. A hidden ledge at the top left has a chest.
        const string MossgateHollowLayout = @"
            ..G....c..............................................................
            ..=========...........................................................
            ........|.................f.......c.........f.........................
            ........|.........=================================...................
            ........|...........|...................................c......m...B..
            ........|...........|................................=================
            ........|.........c.|.......................................|.........
            ........|.....===========...................................|.........
            ........|..m................................................|.........
            ......===========...........................................|.........
            ...................f...................c....................|.........
            ..............===========.........===========...............|.........
            ...........m.....................m..............f...........|.........
            ......===========...........===========.....=========.......|.........
            .F..@.....m...........f.......m.........c.......f.......m...|.........
            ######################################################################";
    }
}
