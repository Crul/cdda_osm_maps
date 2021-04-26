using System.Collections.Generic;

namespace CddaOsmMaps.MapGen.Entities
{
    internal abstract class MapElement
    {
        public List<Polygon> Polygons { get; private set; }

        public MapElement(List<Polygon> polygons) => Polygons = polygons;
    }
}
