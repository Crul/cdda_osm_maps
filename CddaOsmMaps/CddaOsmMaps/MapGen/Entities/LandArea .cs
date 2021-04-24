using System.Collections.Generic;
using System.Drawing;

namespace CddaOsmMaps.MapGen.Entities
{
    internal class LandArea : MapElement
    {
        private readonly Color DEFAULT_COLOR = Color.FromArgb(128, 128, 128);
        private readonly Dictionary<string, Color> COLOR_BY_LANDUSE =
            new Dictionary<string, Color>
            {
                 { "residential", Color.FromArgb(  0,255,255) },
                 { "commercial",  Color.FromArgb(255,  0,255) },
                 { "industrial",  Color.FromArgb(255,255,  0) },
                 { "garages",     Color.FromArgb(255,128,128) },
            };

        public Color FillColor { get; internal set; }

        public LandArea(string type, List<(float x, float y)> path)
            : base(type, path)
            => FillColor = COLOR_BY_LANDUSE.ContainsKey(type)
                ? COLOR_BY_LANDUSE[type]
                : DEFAULT_COLOR;
    }
}
