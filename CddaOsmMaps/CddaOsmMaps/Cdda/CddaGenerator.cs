using CddaOsmMaps.Crosscutting;
using CddaOsmMaps.MapGen.Contracts;
using CddaOsmMaps.MapGen.Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;

namespace CddaOsmMaps.Cdda
{
    internal class CddaGenerator
    {
        private const string CDDA_SAVE_FOLDER = "save";
        private const string CDDA_SAVE_SEGMENTS_FOLDER = "maps";
        private const string MAIN_SAVE_FILE_EXT = ".sav";
        private const string OVERMAP_TILE_FILE_EXT = ".map";
        private const int SAVE_VERSION = 33;
        private readonly string SAVE_VERSION_HEADER = $"# version {SAVE_VERSION}\n";

        private const string SEEN_0_0_FILE_EXT = ".seen.0.0";
        private const int EMPTY_SEEN_DATA_COUNT = 21;

        private const string MAP_MEMORY_FILE_EXT = ".mm";
        private const string O_0_0_FILE = "o.0.0";

        private const string OVERTILE_REGION_X_KEY = "om_x";
        private const string OVERTILE_REGION_Y_KEY = "om_y";
        private const string LEVX_KEY = "levx";
        private const string LEVY_KEY = "levy";
        private const string PLAYER_KEY = "player";
        private const string PLAYER_POSX_KEY = "posx";
        private const string PLAYER_POSY_KEY = "posy";
        private const string ACTIVE_MONSTERS_KEY = "active_monsters";
        private const string STAIR_MONSTERS_KEY = "stair_monsters";

        private readonly bool Log;

        private readonly Dictionary<TerrainType, string> TILE_PER_TERRAIN =
            new Dictionary<TerrainType, string>
            {
                { TerrainType.Default,       "t_grass" },
                { TerrainType.DeepMovWater,  "t_water_moving_dp" },
                { TerrainType.Pavement,      "t_pavement" },
                { TerrainType.ConcreteFloor, "t_concrete" },
                { TerrainType.DirtFloor,     "t_dirt" },
                { TerrainType.Wall,          "t_concrete_wall" },
                { TerrainType.HouseFloor,    "t_thconc_floor" },
                { TerrainType.Grass,         "t_grass" },
                { TerrainType.GrassLong,     "t_grass_long" },
                { TerrainType.Sidewalk,      "t_sidewalk" },
            };

        private readonly string SavePath;
        private readonly string SaveId;
        private readonly IMapGenerator MapGen;
        private Point MapTopLeftAbsPos;

        public CddaGenerator(
            IMapGenerator mapGen,
            string cddaFolder,
            string saveGame,
            bool log
        )
        {
            MapGen = mapGen;
            SavePath = Path.Combine(cddaFolder, CDDA_SAVE_FOLDER, saveGame);
            SaveId = GetSaveId();
            Log = log;
        }

        public void Generate(Point? spawnAbsPos)
        {
            SetMapTopLeftAbsPos();
            WriteMapSegmenFiles();
            WriteSeen00();
            WriteOvermapFiles();
            WriteMapMemory();

            var playerSpawnCoord = GetSpawnCoords(spawnAbsPos);
            WriteMainSave(playerSpawnCoord);
            WriteSegments();
        }

        private void SetMapTopLeftAbsPos()
        {
            static float toMapSizeInOvermapTileUnits(int mapSize)
                => (float)mapSize / CddaMap.OVERMAP_TILE_SIZE;

            var mapSizeInOvermapTileUnits = (
                width: toMapSizeInOvermapTileUnits(MapGen.MapSize.Width),
                height: toMapSizeInOvermapTileUnits(MapGen.MapSize.Height)
            );

            if (Log
                && (mapSizeInOvermapTileUnits.width != (int)mapSizeInOvermapTileUnits.width
                || mapSizeInOvermapTileUnits.height != (int)mapSizeInOvermapTileUnits.height)
            )
                Console.WriteLine($"Map size not multiple of OvermapTile size: {MapGen.MapSize}");

            static float toMapSizeInWholeRegionUnits(int mapSize)
                => (float)Math.Ceiling((float)mapSize / CddaMap.OVERMAP_REGION_SIZE);

            var mapSizeInWholeRegionUnits = new Vector2(
                toMapSizeInWholeRegionUnits(MapGen.MapSize.Width),
                toMapSizeInWholeRegionUnits(MapGen.MapSize.Height)
            );

            var mapCenterInRegionUnits = mapSizeInWholeRegionUnits / 2;

            var mapCenterAbsPos = (
                x: (int)mapCenterInRegionUnits.X * CddaMap.OVERMAP_REGION_SIZE,
                y: (int)mapCenterInRegionUnits.Y * CddaMap.OVERMAP_REGION_SIZE
            );

            static int toMapTopLeftAbsPos(
                float mapSizeInOvermapTileUnits,
                int mapCenterAbsPos,
                int mapSize
            ) => (mapSizeInOvermapTileUnits % 2 == 0)
                ? mapCenterAbsPos - (mapSize / 2)
                : mapCenterAbsPos - (CddaMap.OVERMAP_TILE_SIZE * ((int)mapSizeInOvermapTileUnits - 1) / 2);

            MapTopLeftAbsPos = new Point(
                toMapTopLeftAbsPos(
                    mapSizeInOvermapTileUnits.width,
                    mapCenterAbsPos.x,
                    MapGen.MapSize.Width
                ),
                toMapTopLeftAbsPos(
                    mapSizeInOvermapTileUnits.width,
                    mapCenterAbsPos.x,
                    MapGen.MapSize.Width
                ));
        }

        private CddaPlayerCoords GetSpawnCoords(Point? spawnAbsPos)
        {
            if (!spawnAbsPos.HasValue)
                spawnAbsPos = new Point(
                    MapGen.MapSize.Width / 2,
                    MapGen.MapSize.Height / 2
                );

            return new CddaPlayerCoords(
                GetAbsPosFromRelMapPos(spawnAbsPos.Value)
            );
        }

        private void WriteSegments()
        {
            var mapTopLeftCoords = new CddaTileCoords(GetAbsPosFromRelMapPos(new Point(0, 0)));
            var mapBotRghtCoords = new CddaTileCoords(GetAbsPosFromRelMapPos(
                new Point(MapGen.MapSize.Width - 1, MapGen.MapSize.Height - 1)
            ));

            if (Log)
            {
                if (mapTopLeftCoords.RelPosInSubmap.X != 0
                    || mapTopLeftCoords.RelPosInSubmap.Y != 0
                    || mapTopLeftCoords.SubmapIdx.X != 0
                    || mapTopLeftCoords.SubmapIdx.Y != 0)
                    Console.WriteLine("Map Top Left corner not at (0,0) relative (to Overmap Tile) position");

                if (mapBotRghtCoords.RelPosInSubmap.X != CddaMap.SUBMAP_SIZE - 1
                    || mapBotRghtCoords.RelPosInSubmap.Y != CddaMap.SUBMAP_SIZE - 1
                    || mapBotRghtCoords.SubmapIdx.X != 1
                    || mapBotRghtCoords.SubmapIdx.Y != 1)
                    Console.WriteLine("Map Bottom Right corner not at (MAX,MAX) relative (to Overmap Tile) position");
            }

            var segmentFrom = new Point(
                mapTopLeftCoords.Segment.X,
                mapTopLeftCoords.Segment.Y
            );
            var segmentTo = new Point(
                mapBotRghtCoords.Segment.X,
                mapBotRghtCoords.Segment.Y
            );

            var segmentXRange = EnumExt.RangeCount(segmentFrom.X, segmentTo.X);
            var segmentYRange = EnumExt.RangeCount(segmentFrom.Y, segmentTo.Y);

            foreach (var segmentX in segmentXRange)
                foreach (var segmentY in segmentYRange)
                    WriteSegment(
                        mapTopLeftCoords,
                        mapBotRghtCoords,
                        segmentFrom,
                        segmentTo,
                        new Point(segmentX, segmentY)
                    );
        }

        private void WriteSegment(
            CddaTileCoords mapTopLeftCoords,
            CddaTileCoords mapBotRghtCoords,
            Point segmentFrom,
            Point segmentTo,
            Point segment
        )
        {
            var segmentPath = Path.Combine(
                SavePath,
                CDDA_SAVE_SEGMENTS_FOLDER,
                $"{segment.X}.{segment.Y}.0"
            );
            Directory.CreateDirectory(segmentPath);

            var overmapTileFileXFrom = (segment.X > segmentFrom.X)
                ? segment.X * CddaMap.OVERMAP_TILES_PER_SEGMENT
                : mapTopLeftCoords.OvermapTile.X
                    + (mapTopLeftCoords.RelPosInSubmap.X == 0 ? 0 : 1);

            var overmapTileFileXTo = (segment.X < segmentTo.X)
                ? (segment.X + 1) * CddaMap.OVERMAP_TILES_PER_SEGMENT
                : mapBotRghtCoords.OvermapTile.X
                    - (mapTopLeftCoords.RelPosInSubmap.X == CddaMap.SUBMAP_SIZE - 1 ? 0 : 1);

            var overmapTileFileXRange = EnumExt.RangeCount(overmapTileFileXFrom, overmapTileFileXTo);

            var overmapTileFileYFrom = (segment.Y > segmentFrom.Y)
                ? segment.Y * CddaMap.OVERMAP_TILES_PER_SEGMENT
                : mapTopLeftCoords.OvermapTile.Y
                    + (mapTopLeftCoords.RelPosInSubmap.Y == 0 ? 0 : 1);

            var overmapTileFileYTo = (segment.Y < segmentTo.Y)
                ? (segment.Y + 1) * CddaMap.OVERMAP_TILES_PER_SEGMENT
                : mapBotRghtCoords.OvermapTile.Y
                    - (mapTopLeftCoords.RelPosInSubmap.Y == CddaMap.SUBMAP_SIZE - 1 ? 0 : 1);

            var overmapTileFileYRange = EnumExt.RangeCount(overmapTileFileYFrom, overmapTileFileYTo);

            foreach (var overmapTileFileX in overmapTileFileXRange)
                foreach (var overmapTileFileY in overmapTileFileYRange)
                    WriteOvermapTileFile(
                        mapTopLeftCoords,
                        segmentPath,
                        new Point(overmapTileFileX, overmapTileFileY)
                    );
        }

        private void WriteOvermapTileFile(
            CddaTileCoords mapTopLeftCoords,
            string segmentPath,
            Point overmapTileFile
        )
        {
            var overmapTileData = new List<object>();
            foreach (var submapIdxX in EnumExt.Range(2))
                foreach (var submapIdxY in EnumExt.Range(2))
                    overmapTileData.Add(GetSubmap(
                        mapTopLeftCoords,
                        overmapTileFile,
                        new Point(submapIdxX, submapIdxY)
                    ));

            var overmapTileFilename = $"{overmapTileFile.X}.{overmapTileFile.Y}.0{OVERMAP_TILE_FILE_EXT}";
            JsonIO.WriteJson(Path.Combine(segmentPath, overmapTileFilename), overmapTileData);
        }

        private object GetSubmap(
            CddaTileCoords mapTopLeftCoords,
            Point overmapTileFile,
            Point submapIdx
        )
        {
            // Examples:
            //   99.557.map['coordinates'] = [ [ 198, 1114 ], [ 198, 1115 ], [ 199, 1114 ], [ 199, 1115 ] ]
            //   36.546.map['coordinates'] = [ [ 72, 1092 ], [ 72, 1093 ], [ 73, 1092 ], [ 73, 1093 ] ]
            static int toSubmapCoord(int overmapTileFile, int submapIdx)
                => (overmapTileFile * 2) + submapIdx;

            var submapCoord = new int[]
            {
                toSubmapCoord(overmapTileFile.X, submapIdx.X),
                toSubmapCoord(overmapTileFile.Y, submapIdx.Y),
                0
            };

            var terrain = GetSubmapTerrain(
                mapTopLeftCoords,
                overmapTileFile,
                submapIdx
            );

            var submap = new
            {
                version = SAVE_VERSION,
                coordinates = submapCoord,
                turn_last_touched = 1,
                temperature = 0,
                terrain,
                radiation = new int[] { 0, 144 },
                furniture = Array.Empty<object>(),
                items = Array.Empty<object>(),
                traps = Array.Empty<object>(),
                fields = Array.Empty<object>(),
                cosmetics = Array.Empty<object>(),
                spawns = Array.Empty<object>(),
                vehicles = Array.Empty<object>(),
                partial_constructions = Array.Empty<object>()
            };

            return submap;
        }

        private object[] GetSubmapTerrain(
            CddaTileCoords mapTopLeftCoords,
            Point overmapTileFile,
            Point submapIdx
        )
        {
            var terrain = new List<string>();
            foreach (var submapTileX in EnumExt.Range(CddaMap.SUBMAP_SIZE))
                foreach (var submapTileY in EnumExt.Range(CddaMap.SUBMAP_SIZE))
                {
                    var tileAbsPos = GetAbsPos(
                        overmapTileFile,
                        submapIdx,
                        new Point(submapTileY, submapTileX) // reversed X <-> Y
                    );

                    var pixelPos = new Point(
                        tileAbsPos.X - mapTopLeftCoords.Abspos.X,
                        tileAbsPos.Y - mapTopLeftCoords.Abspos.Y
                    );

                    var tileType = TILE_PER_TERRAIN[MapGen.GetTerrain(pixelPos)];

                    terrain.Add(tileType);
                }

            return SimplifyTerrain(terrain);
        }

        private static object[] SimplifyTerrain(List<string> terrain)
        {
            var simplified = new List<object>();
            var tmpTileInfo = (
                tile: string.Empty,
                count: 0
            );
            foreach (var tile in terrain)
            {
                if (tile == tmpTileInfo.tile)
                    tmpTileInfo.count++;
                else
                {
                    ProcessTerrainTile(simplified, tmpTileInfo);
                    tmpTileInfo = (tile, 1);
                }
            }

            ProcessTerrainTile(simplified, tmpTileInfo);

            return simplified.ToArray();
        }

        private static void ProcessTerrainTile(
            List<object> simplified,
            (string tile, int count) tmpTileInfo
        )
        {
            if (tmpTileInfo.count == 1)
                simplified.Add(tmpTileInfo.tile);
            else if (tmpTileInfo.count > 1)
                simplified.Add(
                    new object[] { tmpTileInfo.tile, tmpTileInfo.count }
                );
        }

        private string GetSaveId()
        {
            var saveFilepath = Directory
                .GetFiles(SavePath, $"*{MAIN_SAVE_FILE_EXT}")[0];

            return Path.GetFileNameWithoutExtension(saveFilepath);
        }

        private void WriteMapSegmenFiles()
        {
            var segmentsFolder = Path.Combine(SavePath, CDDA_SAVE_SEGMENTS_FOLDER);

            if (Directory.Exists(segmentsFolder))
                Directory.Delete(segmentsFolder, recursive: true);

            Directory.CreateDirectory(segmentsFolder);
        }

        private void WriteSeen00()
        {
            var allFalseArray = new object[] { new object[] { false, 32400 } };
            var allFalseArrays = Enumerable
                .Repeat(allFalseArray, EMPTY_SEEN_DATA_COUNT);

            var emptyArray = Array.Empty<int>();
            var emptyArrays = Enumerable
                .Repeat(emptyArray, EMPTY_SEEN_DATA_COUNT);

            var emptySeen00 = new
            {
                visible = allFalseArrays,
                explored = allFalseArrays,
                notes = emptyArrays,
                extras = emptyArrays
            };

            JsonIO.WriteJson<dynamic>(
                Path.Combine(SavePath, $"{SaveId}{SEEN_0_0_FILE_EXT}"),
                emptySeen00,
                header: SAVE_VERSION_HEADER
            );
        }

        private void WriteOvermapFiles()
        {
            var emptyRockArray = new object[] { new object[] { "empty_rock", 32400 } };
            var fieldArray = new object[] { new object[] { "field", 32400 } };
            var openAirArray = new object[] { new object[] { "open_air", 32400 } };

            var layers = Enumerable.Repeat(emptyRockArray, 10)
                .Concat(new object[] { fieldArray })
                .Concat(Enumerable.Repeat(openAirArray, 10));

            var emptyArray = Array.Empty<int>();
            var emptyO00 = new
            {
                layers,
                region_id = "default",
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

            JsonIO.WriteJson<dynamic>(
                Path.Combine(SavePath, O_0_0_FILE),
                emptyO00,
                header: SAVE_VERSION_HEADER
            );
        }

        private void WriteMapMemory()
        {
            var emptyMapMemory = Enumerable
                .Repeat(Array.Empty<int>(), 2)
                .ToArray();

            JsonIO.WriteJson<dynamic>(
                Path.Combine(SavePath, $"{SaveId}{MAP_MEMORY_FILE_EXT}"),
                emptyMapMemory
            );
        }

        private void WriteMainSave(CddaPlayerCoords playerSpawnCoord)
        {
            var mainSaveFilepath = Path
                .Combine(SavePath, $"{SaveId}{MAIN_SAVE_FILE_EXT}");

            var mainSaveData = JsonIO.ReadJson(mainSaveFilepath, skipLines: 1);

            mainSaveData[ACTIVE_MONSTERS_KEY] = new JArray();
            mainSaveData[STAIR_MONSTERS_KEY] = new JArray();

            mainSaveData[OVERTILE_REGION_X_KEY] = playerSpawnCoord.OvermapRegion.X;
            mainSaveData[OVERTILE_REGION_Y_KEY] = playerSpawnCoord.OvermapRegion.Y;
            mainSaveData[LEVX_KEY] = playerSpawnCoord.SavegameLev.X;
            mainSaveData[LEVY_KEY] = playerSpawnCoord.SavegameLev.Y;
            mainSaveData[PLAYER_KEY][PLAYER_POSX_KEY] = playerSpawnCoord.SavegamePos.X;
            mainSaveData[PLAYER_KEY][PLAYER_POSY_KEY] = playerSpawnCoord.SavegamePos.Y;

            JsonIO.WriteJson<dynamic>(
                mainSaveFilepath,
                mainSaveData,
                header: SAVE_VERSION_HEADER
            );
        }

        private Point GetAbsPosFromRelMapPos(Point relpos)
            => new Point(
                relpos.X + MapTopLeftAbsPos.X,
                relpos.Y + MapTopLeftAbsPos.Y
            );

        private static Point GetAbsPos(
            Point overmapTileFile,
            Point submapIdx,
            Point relPosInSubmap
        ) => new Point(
            GetAbsPosComponent(overmapTileFile.X, submapIdx.X, relPosInSubmap.X),
            GetAbsPosComponent(overmapTileFile.Y, submapIdx.Y, relPosInSubmap.Y)
        );

        private static int GetAbsPosComponent(
            int overmapTileFile, int submapIdx, int relPosInSubmap = 0
        ) => (
            relPosInSubmap
            + (overmapTileFile * CddaMap.OVERMAP_TILE_SIZE)
            + (submapIdx * CddaMap.SUBMAP_SIZE)
        );

    }
}
