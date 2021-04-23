using System.Collections.Generic;

namespace CddaOsmMaps.MapGen.Entities
{
    internal class Building
    {
        public string Type { get; private set; }
        public List<(float x, float y)> Path { get; private set; }

        public Building(string type, List<(float x, float y)> path)
        {
            Type = type;
            Path = path;
        }
    }
}
