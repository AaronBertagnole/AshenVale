using UnityEngine;

namespace AshenVale
{
    // Builds the look of a map from its MapDef: ground, backdrop, platforms, ladders, gates, town buildings.
    // Each map gets one root object, switched on while the local player is on that map.
    public static class MapScenery
    {
        const float G = Tuning.Ground;

        public static GameObject Build(MapDef def)
        {
            var root = new GameObject("Map: " + def.name).transform;
            switch (def.style)
            {
                case "emberfields": Emberfields(root); break;
                case "mosswood": Mosswood(root); break;
                case "emberhaven": Emberhaven(root, def); break;
            }
            foreach (var plat in def.platforms) BuildPlatform(root, plat);
            foreach (var ladder in def.ladders) BuildLadder(root, ladder);
            foreach (var gate in def.gates) Sprites.Block(gate.label, gate.color, .9f, 1.9f, 8, root, gate.x, G + .9f);
            return root.gameObject;
        }

        static void Emberfields(Transform root)
        {
            Sprites.Block("Ground", new Color(.19f, .25f, .22f), 55, 1.3f, 0, root, 0, G - .73f);
            Sprites.Block("Ground highlight", new Color(.42f, .51f, .32f), 55, .14f, 1, root, 0, G - .07f);
            for (int i = 0; i < 45; i++)
                Sprites.Block("Distant forest", i % 3 == 0 ? new Color(.12f, .25f, .29f) : new Color(.12f, .2f, .25f),
                              .65f, 2 + (i % 5) * .6f, -3, root, -26 + i * 1.3f, -1.7f);
            for (int i = 0; i < 18; i++)
                Sprites.Block("Stone", new Color(.31f, .36f, .39f), .45f + (i % 3) * .2f, .2f, 2, root, -23 + i * 2.7f, G + .08f);
        }

        static void Mosswood(Transform root)
        {
            Sprites.Block("Mosswood ground", new Color(.13f, .25f, .20f), 55, 1.3f, 0, root, 0, G - .73f);
            Sprites.Block("Mosswood grass", new Color(.28f, .58f, .35f), 55, .14f, 1, root, 0, G - .07f);
            for (int i = 0; i < 38; i++)
                Sprites.Block("Mosswood silhouette", i % 3 == 0 ? new Color(.11f, .28f, .27f) : new Color(.08f, .21f, .19f),
                              .6f + (i % 3) * .2f, 2.5f + (i % 5) * .5f, -3, root, -25 + i * 1.4f, -1.65f);
        }

        static void Emberhaven(Transform root, MapDef def)
        {
            Sprites.Block("Emberhaven cobblestone", new Color(.30f, .27f, .25f), 34, 1.3f, 0, root, 0, G - .73f);
            Sprites.Block("Emberhaven street", new Color(.65f, .53f, .37f), 34, .15f, 1, root, 0, G - .06f);
            Sprites.Block("Town sky", new Color(.12f, .20f, .27f), 34, 10, -10, root, 0, 1);
            for (int i = 0; i < 9; i++)
            {
                float x = -14 + i * 3.5f;
                Sprites.Block("Town backdrop house", new Color(.36f, .28f, .28f), 2.8f, 2.3f, -3, root, x, G + 1.12f);
                Sprites.Block("Town roof", new Color(.54f, .25f, .22f), 3.15f, .5f, -2, root, x, G + 2.45f);
                Sprites.Block("Warm window", new Color(1f, .72f, .34f), .5f, .65f, -1, root, x, G + 1.3f);
            }
            float portal = def.returnPortalX, merchant = def.merchantX;
            Sprites.Block("Return portal glow", new Color(.23f, .45f, 1, .8f), 1.1f, 2.2f, 7, root, portal, G + 1.1f);
            Sprites.Block("Return portal core", new Color(.58f, .78f, 1, .85f), .55f, 1.7f, 8, root, portal, G + 1.1f);
            Sprites.Block("Merchant stall", new Color(.53f, .31f, .18f), 3.3f, 1.3f, 5, root, merchant, G + .75f);
            Sprites.Block("Merchant awning", new Color(.82f, .45f, .19f), 3.8f, .45f, 6, root, merchant, G + 1.7f);
            Sprites.Block("Merchant NPC", new Color(.91f, .76f, .44f), .65f, 1.25f, 9, root, merchant, G + .75f);
            Sprites.Block("Merchant sign", new Color(.93f, .77f, .45f), 1.5f, .32f, 7, root, merchant, G + 2.2f);
        }

        static void BuildPlatform(Transform root, Platform p)
        {
            float mid = (p.left + p.right) * .5f, width = p.right - p.left;
            Sprites.Block("Platform earth", new Color(.25f, .29f, .27f), width, .32f, 2, root, mid, p.top - .18f);
            Sprites.Block("Platform grass", new Color(.47f, .64f, .32f), width, .16f, 3, root, mid, p.top - .04f);
            for (float x = p.left + .4f; x < p.right; x += 1.3f)
                Sprites.Block("Platform support", new Color(.23f, .25f, .27f), .18f, .65f, 1, root, x, p.top - .53f);
        }

        static void BuildLadder(Transform root, Ladder l)
        {
            float h = l.top - l.bottom;
            for (int side = -1; side <= 1; side += 2)
                Sprites.Block("Ladder rail", new Color(.55f, .35f, .19f), .11f, h, 4, root, l.x + side * .27f, (l.bottom + l.top) * .5f);
            for (float y = l.bottom + .24f; y < l.top; y += .38f)
                Sprites.Block("Ladder rung", new Color(.72f, .51f, .28f), .65f, .09f, 5, root, l.x, y);
        }
    }
}
