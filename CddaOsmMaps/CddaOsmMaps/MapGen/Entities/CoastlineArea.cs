namespace CddaOsmMaps.MapGen.Entities
{
    internal class CoastlineArea
    {
        public (int x, int y) InitialPoint { get; private set; }
        public int AdjacentLandBorderPixels { get; set; }
        public int AdjacentWaterBorderPixels { get; set; }

        public CoastlineArea(int x, int y) => InitialPoint = (x, y);

        public bool IsWater()
            // TODO ?? review
            => AdjacentWaterBorderPixels > AdjacentLandBorderPixels;

    }
}
