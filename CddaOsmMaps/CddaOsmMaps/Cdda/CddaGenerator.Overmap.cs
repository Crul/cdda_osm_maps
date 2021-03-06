using CddaOsmMaps.Crosscutting;
using CddaOsmMaps.MapGen.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CddaOsmMaps.Cdda
{
    partial class CddaGenerator
    {
        private const string OVERMAP_FILES_WILDCARD_PATTERN = "o.??.??";
        private readonly Regex OVERMAP_FILES_REGEX = new Regex(@"^o\.-?\d+\.-?\d+$");

        private readonly Func<string, string> OVERMAP_SEEN_FILES_WILDCARD_PATTERN =
            saveId => $"{saveId}.seen.??.??";

        private readonly Regex OVERMAP_SEEN_FILES_REGEX
            = new Regex(@"^saveGameId\.seen\.-?\d+\.-?\d+$");

        private readonly Func<Point, string> GET_OVERMAP_REGION_FILE =
            om => $"o.{om.X}.{om.Y}";

        private readonly Func<string, Point, string> GET_OVERMAP_REGION_SEEN_FILE =
            (saveId, om) => $"{saveId}.seen.{om.X}.{om.Y}";

        private const int OVERMAP_REGION_LAYER_SIZE = 32400;
        private const int OVERMAP_REGION_LAYERS_BELOW_0 = 10;
        private const int OVERMAP_REGION_LAYERS_ABOVE_0 = 10;
        private const int EMPTY_SEEN_DATA_COUNT = 21;
        private const string OVERMAP_TILE_ROCK_VALUE = "empty_rock";
        private const string OVERMAP_TILE_AIR_VALUE = "open_air";

        private readonly Dictionary<OvermapTerrainType, string> OVERMAP_TILE_PER_TERRAIN =
            new Dictionary<OvermapTerrainType, string>
            {
                { OvermapTerrainType.Default,                  "field" },

                { OvermapTerrainType.Water,                    "lake_surface" },
                { OvermapTerrainType.Water100Percent,          "lake_surface" },

                { OvermapTerrainType.HouseDefault,             "house_01_north" },

                { OvermapTerrainType.RoadNorth,                "road_end_north" },
                { OvermapTerrainType.RoadSouth,                "road_end_south" },
                { OvermapTerrainType.RoadWest,                 "road_end_west" },
                { OvermapTerrainType.RoadEast,                 "road_end_east" },
                { OvermapTerrainType.RoadNorthEast,            "road_ne" },
                { OvermapTerrainType.RoadNorthSouth,           "road_ns" },
                { OvermapTerrainType.RoadSouthWest,            "road_sw" },
                { OvermapTerrainType.RoadEastSouth,            "road_es" },
                { OvermapTerrainType.RoadEastWest,             "road_ew" },
                { OvermapTerrainType.RoadWestNorth,            "road_wn" },
                { OvermapTerrainType.RoadNorthEastSouth,       "road_nes" },
                { OvermapTerrainType.RoadNorthEastWest,        "road_new" },
                { OvermapTerrainType.RoadNorthSouthWest,       "road_nsw" },
                { OvermapTerrainType.RoadEastSouthWest,        "road_esw" },
                { OvermapTerrainType.RoadNorthEastSouthWest,   "road_nesw" },

                { OvermapTerrainType.ForestTrailNorth,                "forest_trail_end_north" },
                { OvermapTerrainType.ForestTrailSouth,                "forest_trail_end_south" },
                { OvermapTerrainType.ForestTrailWest,                 "forest_trail_end_west" },
                { OvermapTerrainType.ForestTrailEast,                 "forest_trail_end_east" },
                { OvermapTerrainType.ForestTrailNorthEast,            "forest_trail_ne" },
                { OvermapTerrainType.ForestTrailNorthSouth,           "forest_trail_ns" },
                { OvermapTerrainType.ForestTrailSouthWest,            "forest_trail_sw" },
                { OvermapTerrainType.ForestTrailEastSouth,            "forest_trail_es" },
                { OvermapTerrainType.ForestTrailEastWest,             "forest_trail_ew" },
                { OvermapTerrainType.ForestTrailWestNorth,            "forest_trail_wn" },
                { OvermapTerrainType.ForestTrailNorthEastSouth,       "forest_trail_nes" },
                { OvermapTerrainType.ForestTrailNorthEastWest,        "forest_trail_new" },
                { OvermapTerrainType.ForestTrailNorthSouthWest,       "forest_trail_nsw" },
                { OvermapTerrainType.ForestTrailEastSouthWest,        "forest_trail_esw" },
                { OvermapTerrainType.ForestTrailNorthEastSouthWest,   "forest_trail_nesw" },
            };

        private void WriteOvermapFiles()
        {
            DeleteFiles(OVERMAP_FILES_WILDCARD_PATTERN, OVERMAP_FILES_REGEX);
            RunPerOvermapRegion(WriteOvermapFile);
        }

        private void WriteSeenOvermapFiles()
        {
            DeleteFiles(OVERMAP_SEEN_FILES_WILDCARD_PATTERN(SaveId), OVERMAP_SEEN_FILES_REGEX);
            RunPerOvermapRegion(WriteSeenOvermapFile);
        }

        private void RunPerOvermapRegion(Action<Point> action)
        {
            var overmapRegionXFrom = MapTopLeftCoords.OvermapRegion.X;
            var overmapRegionXTo = MapBotRghtCoords.OvermapRegion.X;

            var overmapRegionYFrom = MapTopLeftCoords.OvermapRegion.Y;
            var overmapRegionYTo = MapBotRghtCoords.OvermapRegion.Y;

            var overmapRegionXRange = EnumExt.RangeFromTo(overmapRegionXFrom, overmapRegionXTo);
            var overmapRegionYRange = EnumExt.RangeFromTo(overmapRegionYFrom, overmapRegionYTo);

            foreach (var overmapRegionX in overmapRegionXRange)
                foreach (var overmapRegionY in overmapRegionYRange)
                    action(new Point(overmapRegionX, overmapRegionY));
        }

        private void WriteOvermapFile(Point overmapRegion)
        {
            static int toAbsOvermapTile(int relOvermapTile, int overmapRegion, int topLeftOvermapTile)
                => relOvermapTile
                    + (
                        overmapRegion
                        * CddaMap.OVERMAP_REGION_SIZE_IN_OVERMAP_TILES
                    )
                    - topLeftOvermapTile;

            var terrain = new List<string>();
            // reversed x <-> y loops
            foreach (var relOvermapTileY in EnumExt.Range(CddaMap.OVERMAP_REGION_SIZE_IN_OVERMAP_TILES))
                foreach (var relOvermapTileX in EnumExt.Range(CddaMap.OVERMAP_REGION_SIZE_IN_OVERMAP_TILES))
                {
                    var absOvermapTile = new Point(
                        // reversed x <-> y
                        toAbsOvermapTile(
                            relOvermapTileY,
                            overmapRegion.Y,
                            MapTopLeftCoords.OvermapTile.Y
                        ),
                        toAbsOvermapTile(
                            relOvermapTileX,
                            overmapRegion.X,
                            MapTopLeftCoords.OvermapTile.X
                        )
                    );

                    var overmapTileType = OVERMAP_TILE_PER_TERRAIN[
                        MapGen.GetOvermapTerrain(absOvermapTile)
                    ];

                    terrain.Add(overmapTileType);
                }

            // only LEVEL 0 is supported
            var emptyRockArray = new object[] {
                new object[] { OVERMAP_TILE_ROCK_VALUE, OVERMAP_REGION_LAYER_SIZE }
            };

            var level0Array = SimplifyOvermap(terrain);

            var openAirArray = new object[] {
                new object[] { OVERMAP_TILE_AIR_VALUE, OVERMAP_REGION_LAYER_SIZE }
            };

            var layers = Enumerable.Repeat(emptyRockArray, OVERMAP_REGION_LAYERS_BELOW_0)
                .Concat(new object[] { level0Array })
                .Concat(Enumerable.Repeat(openAirArray, OVERMAP_REGION_LAYERS_ABOVE_0));

            var emptyArray = Array.Empty<int>();
            var overmapRegionData = new
            {
                layers,
                region_id = OVERMAP_REGION_FILE_REGION_ID_VALUE,
                monster_groups = emptyArray,
                cities = emptyArray,
                connections_out = new { },
                radios = emptyArray,
                monster_map = emptyArray,
                tracked_vehicles = emptyArray,
                scent_traces = emptyArray,
                npcs = emptyArray,
                camps = emptyArray,
                overmap_special_placements = emptyArray,
            };

            var overmapRegionFilepath = Path.Combine(
                SavePath,
                GET_OVERMAP_REGION_FILE(overmapRegion)
            );

            JsonIO.WriteJson<dynamic>(
                overmapRegionFilepath,
                overmapRegionData,
                header: SAVE_VERSION_HEADER
            );
        }

        private void WriteSeenOvermapFile(Point overmapRegion)
        {
            var allFalseArray = new object[] { new object[] { false, 32400 } };
            var allFalseArrays = Enumerable
                .Repeat(allFalseArray, EMPTY_SEEN_DATA_COUNT);

            var emptyArray = Array.Empty<int>();
            var emptyArrays = Enumerable
                .Repeat(emptyArray, EMPTY_SEEN_DATA_COUNT);

            var emptyOvermapSeen = new
            {
                visible = allFalseArrays,
                explored = allFalseArrays,
                notes = emptyArrays,
                extras = emptyArrays
            };

            var overmapRegionSeenFilepath = Path.Combine(
                SavePath,
                GET_OVERMAP_REGION_SEEN_FILE(SaveId, overmapRegion)
            );

            JsonIO.WriteJson<dynamic>(
                overmapRegionSeenFilepath,
                emptyOvermapSeen,
                header: SAVE_VERSION_HEADER
            );
        }

        private static object[] SimplifyOvermap(List<string> terrain)
            => SimplifyTiles(terrain, ProcessOvermapTile);

        private static void ProcessOvermapTile(
            List<object> simplified,
            (string tile, int count) tmpTileInfo
        ) => ProcessTile(
            simplified,
            tmpTileInfo,
            singleTileAsArray: true
        );
    }
}
