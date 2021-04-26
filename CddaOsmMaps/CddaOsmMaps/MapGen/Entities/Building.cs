using System.Collections.Generic;

namespace CddaOsmMaps.MapGen.Entities
{
    internal class Building : TypedMapElement
    {
        public const float WALL_WIDTH = 1;

        public Building(List<Polygon> polygons, string type)
            : base(polygons, type) { }
    }
}
