using CddaOsmMaps.Cdda;
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
    internal partial class MapGenerator : IMapGenerator
    {
        private readonly IMapProvider MapProvider;
        private ImageBuilder MapImage;
        private ImageBuilder OvermapImage;
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
            MapImage = new ImageBuilder(MapSize, EMPTY_AREA_COLOR, log);
            OvermapImage = new ImageBuilder(OvermapSize, EMPTY_AREA_COLOR, log);

            var coastLines = mapElements.Coastlines.ToList();
            if (coastLines.Count > 0)
                GenerateCoastlines(coastLines);

            mapElements.LandAreas
                .Where(la => la.IsVisible)
                .ToList()
                .ForEach(GenerateLandArea);

            var roads = mapElements.Roads.ToList();
            GenerateAllRoads(roads);

            var buildings = mapElements.Buildings.ToList();
            GenerateBuildings(buildings);

            OvermapImageToOvermap();

            if (!string.IsNullOrEmpty(imgPath))
                MapImage.Save(imgPath);
            else
                MapImage.CacheBitmap();

            MapImage.DisposeBuldingProperties();
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
            var pixelColor = MapImage.GetPixelColor(tilePos);
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

        private void GenerateLandArea(LandArea landArea)
        {
            MapImage.DrawComplexArea(landArea.Polygons, landArea.FillColor);
            if (landArea.IsWater)
            {
                var overmapPolygons = ToOvermapPolygons(landArea.Polygons);
                // TODO water overmap tiles from landareas does not have WAter100% marked to avoid generating map
                OvermapImage.DrawComplexArea(overmapPolygons, landArea.FillColor);
            }
        }

        private void OvermapImageToOvermap()
        {
            OvermapImage.CacheBitmap();

            for (int x = 0; x < OvermapSize.Width; x++)
                for (int y = 0; y < OvermapSize.Height; y++)
                {
                    if (Overmap[x, y] != OvermapTerrainType.Default)
                        continue;

                    var pixelColor = OvermapImage.GetPixelColor(new Point(x, y));

                    if (pixelColor == MapColors.FLOOR_COLOR)
                        Overmap[x, y] = OvermapTerrainType.HouseDefault;

                    else if (pixelColor == MapColors.DEEP_WATER_COLOR)
                        // TODO water overmap tiles from landareas does not have WAter100% marked to avoid generating map
                        Overmap[x, y] = OvermapTerrainType.Water;
                }
        }

        private static List<Polygon> ToOvermapPolygons(List<Polygon> mapPolygons)
            => mapPolygons
                .Select(polygon =>
                    new Polygon(polygon.Select(point => point / CddaMap.OVERMAP_TILE_SIZE))
                )
                .ToList();
    }
}
