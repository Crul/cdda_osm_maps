using System.Collections.Generic;
using System.Numerics;

namespace CddaOsmMaps.MapGen.Entities
{
    internal class Polygon : List<Vector2>
    {
        public bool IsOuterPolygon { get; private set; }

        public Polygon(IEnumerable<Vector2> points, bool isOuter)
            : base(points)
            => IsOuterPolygon = isOuter;

        public Polygon(IEnumerable<Vector2> points)
            : this(points, true) { }
    }
}
