using UnityEngine;

namespace AshenVale
{
    public enum MapKind { Field, Town }

    // One-way platform: you land on its top when falling from above.
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

    // Press Up within Tuning.GateReach of x to travel to targetMap, arriving at targetX.
    public struct Gate
    {
        public float x, targetX;
        public int targetMap;
        public Color color;
        public string label;
        public Gate(float x, int targetMap, float targetX, Color color, string label)
        { this.x = x; this.targetMap = targetMap; this.targetX = targetX; this.color = color; this.label = label; }
    }

    public struct SpawnEntry
    {
        public int monster;
        public float weight;
        public SpawnEntry(int monster, float weight) { this.monster = monster; this.weight = weight; }
    }

    // Gameplay layout of one map. Its look is built by MapScenery from `style`.
    public sealed class MapDef
    {
        public int id;
        public string name, style, hint;
        public MapKind kind;
        public float minX, maxX;        // how far the player can walk
        public float cameraClamp;       // how far the camera follows
        public float startX;            // where you arrive on a new hero, or after waking in town
        public Platform[] platforms = new Platform[0];
        public Ladder[] ladders = new Ladder[0];
        public Gate[] gates = new Gate[0];
        public SpawnEntry[] spawns = new SpawnEntry[0];
        public float merchantX, returnPortalX; // Town only
    }

    public static class Maps
    {
        public const int Emberfields = 0, Mosswood = 1, Emberhaven = 2;
        public const int Town = Emberhaven; // where town portals lead and where you wake after falling

        const float G = Tuning.Ground;

        public static readonly MapDef[] All =
        {
            new MapDef
            {
                id = Emberfields, name = "Emberfields", style = "emberfields", kind = MapKind.Field,
                hint = "EMBERFIELDS — GREEN GATE AT FAR RIGHT: UP TO MOSSWOOD",
                minX = -23.5f, maxX = 23.5f, cameraClamp = 14f, startX = -3f,
                platforms = new[] { new Platform(-18f, -12f, -.95f), new Platform(-9f, -3.5f, .55f), new Platform(1.5f, 7f, -.85f),
                                    new Platform(10f, 16f, .55f), new Platform(18f, 22f, -.95f) },
                ladders = new[] { new Ladder(-15f, G, -.95f), new Ladder(-6f, G, .55f), new Ladder(4f, G, -.85f),
                                  new Ladder(13f, G, .55f), new Ladder(20f, G, -.95f) },
                gates = new[] { new Gate(22.5f, Mosswood, -19.5f, new Color(.35f, .85f, .65f, .85f), "Mosswood entrance portal - UP") },
                spawns = new[] { new SpawnEntry(Monsters.EliteEmberling, .17f), new SpawnEntry(Monsters.Emberling, .83f) },
            },
            new MapDef
            {
                id = Mosswood, name = "Mosswood", style = "mosswood", kind = MapKind.Field,
                hint = "MOSSWOOD — BLUE GATE AT FAR LEFT: UP TO EMBERFIELDS",
                minX = -23.5f, maxX = 23.5f, cameraClamp = 14f, startX = -19.5f,
                platforms = new[] { new Platform(-18f, -11f, -1.2f), new Platform(-8f, -2f, .35f), new Platform(2f, 9f, -.6f), new Platform(12f, 19f, .65f) },
                ladders = new[] { new Ladder(-14.5f, G, -1.2f), new Ladder(-5f, G, .35f), new Ladder(5.5f, G, -.6f), new Ladder(15.5f, G, .65f) },
                gates = new[] { new Gate(-22.5f, Emberfields, 19.5f, new Color(.4f, .75f, 1f, .8f), "Mosswood return portal - UP") },
                spawns = new[] { new SpawnEntry(Monsters.VioletWraith, .45f), new SpawnEntry(Monsters.MossCrawler, .55f) },
            },
            new MapDef
            {
                id = Emberhaven, name = "Emberhaven", style = "emberhaven", kind = MapKind.Town,
                hint = "EMBERHAVEN — WALK TO MERCHANT (RIGHT) OR BLUE PORTAL (LEFT), PRESS UP AT PORTAL / E AT MERCHANT",
                minX = -14f, maxX = 14f, cameraClamp = 7f, startX = -3.4f,
                merchantX = 3.5f, returnPortalX = -5f,
            },
        };
    }
}
