using System.Drawing;

namespace CddaOsmMaps.MapGen.Entities
{
    internal class CoastlineArea
    {
        public Point InitialPoint { get; private set; }
        public int AdjacentLandBorderPixels { get; set; }
        public int AdjacentWaterBorderPixels { get; set; }

        public CoastlineArea(Point initialPoint) => InitialPoint = initialPoint;

        public bool IsWater()
            // TODO ?? review
            => AdjacentWaterBorderPixels > AdjacentLandBorderPixels;

    }
}
