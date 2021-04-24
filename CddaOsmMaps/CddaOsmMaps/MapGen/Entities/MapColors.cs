using System.Collections.Generic;
using System.Drawing;

namespace CddaOsmMaps.MapGen.Entities
{
    public static class MapColors
    {
        public static readonly Color GRASS_COLOR = Color.FromArgb(0, 255, 0);
        public static readonly Color DEAD_GRASS_COLOR = Color.FromArgb(172, 172, 0);

        public static readonly Color LAND_AREA_DEFAULT_COLOR = Color.FromArgb(128, 128, 128);
        public static readonly Dictionary<string, Color> LANDUSE_COLORS =
            new Dictionary<string, Color>
            {
                // https://wiki.openstreetmap.org/wiki/Key:landuse
                { "commercial",         Color.FromArgb(  0,  0,255) },
                { "construction",       DIRT_FLOOR_COLOR },
                { "industrial",         Color.FromArgb(255,255,  0) },
                { "residential",        Color.FromArgb(255,172,128) },
                { "retail",             Color.FromArgb(  0,  0,255)},
                { "allotments",         DIRT_FLOOR_COLOR },  // agriculture
                { "farmland",           DIRT_FLOOR_COLOR },
                { "farmyard",           DIRT_FLOOR_COLOR },
                { "flowerbed",          LAND_AREA_DEFAULT_COLOR },
                { "forest",             Color.FromArgb(  0, 128,  0) },
                { "meadow",             LAND_AREA_DEFAULT_COLOR },
                { "orchard",            LAND_AREA_DEFAULT_COLOR },
                { "vineyard",           LAND_AREA_DEFAULT_COLOR },
                { "basin",              LAND_AREA_DEFAULT_COLOR },
                { "brownfield",         DIRT_FLOOR_COLOR },
                { "cemetery",           LAND_AREA_DEFAULT_COLOR },
                { "conservation",       LAND_AREA_DEFAULT_COLOR },
                { "depot",              LAND_AREA_DEFAULT_COLOR },
                { "garages",            Color.FromArgb(128,128,255) },
                { "grass",              DEAD_GRASS_COLOR },
                { "greenfield",         GRASS_COLOR },
                { "greenhouse_horticulture", LAND_AREA_DEFAULT_COLOR },
                { "landfill",           LAND_AREA_DEFAULT_COLOR },
                { "military",           LAND_AREA_DEFAULT_COLOR },
                { "plant_nursery",      LAND_AREA_DEFAULT_COLOR },
                { "port",               LAND_AREA_DEFAULT_COLOR },
                { "quarry",             LAND_AREA_DEFAULT_COLOR },
                { "railway",            LAND_AREA_DEFAULT_COLOR },
                { "recreation_ground",  LAND_AREA_DEFAULT_COLOR },
                { "religious",          LAND_AREA_DEFAULT_COLOR },
                { "reservoir",          LAND_AREA_DEFAULT_COLOR },
                { "salt_pond",          LAND_AREA_DEFAULT_COLOR },
                { "village_green",      LAND_AREA_DEFAULT_COLOR },
                { "winter_sports",      LAND_AREA_DEFAULT_COLOR }
            };

        public static readonly Color PAVEMENT_COLOR = Color.FromArgb(0, 0, 0);
        public static readonly Color CONCRETE_FLOOR_COLOR = Color.FromArgb(64, 64, 0);
        public static readonly Color DIRT_FLOOR_COLOR = Color.FromArgb(180, 130, 0);

        public static readonly Color SIDEWALK_COLOR = Color.FromArgb(156, 156, 156);
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
                { "construction",   CONCRETE_FLOOR_COLOR },
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
