using System.Collections.Generic;
using System.Drawing;

namespace CddaOsmMaps.MapGen.Entities
{
    public static class MapColors
    {
        public static readonly Color LAND_AREA_DEFAULT_COLOR = Color.FromArgb(128, 128, 128);
        public static readonly Dictionary<string, Color> LANDUSE_COLORS =
            new Dictionary<string, Color>
            {
                 { "residential", Color.FromArgb(  0,255,255) },
                 { "commercial",  Color.FromArgb(255,  0,255) },
                 { "industrial",  Color.FromArgb(255,255,  0) },
                 { "garages",     Color.FromArgb(255,128,128) },
            };

        public static readonly Color ROAD_COLOR = Color.FromArgb(0, 0, 0);

        public static readonly Color WALL_COLOR = Color.FromArgb(255, 0, 0);
        public static readonly Color FLOOR_COLOR = Color.FromArgb(0, 255, 0);
    }
}
