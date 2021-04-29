using CddaOsmMaps.Cdda;
using CddaOsmMaps.Crosscutting;
using CddaOsmMaps.MapGen.Contracts;
using CddaOsmMaps.MapGen.Entities;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

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

        private static readonly List<Point> ADJACENT_COORDS
            = new List<(int x, int y)> { (1, 0), (-1, 0), (0, 1), (0, -1) }
                .Select(p => new Point(p.x, p.y))
                .ToList();

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

        public readonly Size mapSize;
        public Size MapSize { get => mapSize; }

        public readonly Size overmapSize;
        public Size OvermapSize { get => overmapSize; }

        public readonly OvermapTerrainType[,] Overmap;

        public MapGenerator(IMapProvider mapProvider)
        {
            MapProvider = mapProvider;
            overmapSize = new Size(
                (int)Math.Floor((double)MapProvider.MapSize.Width / CddaMap.OVERMAP_TILE_SIZE),
                (int)Math.Floor((double)MapProvider.MapSize.Height / CddaMap.OVERMAP_TILE_SIZE)
            );
            mapSize = overmapSize * CddaMap.OVERMAP_TILE_SIZE;
            Overmap = new OvermapTerrainType[overmapSize.Width, overmapSize.Height];
        }

        public void Generate(string imgPath = "", bool log = false)
        {
            var mapElements = MapProvider.GetMapElements();
            Image = new ImageBuilder(MapSize, EMPTY_AREA_COLOR, log);

            var coastLines = mapElements.Coastlines.ToList();
            if (coastLines.Count > 0)
                GenerateCoastlines(coastLines);

            mapElements.LandAreas
                .Where(la => la.IsVisible)
                .ToList()
                .ForEach(GenerateLandArea);

            mapElements.Rivers.ToList().ForEach(GenerateRiver);

            var roads = mapElements.Roads.ToList();
            var dirtRoads = GetRoadsByColor(
                roads,
                roadColor => roadColor == MapColors.DIRT_FLOOR_COLOR
            );
            var nonDirtRoads = GetRoadsByColor(
                roads,
                roadColor => roadColor != MapColors.DIRT_FLOOR_COLOR
            );

            GenerateRoads(dirtRoads);
            GenerateRoads(nonDirtRoads);

            mapElements.Buildings.ToList().ForEach(GenerateBuilding);

            if (!string.IsNullOrEmpty(imgPath))
                Image.Save(imgPath);
            else
                Image.CacheBitmap();

            Image.DisposeBuldingProperties();
        }

        public TerrainType GetTerrain(Point tilePos)
        {
            // TODO remove checking (map should be cropped to full overmap tiles) ?
            var isPixelInImg = (
                0 <= tilePos.X && tilePos.X < MapSize.Width
                && 0 <= tilePos.Y && tilePos.Y < MapSize.Height
            );
            if (!isPixelInImg)
            {
                Console.WriteLine($"WARNING: pixel not in image: {tilePos}, imgSize: {mapSize}");
                return TerrainType.Default;
            }
            var pixelColor = Image.GetPixelColor(tilePos);
            if (TERRAIN_TYPES_BY_COLOR.TryGetValue(pixelColor, out var terrainType))
                return terrainType;

            return TerrainType.Default;
        }

        public OvermapTerrainType GetOvermapTerrain(Point overmapTilePos)
        {
            // reversed x <-> y
            var overmapX = overmapTilePos.Y;
            var overmapY = overmapTilePos.X;

            // check neede because only full overmap regions can be generated
            var isPositionInBounds = (
                0 <= overmapX && overmapX < OvermapSize.Width
                && 0 <= overmapY && overmapY < OvermapSize.Height
            );

            return isPositionInBounds
                ? Overmap[overmapX, overmapY]
                : OvermapTerrainType.Default;
        }


        private void GenerateCoastlines(List<Coastline> coastlines)
        {
            coastlines.ForEach(GenerateCoastlineLSideIndicator);
            coastlines.ForEach(cl => GenerateCoastlineLBorder(cl));
            FillCoastlineDefinedWater();
            coastlines.ForEach(cl => GenerateCoastlineLBorder(cl, EMPTY_AREA_COLOR));
        }

        private void GenerateCoastlineLBorder(Coastline coastline, SKColor? color = null)
            => Image.DrawComplexPath(
                coastline.Polygons,
                color ?? COASTLINE_BORDER_COLOR,
                Coastline.BORDER_WIDTH
            );

        private void GenerateCoastlineLSideIndicator(Coastline coastline)
            => Image.DrawComplexArea(
                coastline.SideIndicators,
                COASTLINE_WATER_SIDE_COLOR
            );

        private void FillCoastlineDefinedWater()
        {
            Image.CacheBitmap();
            var coastlineAreas = new List<CoastlineArea>
                { null }; // 0 idx is undefined coastline

            var coastlineAreaIdxByTile = new uint[MapSize.Width, MapSize.Height];
            uint coastlineAreaIdx = 1;
            for (int x = 0; x < MapSize.Width; x++)
                for (int y = 0; y < MapSize.Height; y++)
                {
                    if (coastlineAreaIdxByTile[x, y] > 0)
                        continue;

                    var point = new Point(x, y);
                    var pixelColor = Image.GetPixelSKColor(point);
                    if (pixelColor != EMPTY_AREA_COLOR)
                        continue;

                    coastlineAreas.Add(GetCoastlineArea(
                        coastlineAreaIdxByTile,
                        coastlineAreaIdx,
                        point
                    ));
                    coastlineAreaIdx++;
                }

            var coastlinieAreaIsWater = coastlineAreas
                .Select(ca => ca?.IsWater() ?? false)
                .ToArray();

            var waterTilesInOvermapTile = new uint[OvermapSize.Width, OvermapSize.Height];
            for (int x = 0; x < MapSize.Width; x++)
                for (int y = 0; y < MapSize.Height; y++)
                {
                    var cAreaIdx = coastlineAreaIdxByTile[x, y];
                    if (cAreaIdx != NO_COASTLINE_AREA_IDX && coastlinieAreaIsWater[cAreaIdx])
                    {
                        // TODO flip vertical with (MapSize.height - y - 1), is there a better way?
                        Image.DrawPixel(new Vector2(x, MapSize.Height - y - 1), COASTLINE_WATER_SIDE_COLOR);

                        var overmapTile = GetOvermapTile(new Point(x, y));
                        waterTilesInOvermapTile[overmapTile.X, overmapTile.Y]++;
                    }
                }

            var overmapTiles = Math.Pow(CddaMap.OVERMAP_TILE_SIZE, 2);
            var minWaterTilesForWaterOvermapTile = overmapTiles / 2;

            for (int x = 0; x < OvermapSize.Width; x++)
                for (int y = 0; y < OvermapSize.Height; y++)
                    if (waterTilesInOvermapTile[x, y] == overmapTiles)
                        // set overmap tile as 100% water (to avoid rendering overmap tile file)
                        Overmap[x, y] = OvermapTerrainType.Water100Percent;

                    else if (waterTilesInOvermapTile[x, y] >= minWaterTilesForWaterOvermapTile)
                        // set overmap tile as partial water (to force rendering overmap tile file)
                        Overmap[x, y] = OvermapTerrainType.Water;
        }

        private CoastlineArea GetCoastlineArea(
            uint[,] coastlineAreaIdxByTile,
            uint idx,
            Point initialPoint
        )
        {
            var coastlineArea = new CoastlineArea(initialPoint);
            var pointsStack = new Queue<Point>();
            pointsStack.Enqueue(coastlineArea.InitialPoint);
            coastlineAreaIdxByTile[initialPoint.X, initialPoint.Y] = idx;

            while (pointsStack.Count > 0)
            {
                var point = pointsStack.Dequeue();

                foreach (var deltaXY in ADJACENT_COORDS)
                {
                    var adjPoint = new Point(point.X + deltaXY.X, point.Y + deltaXY.Y);
                    if (adjPoint.X < 0
                        || adjPoint.X >= MapSize.Width
                        || adjPoint.Y < 0
                        || adjPoint.Y >= MapSize.Height
                        || coastlineAreaIdxByTile[adjPoint.X, adjPoint.Y] > 0
                    ) continue;

                    var adjacentPixelColor = Image.GetPixelSKColor(adjPoint);
                    if (adjacentPixelColor == EMPTY_AREA_COLOR)
                    {
                        coastlineAreaIdxByTile[adjPoint.X, adjPoint.Y] = idx;
                        pointsStack.Enqueue(adjPoint);
                    }
                    else if (adjacentPixelColor == COASTLINE_WATER_SIDE_COLOR)
                    {
                        // ... same as:
                        // coastlineAreaIdxByTile[x, y] = idx;
                        // but it doesn't need to paint the water color again
                        coastlineAreaIdxByTile[adjPoint.X, adjPoint.Y] = NO_COASTLINE_AREA_IDX;
                        coastlineArea.AdjacentWaterBorderPixels++;
                    }
                    else if (adjacentPixelColor == COASTLINE_BORDER_COLOR)
                    {
                        coastlineAreaIdxByTile[adjPoint.X, adjPoint.Y] = NO_COASTLINE_AREA_IDX;
                        coastlineArea.AdjacentLandBorderPixels++;
                    }
                }
            }

            return coastlineArea;
        }

        private static Point GetOvermapTile(Point adjPoint)
            => new Point(
                adjPoint.X / CddaMap.OVERMAP_TILE_SIZE,
                adjPoint.Y / CddaMap.OVERMAP_TILE_SIZE
            );

        private void GenerateLandArea(LandArea landArea)
            => Image.DrawComplexArea(landArea.Polygons, landArea.FillColor);

        private void GenerateRiver(River river)
            => Image.DrawComplexPath(
                river.Polygons,
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
            => Image.DrawComplexPath(
                road.Polygons,
                MapColors.ROAD_COLORS[road.Type],
                MapProvider.PixelsPerMeter * road.Width
            );

        private void GenerateSidewalk(Road road)
            => Image.DrawComplexPath(
                road.Polygons,
                MapColors.SIDEWALK_COLOR,
                MapProvider.PixelsPerMeter * road.SidewalkWidth
            );

        private void GenerateBuilding(Building building)
        {
            Image.DrawComplexArea(building.Polygons, MapColors.FLOOR_COLOR);
            Image.DrawComplexPath(building.Polygons, MapColors.WALL_COLOR, Building.WALL_WIDTH);
        }
    }
}
