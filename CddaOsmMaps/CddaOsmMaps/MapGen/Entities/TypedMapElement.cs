using System.Collections.Generic;

namespace CddaOsmMaps.MapGen.Entities
{
    internal abstract class TypedMapElement : MapElement
    {
        public string Type { get; private set; }

        public TypedMapElement(string type, List<(float x, float y)> path)
            : base(path)
            => Type = type;
    }
}
