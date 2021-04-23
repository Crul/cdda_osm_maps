using System.Collections.Generic;

namespace CddaOsmMaps.MapGen.Entities
{
    internal abstract class MapElement
    {
        public string Type { get; private set; }
        public List<(float x, float y)> Path { get; private set; }

        public MapElement(string type, List<(float x, float y)> path)
        {
            Type = type;
            Path = path;
        }
    }
}
