using CddaOsmMaps.Crosscutting;
using CddaOsmMaps.MapGen.Contracts;
using CddaOsmMaps.MapGen.Entities;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CddaOsmMaps.MapGen
{
    internal class MapGenerator : IMapGenerator
    {
        private readonly IMapProvider MapProvider;
        private ImageBuilder Image;
        private static readonly SKColor EMPTY_AREA_COLOR = Color.White.ToSKColor();
        public static readonly SKColor COASTLINE_BORDER_COLOR = Color.Red.ToSKColor();
        public static readonly SKColor COASTLINE_WATER_SIDE_COLOR = MapColors.DEEP_WATER_COLOR.ToSKColor();
        public const uint NO_COASTLINE_AREA_IDX = int.MaxValue;

        private static readonly List<(int x, int y)> ADJACENT_COORDS = new List<(int x, int y)>
        {
            (1, 0), (-1, 0), (0, 1), (0, -1)
        };

        private readonly Dictionary<Color, TerrainType> TERRAIN_TYPES_BY_COLOR =
            new Dictionary<Color, TerrainType>
            {
                {  MapColors.DEEP_WATER_COLOR,      TerrainType.DeepMovWater },
                {  MapColors.PAVEMENT_COLOR,        TerrainType.Pavement },
                {  MapColors.SIDEWALK_COLOR,        TerrainType.Sidewalk },
                {  MapColors.DIRT_FLOOR_COLOR,      TerrainType.DirtFloor },
                {  MapColors.CONCRETE_FLOOR_COLOR,  TerrainType.ConcreteFloor },
                {  MapColors.FLOOR_COLOR,           TerrainType.HouseFloor },
                {  MapColors.WALL_COLOR,            TerrainType.Wall },
                {  MapColors.GRASS_COLOR,           TerrainType.Grass },
                {  MapColors.GRASS_LONG_COLOR,      TerrainType.GrassLong },
            };

        public (int width, int height) MapSize => MapProvider.MapSize;

        public MapGenerator(IMapProvider mapProvider) => MapProvider = mapProvider;

        public void Generate(string imgPath = "")
        {
            var mapElements = MapProvider.GetMapElements();

            Image = new ImageBuilder(MapProvider.MapSize, EMPTY_AREA_COLOR);

            if (mapElements.Coastlines.Count > 0)
            {
                GenerateCoastlines(mapElements.Coastlines);
                // Image.Save(imgPath);
                // return;
            }

            mapElements.LandAreas
                .Where(la => la.IsVisible)
                .ToList()
                .ForEach(GenerateLandArea);

            mapElements.Rivers.ForEach(GenerateRiver);

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

        private void GenerateCoastlines(List<Coastline> coastlines)
        {
            coastlines.ForEach(GenerateCoastlineLSideIndicator);
            coastlines.ForEach(cl => GenerateCoastlineLBorder(cl));
            FillCoastlineDefinedWater();
            coastlines.ForEach(cl => GenerateCoastlineLBorder(cl, EMPTY_AREA_COLOR));
        }

        private void GenerateCoastlineLBorder(Coastline coastline, SKColor? color = null)
            => Image.DrawPath(coastline.Path, color ?? COASTLINE_BORDER_COLOR, Coastline.BORDER_WIDTH);

        private void GenerateCoastlineLSideIndicator(Coastline coastline)
            => Image.DrawArea(coastline.SideIndicator, COASTLINE_WATER_SIDE_COLOR);

        private void FillCoastlineDefinedWater()
        {
            Image.CacheBitmap();
            var coastlineAreas = new List<CoastlineArea>
                { null }; // 0 idx is undefined coastline

            var coastlineAreaIdxByTile = new uint[MapSize.width, MapSize.height];
            uint coastlineAreaIdx = 1;
            for (int x = 0; x < MapSize.width; x++)
                for (int y = 0; y < MapSize.height; y++)
                {
                    if (coastlineAreaIdxByTile[x, y] > 0)
                        continue;

                    var pixelColor = Image.GetPixelSKColor((x, y));
                    if (pixelColor != EMPTY_AREA_COLOR)
                        continue;

                    coastlineAreas.Add(GetCoastlineArea(
                        coastlineAreaIdxByTile, coastlineAreaIdx, x, y
                    ));
                    coastlineAreaIdx++;
                }

            var coastlinieAreaIsWater = coastlineAreas
                .Select(ca => ca?.IsWater() ?? false)
                .ToArray();

            for (int x = 0; x < MapSize.width; x++)
                for (int y = 0; y < MapSize.height; y++)
                {
                    var cAreaIdx = coastlineAreaIdxByTile[x, y];
                    if (cAreaIdx != NO_COASTLINE_AREA_IDX && coastlinieAreaIsWater[cAreaIdx])
                        // TODO flip vertical with (MapSize.height - y - 1), is there a better way?
                        Image.DrawPixel((x, MapSize.height - y - 1), COASTLINE_WATER_SIDE_COLOR);
                }
        }

        private CoastlineArea GetCoastlineArea(
            uint[,] coastlineAreaIdxByTile, uint idx, int x, int y
        )
        {
            var coastlineArea = new CoastlineArea(x, y);
            var pointsStack = new Queue<(int x, int y)>();
            pointsStack.Enqueue(coastlineArea.InitialPoint);
            coastlineAreaIdxByTile[x, y] = idx;

            while (pointsStack.Count > 0)
            {
                var point = pointsStack.Dequeue();

                foreach (var deltaXY in ADJACENT_COORDS)
                {
                    var adjPoint = (x: point.x + deltaXY.x, y: point.y + deltaXY.y);
                    if (adjPoint.x < 0
                        || adjPoint.x >= MapSize.width
                        || adjPoint.y < 0
                        || adjPoint.y >= MapSize.height
                        || coastlineAreaIdxByTile[adjPoint.x, adjPoint.y] > 0
                    ) continue;

                    var adjacentPixelColor = Image.GetPixelSKColor(adjPoint);
                    if (adjacentPixelColor == EMPTY_AREA_COLOR)
                    {
                        coastlineAreaIdxByTile[adjPoint.x, adjPoint.y] = idx;
                        pointsStack.Enqueue(adjPoint);
                    }
                    else if (adjacentPixelColor == COASTLINE_BORDER_COLOR)
                    {
                        coastlineAreaIdxByTile[adjPoint.x, adjPoint.y] = NO_COASTLINE_AREA_IDX;
                        coastlineArea.AdjacentLandBorderPixels++;
                    }
                    else if (adjacentPixelColor == COASTLINE_WATER_SIDE_COLOR)
                    {
                        // ... same as:
                        // coastlineAreaIdxByTile[x, y] = idx;
                        // but it doesn't need to paint the water color again
                        coastlineAreaIdxByTile[adjPoint.x, adjPoint.y] = NO_COASTLINE_AREA_IDX;
                        coastlineArea.AdjacentWaterBorderPixels++;
                    }
                }
            }

            return coastlineArea;
        }

        private void GenerateLandArea(LandArea landArea)
            => Image.DrawArea(landArea.Path, landArea.FillColor);

        private void GenerateRiver(River river)
            => Image.DrawPath(
                river.Path,
                MapColors.DEEP_WATER_COLOR,
                MapProvider.PixelsPerMeter * river.Width
            );

        private static List<Road> GetRoadsByColor(List<Road> roads, Func<Color, bool> condition)
            => roads
                .Where(road => MapColors.ROAD_COLORS.ContainsKey(road.Type)
                    && condition(MapColors.ROAD_COLORS[road.Type]))
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
        {
            Image.DrawArea(building.Path, MapColors.FLOOR_COLOR);
            Image.DrawPath(building.Path, MapColors.WALL_COLOR, Building.WALL_WIDTH);
        }
    }
}
