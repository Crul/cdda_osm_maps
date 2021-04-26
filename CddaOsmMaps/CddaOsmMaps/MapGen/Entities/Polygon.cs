using System.Collections.Generic;

namespace CddaOsmMaps.MapGen.Entities
{
    internal class Polygon : List<(float x, float y)>
    {
        public bool IsOuterPolygon { get; private set; }

        public Polygon(IEnumerable<(float x, float y)> points, bool isOuter)
            : base(points)
            => IsOuterPolygon = isOuter;

        public Polygon(IEnumerable<(float x, float y)> points)
            : this(points, true) { }
    }
}
