using System.Collections.Generic;
using UnityEngine;

namespace AshenVale
{
    // Where each map sits on the world map (M), as a share of the map's drawing area: x 0 = west edge, 1 = east;
    // y 0 = top, 1 = bottom. Roads between maps come from their portals. If a painted world map is dropped in
    // (Resources/Art/UI/world_map.png), move these so each spot sits on its place in the picture.
    public struct AtlasSpot
    {
        public float x, y;
        public bool secret;     // never hinted at on the world map or minimap until you have been there
    }

    public static class WorldAtlas
    {
        static AtlasSpot[] spots;
        static List<Vector2Int> roads;

        static void Place(AtlasSpot[] into, int map, float x, float y, bool secret = false)
        {
            into[map] = new AtlasSpot { x = x, y = y, secret = secret };
        }

        static AtlasSpot[] Spots
        {
            get
            {
                if (spots != null) return spots;
                var s = new AtlasSpot[Maps.All.Length];
                for (int i = 0; i < s.Length; i++) s[i] = new AtlasSpot { x = .5f, y = .5f };   // until it is placed
                // The world map holds five regions: Emberhaven's lands across the bottom left, Highspire's mountains
                // above them, the coast to the east, the deep forest north of the coast and the ruins at the top.
                // Region 1, west to east along the road; Hollow Den hides up behind Mosswood.
                Place(s, Maps.Emberhaven,     .05f, .80f);
                Place(s, Maps.Emberfields,    .12f, .72f);
                Place(s, Maps.AmberleafTrail, .18f, .82f);
                Place(s, Maps.MossgateHollow, .24f, .70f);
                Place(s, Maps.Mosswood,       .31f, .62f);
                Place(s, Maps.HollowDen,      .27f, .52f, true);
                Place(s, Maps.ShaleCliffs,    .20f, .52f);
                Place(s, Maps.ThornGlade,     .36f, .52f);
                Place(s, Maps.Kingsroad,      .39f, .74f);
                Place(s, Maps.Cinderford,     .47f, .84f);
                Place(s, Maps.AshenMarsh,     .55f, .92f);
                // Region 3, Highspire: up the mountain from the Shale Cliffs summit, then under it.
                Place(s, Maps.FrostpinePass,  .14f, .40f);
                Place(s, Maps.Highspire,      .08f, .30f);
                Place(s, Maps.GlacierSteps,   .14f, .20f);
                Place(s, Maps.WindwardRidge,  .22f, .12f);
                Place(s, Maps.IronveinMines,  .30f, .20f);
                Place(s, Maps.CrystalHollow,  .36f, .10f, true);
                Place(s, Maps.DeepForge,      .34f, .32f);
                Place(s, Maps.ColossusVault,  .40f, .40f);
                // Region 4, the coast: down to the sea east of the marsh, out along the reefs to the point.
                Place(s, Maps.BrackwaterShore, .63f, .86f);
                Place(s, Maps.Saltmarrow,      .71f, .93f);
                Place(s, Maps.WreckersReef,    .79f, .86f);
                Place(s, Maps.SirensGrotto,    .84f, .94f, true);
                Place(s, Maps.SmugglersCove,   .87f, .80f);
                Place(s, Maps.BlackGalleon,    .92f, .70f);
                Place(s, Maps.GallowsPoint,    .95f, .58f);
                Place(s, Maps.CaptainsHold,    .97f, .46f);
                spots = s;
                return spots;
            }
        }

        public static AtlasSpot Spot(int map) { return map >= 0 && map < Spots.Length ? Spots[map] : new AtlasSpot { x = .5f, y = .5f }; }

        public static bool IsSecret(int map) { return Spot(map).secret; }

        // Pairs of maps joined by a portal, each pair once (a.x < a.y).
        public static List<Vector2Int> Roads
        {
            get
            {
                if (roads != null) return roads;
                roads = new List<Vector2Int>();
                foreach (var def in Maps.All)
                    foreach (var portal in def.portals)
                    {
                        var pair = new Vector2Int(Mathf.Min(def.id, portal.targetMap), Mathf.Max(def.id, portal.targetMap));
                        if (pair.x != pair.y && !roads.Contains(pair)) roads.Add(pair);
                    }
                return roads;
            }
        }

        // Whether the world map shows a map to this hero: every map they have been to, plus "???" for the
        // non-secret maps next door to one of those (so you can see where the road goes on).
        public static bool Shown(Character ch, int map)
        {
            if (ch.HasSeen(map)) return true;
            if (IsSecret(map)) return false;
            foreach (var road in Roads)
                if ((road.x == map && ch.HasSeen(road.y)) || (road.y == map && ch.HasSeen(road.x))) return true;
            return false;
        }
    }
}
