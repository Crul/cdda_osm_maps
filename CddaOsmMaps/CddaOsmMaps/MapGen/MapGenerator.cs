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
            var mapElements = MapProvider.GetMapElements();
            mapElements.Roads.ForEach(GenerateRoad);
            mapElements.Buildings.ForEach(GenerateBuilding);

            if (!string.IsNullOrEmpty(imgPath))
                Image.Save(imgPath);

            Image.DisposeBuldingProperties();
        }

        public TerrainType GetTerrain((int x, int y) pixelPos)
        {
            var isPixelInImg = (
                0 <= pixelPos.x && pixelPos.x < MapSize.width
                && 0 <= pixelPos.y && pixelPos.y < MapSize.height
            );
            if (!isPixelInImg)
                return TerrainType.Default;

            var pixelColor = Image.GetPixelColor(pixelPos);

            if (pixelColor == Road.ROAD_COLOR)
                return TerrainType.Pavement;

            return TerrainType.Default;
        }

        private void GenerateRoad(Road road)
            => Image.DrawPoints(
                road.Path,
                Road.ROAD_COLOR,
                MapProvider.PixelsPerMeter * road.Width
            );

        private void GenerateBuilding(Building obj)
        {
            // TODO buildings
        }
    }
}
