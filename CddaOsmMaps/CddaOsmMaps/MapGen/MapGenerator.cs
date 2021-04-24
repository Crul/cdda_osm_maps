using CddaOsmMaps.Crosscutting;
using CddaOsmMaps.MapGen.Contracts;
using CddaOsmMaps.MapGen.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CddaOsmMaps.MapGen
{
    internal class MapGenerator : IMapGenerator
    {
        private readonly IMapProvider MapProvider;
        private readonly ImageBuilder Image;

        private readonly Dictionary<Color, TerrainType> TERRAIN_TYPES_BY_COLOR =
            new Dictionary<Color, TerrainType>
            {
                {  MapColors.PAVEMENT_COLOR,        TerrainType.Pavement },
                {  MapColors.SIDEWALK_COLOR,        TerrainType.Sidewalk },
                {  MapColors.DIRT_FLOOR_COLOR,      TerrainType.DirtFloor },
                {  MapColors.CONCRETE_FLOOR_COLOR,  TerrainType.ConcreteFloor },
                {  MapColors.FLOOR_COLOR,           TerrainType.HouseFloor },
                {  MapColors.WALL_COLOR,            TerrainType.Wall },
                {  MapColors.GRASS_COLOR,           TerrainType.Grass },
                {  MapColors.DEAD_GRASS_COLOR,      TerrainType.DeadGrass },
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

            var dirtRoads = GetRoadsByColor(
                mapElements.Roads,
                roadColor => roadColor == MapColors.DIRT_FLOOR_COLOR
            );

            var nonDirtRoads = GetRoadsByColor(
                mapElements.Roads,
                roadColor => roadColor != MapColors.DIRT_FLOOR_COLOR
            );

            GenerateRoads(dirtRoads);
            GenerateRoads(nonDirtRoads);

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
                landArea.FillColor,
                landArea.FillColor,
                0
            );

        private static List<Road> GetRoadsByColor(List<Road> roads, Func<Color, bool> condition)
            => roads
                .Where(road => condition(MapColors.ROAD_COLORS[road.Type]))
                .OrderBy(r => r.Width)
                .ToList();

        private void GenerateRoads(List<Road> roads)
        {
            roads.Where(r => r.HasSidewalk)
                .ToList()
                .ForEach(GenerateSidewalk);

            roads.ForEach(GenerateRoad);
        }

        private void GenerateRoad(Road road)
            => Image.DrawPath(
                road.Path,
                MapColors.ROAD_COLORS[road.Type],
                MapProvider.PixelsPerMeter * road.Width
            );

        private void GenerateSidewalk(Road road)
            => Image.DrawPath(
                road.Path,
                MapColors.SIDEWALK_COLOR,
                MapProvider.PixelsPerMeter * road.SidewalkWidth
            );

        private void GenerateBuilding(Building building)
            => Image.DrawArea(
                building.Path,
                fillColor: MapColors.FLOOR_COLOR,
                strokeColor: MapColors.WALL_COLOR,
                strokeWidth: Building.WALL_WIDTH
            );
    }
}
