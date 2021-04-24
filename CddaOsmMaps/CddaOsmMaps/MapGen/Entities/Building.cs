using System.Collections.Generic;
using System.Drawing;

namespace CddaOsmMaps.MapGen.Entities
{
    internal class Building : MapElement
    {
        public const float WALL_WIDTH = 1;
        public static readonly Color WALL_COLOR = Color.FromArgb(255, 0, 0);
        public static readonly Color FLOOR_COLOR = Color.FromArgb(0, 255, 0);

        public Building(string type, List<(float x, float y)> path) : base(type, path) { }
    }
}
