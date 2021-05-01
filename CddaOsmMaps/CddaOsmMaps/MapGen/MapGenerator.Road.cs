using CddaOsmMaps.MapGen.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CddaOsmMaps.MapGen
{
    partial class MapGenerator
    {
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
    }
}
