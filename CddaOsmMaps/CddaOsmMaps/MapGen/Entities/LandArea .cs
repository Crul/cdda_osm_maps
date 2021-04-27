using System.Collections.Generic;
using System.Drawing;

namespace CddaOsmMaps.MapGen.Entities
{
    internal class LandArea : TypedMapElement
    {
        public Color FillColor { get; private set; }
        public bool IsVisible { get; private set; }

        public LandArea(string type, List<(float x, float y)> path)
            : base(type, path)
        {
            FillColor = MapColors.LANDUSE_COLORS.ContainsKey(type)
                  ? MapColors.LANDUSE_COLORS[type]
                  : MapColors.INVISIBLE;

            IsVisible = (FillColor != MapColors.INVISIBLE);
        }
    }
}
