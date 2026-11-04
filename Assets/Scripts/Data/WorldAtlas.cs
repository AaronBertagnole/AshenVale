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
                // Positions from the owner's painted atlas (AshenVale_World_Map, WorldMapLocations.json; x and y are
                // shares of the picture from its top-left). Resources/Art/UI/world_map.png is the unlabelled terrain of it.
                Place(s, Maps.Emberhaven,       0.050f, 0.800f);
                Place(s, Maps.Emberfields,      0.120f, 0.721f);
                Place(s, Maps.AmberleafTrail,   0.180f, 0.820f);
                Place(s, Maps.MossgateHollow,   0.241f, 0.701f);
                Place(s, Maps.Mosswood,         0.311f, 0.621f);
                Place(s, Maps.HollowDen,        0.270f, 0.520f, true);
                Place(s, Maps.ThornGlade,       0.361f, 0.520f);
                Place(s, Maps.ShaleCliffs,      0.200f, 0.520f);
                Place(s, Maps.FrostpinePass,    0.140f, 0.401f);
                Place(s, Maps.Highspire,        0.080f, 0.300f);
                Place(s, Maps.GlacierSteps,     0.140f, 0.201f);
                Place(s, Maps.WindwardRidge,    0.221f, 0.119f);
                Place(s, Maps.IronveinMines,    0.300f, 0.201f);
                Place(s, Maps.CrystalHollow,    0.361f, 0.100f, true);
                Place(s, Maps.DeepForge,        0.340f, 0.320f);
                Place(s, Maps.ColossusVault,    0.400f, 0.401f);
                Place(s, Maps.Kingsroad,        0.391f, 0.741f);
                Place(s, Maps.Cinderford,       0.471f, 0.841f);
                Place(s, Maps.AshenMarsh,       0.550f, 0.920f);
                Place(s, Maps.BrackwaterShore,  0.630f, 0.861f);
                Place(s, Maps.Saltmarrow,       0.710f, 0.930f);
                Place(s, Maps.ThistledownTrail, 0.681f, 0.760f);
                Place(s, Maps.Eldermere,        0.641f, 0.641f);
                Place(s, Maps.Glimmerwood,      0.701f, 0.520f);
                Place(s, Maps.Moonwell,         0.781f, 0.460f, true);
                Place(s, Maps.HollowLibrary,    0.641f, 0.420f);
                Place(s, Maps.Owlkeep,          0.721f, 0.320f);
                Place(s, Maps.SpellbrokenGlade, 0.801f, 0.240f);
                Place(s, Maps.ArchmageSanctum,  0.881f, 0.300f);
                Place(s, Maps.ForgottenStair,   0.740f, 0.140f);
                Place(s, Maps.Lastlight,        0.641f, 0.080f);
                Place(s, Maps.SunkenAvenue,     0.561f, 0.159f);
                Place(s, Maps.HallOfIdols,      0.480f, 0.080f);
                Place(s, Maps.Oubliette,        0.400f, 0.140f, true);
                Place(s, Maps.WeepingCatacombs, 0.460f, 0.259f);
                Place(s, Maps.BlackZiggurat,    0.501f, 0.360f);
                Place(s, Maps.SleepersThrone,   0.561f, 0.460f);
                Place(s, Maps.WreckersReef,     0.790f, 0.861f);
                Place(s, Maps.SirensGrotto,     0.840f, 0.941f, true);
                Place(s, Maps.SmugglersCove,    0.870f, 0.800f);
                Place(s, Maps.BlackGalleon,     0.900f, 0.701f);
                Place(s, Maps.GallowsPoint,     0.930f, 0.580f);
                Place(s, Maps.CaptainsHold,     0.930f, 0.440f);
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
