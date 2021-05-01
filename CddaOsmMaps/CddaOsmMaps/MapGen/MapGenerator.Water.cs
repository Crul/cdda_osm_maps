using CddaOsmMaps.Cdda;
using CddaOsmMaps.MapGen.Entities;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace CddaOsmMaps.MapGen
{
    partial class MapGenerator
    {
        private void GenerateCoastlines(List<Coastline> coastlines)
        {
            coastlines.ForEach(GenerateCoastlineLSideIndicator);
            coastlines.ForEach(cl => GenerateCoastlineLBorder(cl));
            FillCoastlineDefinedWater();
            coastlines.ForEach(cl => GenerateCoastlineLBorder(cl, EMPTY_AREA_COLOR));
        }

        private void GenerateCoastlineLBorder(Coastline coastline, SKColor? color = null)
            => MapImage.DrawComplexPath(
                coastline.Polygons,
                color ?? COASTLINE_BORDER_COLOR,
                Coastline.BORDER_WIDTH
            );

        private void GenerateCoastlineLSideIndicator(Coastline coastline)
            => MapImage.DrawComplexArea(
                coastline.SideIndicators,
                COASTLINE_WATER_SIDE_COLOR
            );

        private void FillCoastlineDefinedWater()
        {
            MapImage.CacheBitmap();
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
                    var pixelColor = MapImage.GetPixelSKColor(point);
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
                        MapImage.DrawPixel(new Vector2(x, MapSize.Height - y - 1), COASTLINE_WATER_SIDE_COLOR);

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

                    var adjacentPixelColor = MapImage.GetPixelSKColor(adjPoint);
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
    }
}
