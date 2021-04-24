using System.Collections.Generic;
using System.Drawing;

namespace CddaOsmMaps.MapGen.Entities
{
    internal class LandArea : MapElement
    {
        public Color FillColor { get; internal set; }

        public LandArea(string type, List<(float x, float y)> path)
            : base(type, path)
            => FillColor = MapColors.LANDUSE_COLORS.ContainsKey(type)
                ? MapColors.LANDUSE_COLORS[type]
                : MapColors.LAND_AREA_DEFAULT_COLOR;
    }
}
