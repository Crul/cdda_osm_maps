using CddaOsmMaps.Crosscutting;
using CddaOsmMaps.MapGen.Contracts;
using CddaOsmMaps.MapGen.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CddaOsmMaps.Cdda
{
    internal class CddaGenerator
    {
        private const string CDDA_SAVE_FOLDER = "save";
        private const string CDDA_SAVE_SEGMENTS_FOLDER = "maps";
        private const string MAIN_SAVE_FILE_EXT = ".sav";
        private const string SUBMAP_4X_FILE_EXT = ".map";
        private const int SAVE_VERSION = 33;
        private readonly string SAVE_VERSION_HEADER = $"# version {SAVE_VERSION}\n";

        private const string SEEN_0_0_FILE_EXT = ".seen.0.0";
        private const int EMPTY_SEEN_DATA_COUNT = 21;

        private const string MAP_MEMORY_FILE_EXT = ".mm";
        private const string O_0_0_FILE = "o.0.0";

        private const string LEVX_KEY = "levx";
        private const string LEVY_KEY = "levy";
        private const string PLAYER_KEY = "player";
        private const string PLAYER_POSX_KEY = "posx";
        private const string PLAYER_POSY_KEY = "posy";
        private const string ACTIVE_MONSTERS_KEY = "active_monsters";
        private const string STAIR_MONSTERS_KEY = "stair_monsters";

        private const int SUBMAP_SIZE = 12;
        private const int SUBMAP_x4_SIZE = SUBMAP_SIZE * 2;
        private const int SUBMAP_x4_PER_SEGMENT = 32;
        private const int SEGMENT_SIZE = SUBMAP_x4_PER_SEGMENT * SUBMAP_x4_SIZE;

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

        public CddaGenerator(IMapGenerator mapGen, string cddaFolder, string saveGame)
        {
            MapGen = mapGen;
            SavePath = Path.Combine(cddaFolder, CDDA_SAVE_FOLDER, saveGame);
            SaveId = GetSaveId();
        }

        public void Generate(PointFloat spawnPoint)
        {
            CleanMapSegmenFiles();
            CleanSeen00();
            CleanO00();
            CleanMapMemory();
            var playerAbsPos = CleanMainSaveAndGetPlayerAbsPos();
            GenerateSegments(playerAbsPos, spawnPoint);
        }

        private void GenerateSegments((int x, int y) playerAbsPos, PointFloat spawnPoint)
        {
            var absSpawnPoint = (
                x: (int)(spawnPoint?.X ?? (MapGen.MapSize.width / 2)),
                y: (int)(spawnPoint?.Y ?? (MapGen.MapSize.height / 2))
            );
            var mapTopLeftAbspos = (
                x: playerAbsPos.x - absSpawnPoint.x,
                y: playerAbsPos.y - absSpawnPoint.y
            );

            static int toMapBotRghtAbspos(int mapTopLeftAbspos, int imgSize)
                => mapTopLeftAbspos + imgSize;

            var mapBotRghtAbspos = (
                x: toMapBotRghtAbspos(mapTopLeftAbspos.x, MapGen.MapSize.width),
                y: toMapBotRghtAbspos(mapTopLeftAbspos.y, MapGen.MapSize.height)
            );

            var mapTopLeftCoords = GetCoords(mapTopLeftAbspos);
            var mapBotRghtCoords = GetCoords(mapBotRghtAbspos);

            var segmentXFrom = mapTopLeftCoords.Segment.X;
            var segmentXTo = mapBotRghtCoords.Segment.X;
            var segmentYFrom = mapTopLeftCoords.Segment.Y;
            var segmentYTo = mapBotRghtCoords.Segment.Y;

            var segmentXRange = EnumExt.RangeCount(segmentXFrom, segmentXTo);
            var segmentYRange = EnumExt.RangeCount(segmentYFrom, segmentYTo);

            foreach (var segmentX in segmentXRange)
                foreach (var segmentY in segmentYRange)
                    GenerateSegment(
                        mapTopLeftAbspos,
                        mapTopLeftCoords,
                        mapBotRghtCoords,
                        segmentXFrom,
                        segmentXTo,
                        segmentYFrom,
                        segmentYTo,
                        segmentX,
                        segmentY
                    );
        }

        private void GenerateSegment(
            (int x, int y) mapTopLeftAbspos,
            CddaCoords mapTopLeftCoords,
            CddaCoords mapBotRghtCoords,
            int segmentXFrom,
            int segmentXTo,
            int segmentYFrom,
            int segmentYTo,
            int segmentX,
            int segmentY
        )
        {
            var segmentPath = Path.Combine(
                SavePath,
                CDDA_SAVE_SEGMENTS_FOLDER,
                $"{segmentX}.{segmentY}.0"
            );
            Directory.CreateDirectory(segmentPath);

            var submap4xFileXFrom = (segmentX > segmentXFrom)
                ? segmentX * SUBMAP_x4_PER_SEGMENT
                : mapTopLeftCoords.Submap4xFile.X
                    + (mapTopLeftCoords.SubmapRelPos.X == 0 ? 0 : 1);

            var submap4xFileXTo = (segmentX < segmentXTo)
                ? (segmentX + 1) * SUBMAP_x4_PER_SEGMENT
                : mapBotRghtCoords.Submap4xFile.X
                    - (mapTopLeftCoords.SubmapRelPos.X == SUBMAP_SIZE - 1 ? 0 : 1);

            var submap4xFileXRange = EnumExt.RangeCount(submap4xFileXFrom, submap4xFileXTo);

            var submap4xFileYFrom = (segmentY > segmentYFrom)
                ? segmentY * SUBMAP_x4_PER_SEGMENT
                : mapTopLeftCoords.Submap4xFile.Y
                    + (mapTopLeftCoords.SubmapRelPos.Y == 0 ? 0 : 1);

            var submap4xFileYTo = (segmentY < segmentYTo)
                ? (segmentY + 1) * SUBMAP_x4_PER_SEGMENT
                : mapBotRghtCoords.Submap4xFile.Y
                    - (mapTopLeftCoords.SubmapRelPos.Y == SUBMAP_SIZE - 1 ? 0 : 1);

            var submap4xFileYRange = EnumExt.RangeCount(submap4xFileYFrom, submap4xFileYTo);

            foreach (var submap4xFileX in submap4xFileXRange)
                foreach (var submap4xFileY in submap4xFileYRange)
                    GenerateSubmap4xFile(
                        mapTopLeftAbspos,
                        segmentPath,
                        submap4xFileX,
                        submap4xFileY
                    );
        }

        private void GenerateSubmap4xFile(
            (int x, int y) mapTopLeftAbspos,
            string segmentPath,
            int submap4xFileX,
            int submap4xFileY
        )
        {
            var submap4XData = new List<object>();
            foreach (var submapIdxX in EnumExt.Range(2))
                foreach (var submapIdxY in EnumExt.Range(2))
                    submap4XData.Add(GetSubmap(
                        mapTopLeftAbspos,
                        submap4xFileX,
                        submap4xFileY,
                        submapIdxX,
                        submapIdxY
                    ));

            var submap4XFilename = $"{submap4xFileX}.{submap4xFileY}.0{SUBMAP_4X_FILE_EXT}";
            JsonIO.WriteJson(Path.Combine(segmentPath, submap4XFilename), submap4XData);
        }

        private object GetSubmap(
            (int x, int y) mapTopLeftAbspos,
            int submap4xFileX,
            int submap4xFileY,
            int submapIdxX,
            int submapIdxY)
        {
            // Examples:
            //   99.557.map['coordinates'] = [ [ 198, 1114 ], [ 198, 1115 ], [ 199, 1114 ], [ 199, 1115 ] ]
            //   36.546.map['coordinates'] = [ [ 72, 1092 ], [ 72, 1093 ], [ 73, 1092 ], [ 73, 1093 ] ]
            static int toSubmapCoord(int submap4xFile, int submapIdx)
                => (submap4xFile * 2) + submapIdx;

            var submapCoord = new int[]
            {
                toSubmapCoord(submap4xFileX, submapIdxX),
                toSubmapCoord(submap4xFileY, submapIdxY),
                0
            };

            var terrain = GetSubmapTerrain(
                mapTopLeftAbspos,
                submap4xFileX,
                submap4xFileY,
                submapIdxX,
                submapIdxY
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
            (int x, int y) mapTopLeftAbspos,
            int submap4xFileX,
            int submap4xFileY,
            int submapIdxX,
            int submapIdxY
        )
        {
            var terrain = new List<string>();
            foreach (var submapTileX in EnumExt.Range(SUBMAP_SIZE))
                foreach (var submapTileY in EnumExt.Range(SUBMAP_SIZE))
                {
                    var tileAbsPos = GetAbsPos(
                        (submap4xFileX, submap4xFileY),
                        (submapIdxX, submapIdxY),
                        (submapTileY, submapTileX) // reversed X <-> Y
                    );

                    var pixelPos = (
                        x: tileAbsPos.x - mapTopLeftAbspos.x,
                        y: tileAbsPos.y - mapTopLeftAbspos.y
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

        private void CleanMapSegmenFiles()
        {
            var segmentsFolder = Path.Combine(SavePath, CDDA_SAVE_SEGMENTS_FOLDER);

            if (Directory.Exists(segmentsFolder))
                Directory.Delete(segmentsFolder, recursive: true);

            Directory.CreateDirectory(segmentsFolder);
        }

        private void CleanSeen00()
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

        private void CleanO00()
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

        private void CleanMapMemory()
        {
            var emptyMapMemory = Enumerable
                .Repeat(Array.Empty<int>(), 2)
                .ToArray();

            JsonIO.WriteJson<dynamic>(
                Path.Combine(SavePath, $"{SaveId}{MAP_MEMORY_FILE_EXT}"),
                emptyMapMemory
            );
        }

        private (int x, int y) CleanMainSaveAndGetPlayerAbsPos()
        {
            var mainSaveFilepath = Path
                .Combine(SavePath, $"{SaveId}{MAIN_SAVE_FILE_EXT}");

            var mainSaveData = JsonIO
                .ReadJson<Dictionary<string, object>>(mainSaveFilepath, skipLines: 1);

            mainSaveData[ACTIVE_MONSTERS_KEY] = Array.Empty<int>();
            mainSaveData[STAIR_MONSTERS_KEY] = Array.Empty<int>();

            JsonIO.WriteJson<dynamic>(
                mainSaveFilepath,
                mainSaveData,
                header: SAVE_VERSION_HEADER
            );

            var levX = ((JsonElement)mainSaveData[LEVX_KEY]).GetInt32();
            var levY = ((JsonElement)mainSaveData[LEVY_KEY]).GetInt32();

            var playerData = (JsonElement)mainSaveData[PLAYER_KEY];
            var playerPosX = playerData.GetProperty(PLAYER_POSX_KEY).GetInt32();
            var playerPosY = playerData.GetProperty(PLAYER_POSY_KEY).GetInt32();

            static int toPlayerAbspos(int lev, int playerPos)
                => (lev * SUBMAP_SIZE) + playerPos;

            var playerAbsPos = (
                x: toPlayerAbspos(levX, playerPosX),
                y: toPlayerAbspos(levY, playerPosY)
            );

            return playerAbsPos;
        }

        private static CddaCoords GetCoords((int x, int y) abspos)
        {
            // SEGMENT
            // floor(abspos / 768) = 4, 0
            static int toSegment(int abspos)
                => (int)Math.Floor((double)abspos / SEGMENT_SIZE);

            var segment = (
                x: toSegment(abspos.x),
                y: toSegment(abspos.y)
            );

            // IN SEGMENT RELATIVE POSITION relposInSegment
            // abspos % 768 = 6, 583
            static int toRelposInSegment(int abspos)
                => MathExt.MathMod(abspos, SEGMENT_SIZE);
            var relposInSegment = (
                x: toRelposInSegment(abspos.x),
                y: toRelposInSegment(abspos.y)
            );

            // IN SEGMENT RELATIVE 4x SUBMAP relSubmap4xFile
            // floor(relposInSegment / 24) = 0, 24
            static int toRelSubmap4xFile(int relposInSegment)
                => (int)Math.Floor((double)relposInSegment / SUBMAP_x4_SIZE);

            var relSubmap4xFile = (
                x: toRelSubmap4xFile(relposInSegment.x),
                y: toRelSubmap4xFile(relposInSegment.y)
            );

            // ABSOLUTE 4x SUBMAP FILE
            // rel_submap_4x_file + segment * 32  = 128, 24
            static int toSubmap4xFile(int relSubmap4xFile, int segment)
                => (int)Math.Floor((double)relSubmap4xFile + (segment * SUBMAP_x4_PER_SEGMENT));

            var submap4xFile = (
                x: toSubmap4xFile(relSubmap4xFile.x, segment.x),
                y: toSubmap4xFile(relSubmap4xFile.y, segment.y)
            );

            // SUBMAP INDEX IN 4xSUBMAP FILE
            // floor(relposInSegment / 12) - (2 x relSubmap4xFile)
            //                             = (0, 48) - 2x(0, 24)
            //                             =  0,  0
            static int toSubmapIdxIn4xFile(int relposInSegment, int relSubmap4xFile)
                => (int)Math.Floor((double)relposInSegment / SUBMAP_SIZE) - (2 * relSubmap4xFile);

            var submapIdxIn4xFile = (
                x: toSubmapIdxIn4xFile(relposInSegment.x, relSubmap4xFile.x),
                y: toSubmapIdxIn4xFile(relposInSegment.y, relSubmap4xFile.y)
            );

            // IN SUBMAP RELATIVE POSITION
            // abspos % 12               = 6, 7
            static int toSubmapRelpos(int abspos)
                => MathExt.MathMod(abspos, SUBMAP_SIZE);
            var submapRelpos = (
                x: toSubmapRelpos(abspos.x),
                u: toSubmapRelpos(abspos.y)
            );

            return new CddaCoords
            {
                Segment = new Point3D(segment),
                Submap4xFile = new Point3D(submap4xFile),
                SubmapIdx = new Point3D(submapIdxIn4xFile),
                SubmapRelPos = new Point3D(submapRelpos)
            };
        }

        private static (int x, int y) GetAbsPos(
            (int x, int y) submap4xFile,
            (int x, int y) submapIdx,
            (int x, int y) submapRelpos
        ) => (
            GetAbsPosComponent(submap4xFile.x, submapIdx.x, submapRelpos.x),
            GetAbsPosComponent(submap4xFile.y, submapIdx.y, submapRelpos.y)
        );

        private static int GetAbsPosComponent(
            int submap4xFile, int submapIdx, int submapRelpos = 0
        ) => (
            submapRelpos
            + (submap4xFile * SUBMAP_x4_SIZE)
            + (submapIdx * SUBMAP_SIZE)
        );

    }
}
