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
                // West to east along the road, as in MapDefs; Hollow Den hides up behind Mosswood.
                Place(s, Maps.Emberhaven,     .06f, .62f);
                Place(s, Maps.Emberfields,    .18f, .48f);
                Place(s, Maps.AmberleafTrail, .29f, .62f);
                Place(s, Maps.MossgateHollow, .40f, .44f);
                Place(s, Maps.Mosswood,       .52f, .32f);
                Place(s, Maps.HollowDen,      .45f, .10f, true);
                Place(s, Maps.ShaleCliffs,    .33f, .16f);
                Place(s, Maps.ThornGlade,     .60f, .20f);
                Place(s, Maps.Kingsroad,      .65f, .47f);
                Place(s, Maps.Cinderford,     .79f, .58f);
                Place(s, Maps.AshenMarsh,     .93f, .74f);
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
