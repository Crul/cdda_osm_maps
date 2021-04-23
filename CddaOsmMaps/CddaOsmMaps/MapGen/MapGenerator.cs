using CddaOsmMaps.Crosscutting;
using CddaOsmMaps.MapGen.Contracts;
using CddaOsmMaps.MapGen.Entities;

namespace CddaOsmMaps.MapGen
{
    internal class MapGenerator : IMapGenerator
    {
        private readonly IMapProvider MapProvider;
        private readonly ImageBuilder Image;

        public (int width, int height) MapSize => MapProvider.MapSize;

        public MapGenerator(IMapProvider mapProvider)
        {
            MapProvider = mapProvider;
            Image = new ImageBuilder(MapProvider.MapSize);
        }

        public void Generate(string imgPath = "")
        {
            MapProvider.GetRoads().ForEach(GenerateRoad);

            if (!string.IsNullOrEmpty(imgPath))
                Image.Save(imgPath);

            Image.DisposeBuldingProperties();
        }

        public bool IsRoad((int x, int y) pixelPos)
        {
            var isPixelInImg = (
                0 <= pixelPos.x && pixelPos.x < MapSize.width
                && 0 <= pixelPos.y && pixelPos.y < MapSize.height
            );

            return isPixelInImg
                && Image.IsPixelColor(pixelPos, Road.ROAD_COLOR);
        }

        private void GenerateRoad(Road road)
            => Image.DrawPoints(
                road.Path,
                Road.ROAD_COLOR,
                MapProvider.PixelsPerMeter * road.Width
            );
    }
}
