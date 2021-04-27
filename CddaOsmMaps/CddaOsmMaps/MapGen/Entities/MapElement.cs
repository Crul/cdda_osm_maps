using System.Collections.Generic;

namespace CddaOsmMaps.MapGen.Entities
{
    internal abstract class MapElement
    {
        public List<(float x, float y)> Path { get; private set; }

        public MapElement(List<(float x, float y)> path) => Path = path;
    }
}
