using CddaOsmMaps.Crosscutting;
using CddaOsmMaps.MapGen.Contracts;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace CddaOsmMaps.Cdda
{
    internal partial class CddaGenerator
    {
        private const string CDDA_SAVE_FOLDER = "save";
        private const string CDDA_SAVE_SEGMENTS_FOLDER = "maps";
        private const string MAIN_SAVE_FILE_EXT = ".sav";
        private const string OVERMAP_TILE_FILE_EXT = ".map";
        private const string MAP_MEMORY_FILE_EXT = ".mm";

        private const int SAVE_VERSION = 33;
        private readonly string SAVE_VERSION_HEADER = $"# version {SAVE_VERSION}\n";

        // main save file {saveId}.sav
        private const string OVERTILE_REGION_X_KEY = "om_x";
        private const string OVERTILE_REGION_Y_KEY = "om_y";
        private const string LEVX_KEY = "levx";
        private const string LEVY_KEY = "levy";
        private const string PLAYER_KEY = "player";
        private const string PLAYER_POSX_KEY = "posx";
        private const string PLAYER_POSY_KEY = "posy";
        private const string ACTIVE_MONSTERS_KEY = "active_monsters";
        private const string STAIR_MONSTERS_KEY = "stair_monsters";

        private const string OVERMAP_REGION_FILE_REGION_ID_VALUE = "default";

        private readonly bool Log;

        private readonly string SavePath;
        private readonly string SaveId;
        private readonly IMapGenerator MapGen;

        private Point MapTopLeftAbsPos;
        private CddaTileCoords MapTopLeftCoords;
        private CddaTileCoords MapBotRghtCoords;
        private CddaPlayerCoords PlayerSpawnCoords;
        private CddaTileCoords PlayerSpawnTileCoords;

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
            SetMapCornersData();
            WriteMapSegmenFiles();
            WriteSeenOvermapFiles();
            WriteOvermapFiles();
            WriteMapMemory();

            SetSpawnCoords(spawnAbsPos);
            WriteMainSave();
            WriteSegments();
        }

        private void SetMapCornersData()
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
                x: (int)(mapCenterInRegionUnits.X * CddaMap.OVERMAP_REGION_SIZE),
                y: (int)(mapCenterInRegionUnits.Y * CddaMap.OVERMAP_REGION_SIZE)
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

            MapTopLeftCoords = new CddaTileCoords(GetAbsPosFromRelMapPos(new Point(0, 0)));

            MapBotRghtCoords = new CddaTileCoords(GetAbsPosFromRelMapPos(
                new Point(MapGen.MapSize.Width - 1, MapGen.MapSize.Height - 1)
            ));
        }

        private void SetSpawnCoords(Point? spawnAbsPos)
        {
            if (!spawnAbsPos.HasValue)
                spawnAbsPos = new Point(
                    MapGen.MapSize.Width / 2,
                    MapGen.MapSize.Height / 2
                );

            var absPos = GetAbsPosFromRelMapPos(spawnAbsPos.Value);
            PlayerSpawnCoords = new CddaPlayerCoords(absPos);
            PlayerSpawnTileCoords = new CddaTileCoords(absPos);
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

        private void WriteMainSave()
        {
            var mainSaveFilepath = Path
                .Combine(SavePath, $"{SaveId}{MAIN_SAVE_FILE_EXT}");

            var mainSaveData = JsonIO.ReadJson(mainSaveFilepath, skipLines: 1);

            mainSaveData[ACTIVE_MONSTERS_KEY] = new JArray();
            mainSaveData[STAIR_MONSTERS_KEY] = new JArray();

            mainSaveData[OVERTILE_REGION_X_KEY] = PlayerSpawnCoords.OvermapRegion.X;
            mainSaveData[OVERTILE_REGION_Y_KEY] = PlayerSpawnCoords.OvermapRegion.Y;
            mainSaveData[LEVX_KEY] = PlayerSpawnCoords.SavegameLev.X;
            mainSaveData[LEVY_KEY] = PlayerSpawnCoords.SavegameLev.Y;
            mainSaveData[PLAYER_KEY][PLAYER_POSX_KEY] = PlayerSpawnCoords.SavegamePos.X;
            mainSaveData[PLAYER_KEY][PLAYER_POSY_KEY] = PlayerSpawnCoords.SavegamePos.Y;

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

        private static object[] SimplifyTiles(
            List<string> tiles,
            Action<List<object>, (string tile, int count)> processTerrainInTileFn
        )
        {
            var simplified = new List<object>();
            var tmpTileInfo = (
                tile: tiles[0],
                count: 1
            );
            foreach (var tile in tiles.Skip(1))
                if (tile == tmpTileInfo.tile)
                    tmpTileInfo.count++;
                else
                {
                    processTerrainInTileFn(simplified, tmpTileInfo);
                    tmpTileInfo = (tile, 1);
                }

            processTerrainInTileFn(simplified, tmpTileInfo);

            return simplified.ToArray();
        }

        private static void ProcessTile(
            List<object> simplified,
            (string tile, int count) tmpTileInfo,
            bool singleTileAsArray
        )
        {
            var isArrayValue = (singleTileAsArray || tmpTileInfo.count > 1);

            simplified.Add(
                isArrayValue
                    ? new object[] { tmpTileInfo.tile, tmpTileInfo.count }
                    : (object)tmpTileInfo.tile
            );
        }

        private void DeleteFiles(string wildcardPatter, Regex regexPattern)
        {
            var savePathDir = new DirectoryInfo(SavePath);
            savePathDir
                .EnumerateFiles(wildcardPatter)
                .Where(f => regexPattern.IsMatch(f.Name))
                .ToList()
                .ForEach(file => file.Delete());
        }
    }
}
