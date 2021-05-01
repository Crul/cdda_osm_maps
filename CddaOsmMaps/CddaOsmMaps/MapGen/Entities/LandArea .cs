using System.Collections.Generic;
using System.Drawing;

namespace CddaOsmMaps.MapGen.Entities
{
    internal class LandArea : TypedMapElement
    {
        public Color FillColor { get; private set; }
        public bool IsVisible { get; private set; }
        public bool IsWater { get => FillColor == MapColors.DEEP_WATER_COLOR; }

        public LandArea(List<Polygon> polygons, string type)
            : base(polygons, type)
        {
            FillColor = MapColors.LANDUSE_COLORS.ContainsKey(type)
                  ? MapColors.LANDUSE_COLORS[type]
                  : MapColors.INVISIBLE;

            IsVisible = (FillColor != MapColors.INVISIBLE);
        }
    }
}
