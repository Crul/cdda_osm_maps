using System.Collections.Generic;

namespace CddaOsmMaps.MapGen.Entities
{
    internal class LandArea : MapElement
    {
        private static readonly (byte r, byte g, byte b) DEFAULT_COLOR = (128, 128, 128);
        private static readonly Dictionary<string, (byte r, byte g, byte b)> COLOR_BY_LANDUSE =
            new Dictionary<string, (byte r, byte g, byte b)>
            {
                 { "residential", (  0,255,255) },
                 { "commercial",  (255,  0,255) },
                 { "industrial",  (255,255,  0) },
                 { "garages",     (255,128,128) },
            };

        public LandArea(string type, List<(float x, float y)> path)
            : base(type, path)
            => Color = COLOR_BY_LANDUSE.ContainsKey(type)
                ? COLOR_BY_LANDUSE[type]
                : DEFAULT_COLOR;

        public (byte r, byte g, byte b) Color { get; internal set; }
    }
}
