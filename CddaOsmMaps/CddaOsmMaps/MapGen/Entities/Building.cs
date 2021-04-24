using System.Collections.Generic;

namespace CddaOsmMaps.MapGen.Entities
{
    internal class Building : MapElement
    {
        public const float WALL_WIDTH = 1;

        public Building(string type, List<(float x, float y)> path) : base(type, path) { }
    }
}
