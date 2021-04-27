using System.Collections.Generic;
using System.Drawing;

namespace CddaOsmMaps.MapGen.Entities
{
    public static class MapColors
    {
        public static readonly Color INVISIBLE = Color.FromArgb(128, 128, 128);

        public static readonly Color DEEP_WATER_COLOR = Color.FromArgb(128, 255, 255);

        public static readonly Color GRASS_COLOR = Color.FromArgb(0, 255, 0);
        public static readonly Color GRASS_LONG_COLOR = Color.FromArgb(172, 172, 0);

        public static readonly Color LAND_AREA_DEFAULT_COLOR = Color.FromArgb(178, 128, 128);
        public static readonly Dictionary<string, Color> LANDUSE_COLORS =
            new Dictionary<string, Color>
            {
                // https://wiki.openstreetmap.org/wiki/Key:natural
                { "water",              DEEP_WATER_COLOR },
                { "wetland",            DEEP_WATER_COLOR },
                { "glacier",            DEEP_WATER_COLOR },
                { "bay",                DEEP_WATER_COLOR },
                { "spring",             DEEP_WATER_COLOR },
                { "hot_spring",         DEEP_WATER_COLOR },    
                // https://wiki.openstreetmap.org/wiki/Key:landuse
                { "commercial",         Color.FromArgb(  0,  0,255) },
                { "construction",       DIRT_FLOOR_COLOR },
                { "industrial",         Color.FromArgb(255,255,  0) },
                { "residential",        Color.FromArgb(255,172,128) },
                { "retail",             Color.FromArgb(  0,  0,255)},
                { "allotments",         DIRT_FLOOR_COLOR },  // agriculture
                { "farmland",           DIRT_FLOOR_COLOR },
                { "farmyard",           DIRT_FLOOR_COLOR },
                { "flowerbed",          GRASS_LONG_COLOR }, // TODO flowers
                { "forest",             Color.FromArgb(  0, 128,  0) },
                { "meadow",             INVISIBLE },
                { "orchard",            INVISIBLE },
                { "vineyard",           INVISIBLE },
                { "basin",              INVISIBLE },
                { "brownfield",         DIRT_FLOOR_COLOR },
                { "cemetery",           INVISIBLE },
                { "conservation",       INVISIBLE },
                { "depot",              INVISIBLE },
                { "garages",            Color.FromArgb(128,128,255) },
                { "grass",              GRASS_LONG_COLOR },
                { "greenfield",         GRASS_COLOR },
                { "greenhouse_horticulture", INVISIBLE },
                { "landfill",           INVISIBLE },
                { "military",           INVISIBLE },
                { "plant_nursery",      INVISIBLE },
                { "port",               INVISIBLE },
                { "quarry",             INVISIBLE },
                { "railway",            INVISIBLE },
                { "recreation_ground",  INVISIBLE },
                { "religious",          INVISIBLE },
                { "reservoir",          INVISIBLE },
                { "salt_pond",          INVISIBLE },
                { "village_green",      INVISIBLE },
                { "winter_sports",      INVISIBLE }
            };

        public static readonly Color PAVEMENT_COLOR = Color.FromArgb(0, 0, 0);
        public static readonly Color CONCRETE_FLOOR_COLOR = Color.FromArgb(156, 156, 156);
        public static readonly Color DIRT_FLOOR_COLOR = Color.FromArgb(180, 130, 0);

        public static readonly Color SIDEWALK_COLOR = Color.FromArgb(172, 172, 172);
        public static readonly Color ROAD_DEFAULT_COLOR = PAVEMENT_COLOR;
        public static readonly Dictionary<string, Color> ROAD_COLORS =
            new Dictionary<string, Color>
            {
                // https://wiki.openstreetmap.org/wiki/Key:highway
                // TODO ? use <tag k="surface" v="..."/>
                // https://wiki.openstreetmap.org/wiki/Key:surface
                { "motorway",       PAVEMENT_COLOR },
                { "motorway_link",  PAVEMENT_COLOR },
                { "trunk",          PAVEMENT_COLOR },
                { "trunk_link",     PAVEMENT_COLOR },
                { "primary",        PAVEMENT_COLOR },
                { "primary_link",   PAVEMENT_COLOR },
                { "secondary",      PAVEMENT_COLOR },
                { "secondary_link", PAVEMENT_COLOR },
                { "tertiary",       PAVEMENT_COLOR },
                { "tertiary_link",  PAVEMENT_COLOR },
                { "unclassified",   DIRT_FLOOR_COLOR },
                { "residential",    PAVEMENT_COLOR },
                { "living_street",  PAVEMENT_COLOR },
                { "service",        DIRT_FLOOR_COLOR },
                { "pedestrian",     CONCRETE_FLOOR_COLOR },
                { "track",          DIRT_FLOOR_COLOR },
                { "bus_guideway",   PAVEMENT_COLOR },
                { "escape",         PAVEMENT_COLOR },
                { "raceway",        PAVEMENT_COLOR },
                { "road",           PAVEMENT_COLOR },
                { "busway",         PAVEMENT_COLOR },
                { "footway",        DIRT_FLOOR_COLOR },
                { "bridleway",      DIRT_FLOOR_COLOR },
                { "steps",          CONCRETE_FLOOR_COLOR },
                { "path",           DIRT_FLOOR_COLOR },
                { "cycleway",       DIRT_FLOOR_COLOR },
                { "construction",   DIRT_FLOOR_COLOR },
                { "bus_stop",       PAVEMENT_COLOR },
                { "crossing",       PAVEMENT_COLOR },
                { "emergency_bay",  PAVEMENT_COLOR },
                { "motorway_junction", PAVEMENT_COLOR },
                { "passing_place",  PAVEMENT_COLOR },
                { "platform",       PAVEMENT_COLOR },
                { "rest_area",      PAVEMENT_COLOR },
            };

        public static readonly Color WALL_COLOR = Color.FromArgb(172, 96, 0);
        public static readonly Color FLOOR_COLOR = Color.FromArgb(255, 128, 0);
    }
}
