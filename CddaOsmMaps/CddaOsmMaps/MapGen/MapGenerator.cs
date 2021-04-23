using CddaOsmMaps.Crosscutting;
using CddaOsmMaps.MapGen.Contracts;
using CddaOsmMaps.MapGen.Entities;
using System.Collections.Generic;

namespace CddaOsmMaps.MapGen
{
    internal class MapGenerator : IMapGenerator
    {
        private readonly IMapProvider MapProvider;
        private readonly ImageBuilder Image;

        private readonly Dictionary<(byte r, byte g, byte b), TerrainType> TERRAIN_TYPES_BY_COLOR =
            new Dictionary<(byte r, byte g, byte b), TerrainType>
            {
                {  Road.ROAD_COLOR, TerrainType.Pavement },
                {  Building.FLOOR_COLOR, TerrainType.HouseFloor },
                {  Building.WALL_COLOR, TerrainType.Wall },
            };

        public (int width, int height) MapSize => MapProvider.MapSize;

        public MapGenerator(IMapProvider mapProvider)
        {
            MapProvider = mapProvider;
            Image = new ImageBuilder(MapProvider.MapSize);
        }

        public void Generate(string imgPath = "")
        {
            var mapElements = MapProvider.GetMapElements();
            mapElements.LandAreas.ForEach(GenerateLandArea);
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
            if (TERRAIN_TYPES_BY_COLOR.TryGetValue(pixelColor, out var terrainType))
                return terrainType;

            return TerrainType.Default;
        }

        private void GenerateLandArea(LandArea landArea)
            => Image.DrawArea(
                landArea.Path,
                landArea.Color,
                landArea.Color,
                0
            );

        private void GenerateRoad(Road road)
            => Image.DrawPath(
                road.Path,
                Road.ROAD_COLOR,
                MapProvider.PixelsPerMeter * road.Width
            );

        private void GenerateBuilding(Building building)
            => Image.DrawArea(
                building.Path,
                fillColor: Building.FLOOR_COLOR,
                strokeColor: Building.WALL_COLOR,
                strokeWidth: Building.WALL_WIDTH
            );
    }
}
