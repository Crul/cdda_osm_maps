using CddaOsmMaps.Crosscutting;
using CddaOsmMaps.MapGen.Contracts;
using CddaOsmMaps.MapGen.Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        private (int x, int y) MapTopLeftAbsPos;

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

        public void Generate(PointFloat spawnAbsPos)
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
                width: toMapSizeInOvermapTileUnits(MapGen.MapSize.width),
                height: toMapSizeInOvermapTileUnits(MapGen.MapSize.height)
            );

            if (Log
                && (mapSizeInOvermapTileUnits.width != (int)mapSizeInOvermapTileUnits.width
                || mapSizeInOvermapTileUnits.height != (int)mapSizeInOvermapTileUnits.height)
            )
                Console.WriteLine($"Map size not multiple of OvermapTile size: {MapGen.MapSize}");


            static float toMapSizeInWholeRegionUnits(int mapSize)
                => (float)Math.Ceiling((float)mapSize / CddaMap.OVERMAP_REGION_SIZE);

            var mapSizeInWholeRegionUnits = (
                width: toMapSizeInWholeRegionUnits(MapGen.MapSize.width),
                height: toMapSizeInWholeRegionUnits(MapGen.MapSize.height)
            );

            var mapCenterInRegionUnits = (
                x: mapSizeInWholeRegionUnits.width / 2,
                y: mapSizeInWholeRegionUnits.height / 2
            );

            var mapCenterAbsPos = (
                x: (int)mapCenterInRegionUnits.x * CddaMap.OVERMAP_REGION_SIZE,
                y: (int)mapCenterInRegionUnits.y * CddaMap.OVERMAP_REGION_SIZE
            );

            int mapTopLeftAbsPosX;
            int mapTopLeftAbsPosY;

            if (mapSizeInOvermapTileUnits.width % 2 == 0)
                mapTopLeftAbsPosX = mapCenterAbsPos.x - (MapGen.MapSize.width / 2);

            else
                mapTopLeftAbsPosX = mapCenterAbsPos.x - (CddaMap.OVERMAP_TILE_SIZE * ((int)mapSizeInOvermapTileUnits.width - 1) / 2);

            if (mapSizeInOvermapTileUnits.height % 2 == 0)
                mapTopLeftAbsPosY = mapCenterAbsPos.y - (MapGen.MapSize.height / 2);

            else
                mapTopLeftAbsPosY = mapCenterAbsPos.y - (CddaMap.OVERMAP_TILE_SIZE * ((int)mapSizeInOvermapTileUnits.height - 1) / 2);

            MapTopLeftAbsPos = (mapTopLeftAbsPosX, mapTopLeftAbsPosY);
        }

        private CddaPlayerCoords GetSpawnCoords(PointFloat spawnAbsPos)
        {
            if (spawnAbsPos == null)
                return new CddaPlayerCoords(
                    GetAbsPosFromRelMapPos((
                        CddaMap.OVERMAP_REGION_SIZE / 2,
                        CddaMap.OVERMAP_REGION_SIZE / 2
                    ))
                );

            return new CddaPlayerCoords(
                GetAbsPosFromRelMapPos((spawnAbsPos.X, spawnAbsPos.Y))
            );
        }

        /*
private (CddaCoords topLeft, CddaCoords botRght) GetMapCddaCoords(
   (int x, int y) playerAbsPos, PointFloat spawnPoint
)
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

   return (mapTopLeftCoords, mapBotRghtCoords);
}
*/
        private void WriteSegments()
        {
            var mapTopLeftCoords = new CddaTileCoords(GetAbsPosFromRelMapPos((0, 0)));
            var mapBotRghtCoords = new CddaTileCoords(GetAbsPosFromRelMapPos(
                (MapGen.MapSize.width - 1, MapGen.MapSize.height - 1)
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

            var segmentXFrom = mapTopLeftCoords.Segment.X;
            var segmentXTo = mapBotRghtCoords.Segment.X;
            var segmentYFrom = mapTopLeftCoords.Segment.Y;
            var segmentYTo = mapBotRghtCoords.Segment.Y;

            var segmentXRange = EnumExt.RangeCount(segmentXFrom, segmentXTo);
            var segmentYRange = EnumExt.RangeCount(segmentYFrom, segmentYTo);

            foreach (var segmentX in segmentXRange)
                foreach (var segmentY in segmentYRange)
                    WriteSegment(
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

        private void WriteSegment(
            CddaTileCoords mapTopLeftCoords,
            CddaTileCoords mapBotRghtCoords,
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

            var overmapTileFileXFrom = (segmentX > segmentXFrom)
                ? segmentX * CddaMap.OVERMAP_TILES_PER_SEGMENT
                : mapTopLeftCoords.OvermapTile.X
                    + (mapTopLeftCoords.RelPosInSubmap.X == 0 ? 0 : 1);

            var overmapTileFileXTo = (segmentX < segmentXTo)
                ? (segmentX + 1) * CddaMap.OVERMAP_TILES_PER_SEGMENT
                : mapBotRghtCoords.OvermapTile.X
                    - (mapTopLeftCoords.RelPosInSubmap.X == CddaMap.SUBMAP_SIZE - 1 ? 0 : 1);

            var overmapTileFileXRange = EnumExt.RangeCount(overmapTileFileXFrom, overmapTileFileXTo);

            var overmapTileFileYFrom = (segmentY > segmentYFrom)
                ? segmentY * CddaMap.OVERMAP_TILES_PER_SEGMENT
                : mapTopLeftCoords.OvermapTile.Y
                    + (mapTopLeftCoords.RelPosInSubmap.Y == 0 ? 0 : 1);

            var overmapTileFileYTo = (segmentY < segmentYTo)
                ? (segmentY + 1) * CddaMap.OVERMAP_TILES_PER_SEGMENT
                : mapBotRghtCoords.OvermapTile.Y
                    - (mapTopLeftCoords.RelPosInSubmap.Y == CddaMap.SUBMAP_SIZE - 1 ? 0 : 1);

            var overmapTileFileYRange = EnumExt.RangeCount(overmapTileFileYFrom, overmapTileFileYTo);

            foreach (var overmapTileFileX in overmapTileFileXRange)
                foreach (var overmapTileFileY in overmapTileFileYRange)
                    WriteOvermapTileFile(
                        mapTopLeftCoords,
                        segmentPath,
                        overmapTileFileX,
                        overmapTileFileY
                    );
        }

        private void WriteOvermapTileFile(
            CddaTileCoords mapTopLeftCoords,
            string segmentPath,
            int overmapTileFileX,
            int overmapTileFileY
        )
        {
            var overmapTileData = new List<object>();
            foreach (var submapIdxX in EnumExt.Range(2))
                foreach (var submapIdxY in EnumExt.Range(2))
                    overmapTileData.Add(GetSubmap(
                        mapTopLeftCoords,
                        overmapTileFileX,
                        overmapTileFileY,
                        submapIdxX,
                        submapIdxY
                    ));

            var overmapTileFilename = $"{overmapTileFileX}.{overmapTileFileY}.0{OVERMAP_TILE_FILE_EXT}";
            JsonIO.WriteJson(Path.Combine(segmentPath, overmapTileFilename), overmapTileData);
        }

        private object GetSubmap(
            CddaTileCoords mapTopLeftCoords,
            int overmapTileFileX,
            int overmapTileFileY,
            int submapIdxX,
            int submapIdxY)
        {
            // Examples:
            //   99.557.map['coordinates'] = [ [ 198, 1114 ], [ 198, 1115 ], [ 199, 1114 ], [ 199, 1115 ] ]
            //   36.546.map['coordinates'] = [ [ 72, 1092 ], [ 72, 1093 ], [ 73, 1092 ], [ 73, 1093 ] ]
            static int toSubmapCoord(int overmapTileFile, int submapIdx)
                => (overmapTileFile * 2) + submapIdx;

            var submapCoord = new int[]
            {
                toSubmapCoord(overmapTileFileX, submapIdxX),
                toSubmapCoord(overmapTileFileY, submapIdxY),
                0
            };

            var terrain = GetSubmapTerrain(
                mapTopLeftCoords,
                overmapTileFileX,
                overmapTileFileY,
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
            CddaTileCoords mapTopLeftCoords,
            int overmapTileFileX,
            int overmapTileFileY,
            int submapIdxX,
            int submapIdxY
        )
        {
            var terrain = new List<string>();
            foreach (var submapTileX in EnumExt.Range(CddaMap.SUBMAP_SIZE))
                foreach (var submapTileY in EnumExt.Range(CddaMap.SUBMAP_SIZE))
                {
                    var tileAbsPos = GetAbsPos(
                        (overmapTileFileX, overmapTileFileY),
                        (submapIdxX, submapIdxY),
                        (submapTileY, submapTileX) // reversed X <-> Y
                    );

                    var pixelPos = (
                        x: tileAbsPos.x - mapTopLeftCoords.Abspos.x,
                        y: tileAbsPos.y - mapTopLeftCoords.Abspos.y
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

        private (int x, int y) GetAbsPosFromRelMapPos((float x, float y) relpos)
            => (
                (int)relpos.x + MapTopLeftAbsPos.x,
                (int)relpos.y + MapTopLeftAbsPos.y
            );

        private static (int x, int y) GetAbsPos(
            (int x, int y) overmapTileFile,
            (int x, int y) submapIdx,
            (int x, int y) relPosInSubmap
        ) => (
            GetAbsPosComponent(overmapTileFile.x, submapIdx.x, relPosInSubmap.x),
            GetAbsPosComponent(overmapTileFile.y, submapIdx.y, relPosInSubmap.y)
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
