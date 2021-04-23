using System.Collections.Generic;

namespace CddaOsmMaps.MapGen.Entities
{
    internal class Building
    {
        public string Type { get; private set; }
        public List<(float x, float y)> Path { get; private set; }

        public const float WALL_WIDTH = 1;
        public static readonly (byte r, byte g, byte b) WALL_COLOR = (255, 0, 0);
        public static readonly (byte r, byte g, byte b) FLOOR_COLOR = (0, 255, 0);

        public Building(string type, List<(float x, float y)> path)
        {
            Type = type;
            Path = path;
        }
    }
}
