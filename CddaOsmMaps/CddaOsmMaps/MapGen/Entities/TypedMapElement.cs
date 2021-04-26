using System.Collections.Generic;

namespace CddaOsmMaps.MapGen.Entities
{
    internal abstract class TypedMapElement : MapElement
    {
        public string Type { get; private set; }

        public TypedMapElement(List<Polygon> polygons, string type)
            : base(polygons)
            => Type = type;
    }
}
