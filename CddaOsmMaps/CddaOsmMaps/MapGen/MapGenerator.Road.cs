using CddaOsmMaps.Cdda;
using CddaOsmMaps.MapGen.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace CddaOsmMaps.MapGen
{
    partial class MapGenerator
    {
        private const bool DO_NOT_FORCE_CONNECTED_ROADS = false;

        private void GenerateAllRoads(List<Road> roads)
        {
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

            GenerateOvermapRoads(roads);
        }

        private void GenerateOvermapRoads(List<Road> roads)
        {
            var overmapRoadsInfo
                = new OvermapRoad[OvermapSize.Width, OvermapSize.Height];

            roads.Where(r => Road.ROAD_TYPES_FOR_OVERMAP.Contains(r.Type))
                .ToList()
                .ForEach(road => GenerateOvermapRoad(overmapRoadsInfo, road));

            var overmapForestTrailsInfo
                = new OvermapRoad[OvermapSize.Width, OvermapSize.Height];

            roads.Where(r => Road.FOREST_TRAIL_TYPES_FOR_OVERMAP.Contains(r.Type))
                .ToList()
                .ForEach(road => GenerateOvermapRoad(overmapForestTrailsInfo, road));

            for (int x = 0; x < OvermapSize.Width; x++)
                for (int y = 0; y < OvermapSize.Height; y++)
                {
                    var roadTerrainType = overmapRoadsInfo[x, y]?
                        .GetOvermapRoadTerrainType();

                    if (roadTerrainType.HasValue)
                        Overmap[x, y] = roadTerrainType.Value;
                    else
                    {
                        var foresTrailTerrainType = overmapForestTrailsInfo[x, y]?
                            .GetOvermapForestTrailTerrainType();

                        if (foresTrailTerrainType.HasValue)
                            Overmap[x, y] = foresTrailTerrainType.Value;
                    }
                }
        }

        private void GenerateOvermapRoad(OvermapRoad[,] overmapRoadsInfo, Road road)
            => road
                .Polygons
                .ForEach(polygon => GenerateOvermapRoad(
                    overmapRoadsInfo,
                    polygon.Select(GetOvermapTile).ToList()
                ));

        private void GenerateOvermapRoad(
            OvermapRoad[,] overmapRoadsInfo,
            List<Point> roadPath
        ) => Enumerable
            .Range(0, roadPath.Count - 1)
            .ToList()
            .ForEach(idx => GenerateOvermapRoadSegment(
                overmapRoadsInfo,
                roadPath[idx],
                roadPath[idx + 1]
            ));

        private void GenerateOvermapRoadSegment(
            OvermapRoad[,] overmapRoadsInfo,
            Point from,
            Point to
        )
        {
            // TODO GenerateOvermapRoadSegment refactor his insanity

            // reversed x <-> y
            // TODO flip vertical with (MapSize.height - y - 1), is there a better way?
            from = new Point(from.Y, OvermapSize.Height - from.X - 1);
            to = new Point(to.Y, OvermapSize.Height - to.X - 1);

            if (from == to)
                return;

            var xDiff = Math.Abs(from.X - to.X);
            var yDiff = Math.Abs(from.Y - to.Y);

            if (xDiff >= yDiff)
            {
                if (from.X > to.X) // orientation does not matter
                    (from, to) = (to, from);

                var floatY = 0.5f + from.Y;
                var prevY = from.Y;
                var yStep = (float)(to.Y - from.Y) / (to.X - from.X);
                SetOvermapRoadInfo(overmapRoadsInfo, from.X, from.Y, r => r.HasEast = true);
                for (var x = from.X + 1; x < to.X; x++)
                {
                    floatY += yStep;
                    var intY = (int)floatY;
                    if (DO_NOT_FORCE_CONNECTED_ROADS || prevY == intY)
                    {
                        SetOvermapRoadInfo(overmapRoadsInfo, x, intY, r =>
                        {
                            r.HasEast = true;
                            r.HasWest = true;
                        });
                    }
                    else if (prevY < intY)
                    {
                        // TODO choose (x, y-1) VS (x-1, y) based on angle ?
                        SetOvermapRoadInfo(overmapRoadsInfo, x, intY - 1, r =>
                        {
                            r.HasWest = true;
                            r.HasSouth = true;
                        });
                        SetOvermapRoadInfo(overmapRoadsInfo, x, intY, r =>
                        {
                            r.HasEast = true;
                            r.HasNorth = true;
                        });
                    }
                    else // if (prevY > intY)
                    {
                        // TODO choose (x, y+1) VS (x-1, y) based on angle ?
                        SetOvermapRoadInfo(overmapRoadsInfo, x, intY + 1, r =>
                        {
                            r.HasWest = true;
                            r.HasNorth = true;
                        });
                        SetOvermapRoadInfo(overmapRoadsInfo, x, intY, r =>
                        {
                            r.HasEast = true;
                            r.HasSouth = true;
                        });
                    }

                    prevY = (int)floatY;
                }

                if (DO_NOT_FORCE_CONNECTED_ROADS || prevY == to.Y)
                    SetOvermapRoadInfo(overmapRoadsInfo, to.X, to.Y, r => r.HasWest = true);

                else if (prevY < to.Y)
                {
                    // TODO choose (x, y-1) VS (x-1, y) based on angle ?
                    SetOvermapRoadInfo(overmapRoadsInfo, to.X, to.Y - 1, r =>
                    {
                        r.HasWest = true;
                        r.HasSouth = true;
                    });
                    SetOvermapRoadInfo(overmapRoadsInfo, to.X, to.Y, r => r.HasNorth = true);
                }
                else // if (prevY > to.Y)
                {
                    // TODO choose (x, y+1) VS (x-1, y) based on angle ?
                    SetOvermapRoadInfo(overmapRoadsInfo, to.X, to.Y + 1, r =>
                    {
                        r.HasWest = true;
                        r.HasNorth = true;
                    });
                    SetOvermapRoadInfo(overmapRoadsInfo, to.X, to.Y, r => r.HasSouth = true);
                }
            }
            else
            {
                if (from.Y > to.Y) // orientation does not matter
                    (from, to) = (to, from);

                var floatX = 0.5f + from.X;
                var prevX = from.X;
                var xStep = (float)(to.X - from.X) / (to.Y - from.Y);
                SetOvermapRoadInfo(overmapRoadsInfo, from.X, from.Y, r => r.HasSouth = true);
                for (var y = from.Y + 1; y < to.Y; y++)
                {
                    floatX += xStep;
                    var intX = (int)floatX;
                    if (DO_NOT_FORCE_CONNECTED_ROADS || prevX == intX)
                    {
                        SetOvermapRoadInfo(overmapRoadsInfo, intX, y, r =>
                        {
                            r.HasSouth = true;
                            r.HasNorth = true;
                        });
                    }
                    else if (prevX < intX)
                    {
                        // TODO choose (x-1, y) VS (x, y-1) based on angle ?
                        SetOvermapRoadInfo(overmapRoadsInfo, intX - 1, y, r =>
                        {
                            r.HasNorth = true;
                            r.HasEast = true;
                        });
                        SetOvermapRoadInfo(overmapRoadsInfo, intX, y, r =>
                        {
                            r.HasSouth = true;
                            r.HasWest = true;
                        });
                    }
                    else // if (prevX > intX)
                    {
                        // TODO choose (x+1, y) VS (x, y-1) based on angle ?
                        SetOvermapRoadInfo(overmapRoadsInfo, intX + 1, y, r =>
                        {
                            r.HasNorth = true;
                            r.HasWest = true;
                        });
                        SetOvermapRoadInfo(overmapRoadsInfo, intX, y, r =>
                        {
                            r.HasSouth = true;
                            r.HasEast = true;
                        });
                    }

                    prevX = (int)floatX;
                }

                if (DO_NOT_FORCE_CONNECTED_ROADS || prevX == to.X)
                    SetOvermapRoadInfo(overmapRoadsInfo, to.X, to.Y, r => r.HasNorth = true);

                else if (prevX < to.X)
                {
                    // TODO choose (x-1, y) VS (x, y-1) based on angle ?
                    SetOvermapRoadInfo(overmapRoadsInfo, to.X - 1, to.Y, r =>
                    {
                        r.HasNorth = true;
                        r.HasEast = true;
                    });
                    SetOvermapRoadInfo(overmapRoadsInfo, to.X, to.Y, r => r.HasWest = true);
                }
                else // if (prevX > to.X)
                {
                    // TODO choose (x+1, y) VS (x, y-1) based on angle ?
                    SetOvermapRoadInfo(overmapRoadsInfo, to.X + 1, to.Y, r =>
                    {
                        r.HasNorth = true;
                        r.HasWest = true;
                    });
                    SetOvermapRoadInfo(overmapRoadsInfo, to.X, to.Y, r => r.HasEast = true);
                }
            }
        }

        private void SetOvermapRoadInfo(
            OvermapRoad[,] overmapRoadsInfo,
            int x,
            int y,
            Action<OvermapRoad> action)
        {
            var isInBounds =
                0 < x && x < OvermapSize.Width - 1
                && 0 < y && y < OvermapSize.Height - 1;

            if (!isInBounds)
                return;

            if (overmapRoadsInfo[x, y] == null)
                overmapRoadsInfo[x, y] = new OvermapRoad();

            action(overmapRoadsInfo[x, y]);
        }

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
            => MapImage.DrawComplexPath(
                road.Polygons,
                MapColors.ROAD_COLORS[road.Type],
                MapProvider.PixelsPerMeter * road.Width
            );

        private void GenerateSidewalk(Road road)
            => MapImage.DrawComplexPath(
                road.Polygons,
                MapColors.SIDEWALK_COLOR,
                MapProvider.PixelsPerMeter * road.SidewalkWidth
            );

        private static Point GetOvermapTile(Vector2 absPos)
            => new Point(
                (int)(absPos.X / CddaMap.OVERMAP_TILE_SIZE),
                (int)(absPos.Y / CddaMap.OVERMAP_TILE_SIZE)
            );
    }
}
