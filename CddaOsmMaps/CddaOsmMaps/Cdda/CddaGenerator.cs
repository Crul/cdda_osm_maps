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
    internal partial class CddaGenerator
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
    }
}
