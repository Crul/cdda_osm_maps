using CddaOsmMaps.Crosscutting;
using CddaOsmMaps.MapGen.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace CddaOsmMaps.Cdda
{
    partial class CddaGenerator
    {
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

        private void WriteSegments()
        {
            if (Log)
            {
                if (MapTopLeftCoords.RelPosInSubmap.X != 0
                    || MapTopLeftCoords.RelPosInSubmap.Y != 0
                    || MapTopLeftCoords.SubmapIdx.X != 0
                    || MapTopLeftCoords.SubmapIdx.Y != 0)
                    Console.WriteLine("Map Top Left corner not at (0,0) relative (to Overmap Tile) position");

                if (MapBotRghtCoords.RelPosInSubmap.X != CddaMap.SUBMAP_SIZE - 1
                    || MapBotRghtCoords.RelPosInSubmap.Y != CddaMap.SUBMAP_SIZE - 1
                    || MapBotRghtCoords.SubmapIdx.X != 1
                    || MapBotRghtCoords.SubmapIdx.Y != 1)
                    Console.WriteLine("Map Bottom Right corner not at (MAX,MAX) relative (to Overmap Tile) position");
            }

            var segmentFrom = new Point(
                MapTopLeftCoords.Segment.X,
                MapTopLeftCoords.Segment.Y
            );
            var segmentTo = new Point(
                MapBotRghtCoords.Segment.X,
                MapBotRghtCoords.Segment.Y
            );

            var segmentXRange = EnumExt.RangeFromTo(segmentFrom.X, segmentTo.X);
            var segmentYRange = EnumExt.RangeFromTo(segmentFrom.Y, segmentTo.Y);

            foreach (var segmentX in segmentXRange)
                foreach (var segmentY in segmentYRange)
                    WriteSegment(
                        segmentFrom,
                        segmentTo,
                        new Point(segmentX, segmentY)
                    );
        }

        private void WriteSegment(
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
                ? segment.X * CddaMap.SEGMENT_SIZE_IN_OVERMAP_TILES
                : MapTopLeftCoords.OvermapTile.X
                    + (MapTopLeftCoords.RelPosInSubmap.X == 0 ? 0 : 1);

            var overmapTileFileXTo = (segment.X < segmentTo.X)
                ? (segment.X + 1) * CddaMap.SEGMENT_SIZE_IN_OVERMAP_TILES
                : MapBotRghtCoords.OvermapTile.X
                    - (MapTopLeftCoords.RelPosInSubmap.X == CddaMap.SUBMAP_SIZE - 1 ? 0 : 1);

            var overmapTileFileXRange = EnumExt.RangeFromTo(overmapTileFileXFrom, overmapTileFileXTo);

            var overmapTileFileYFrom = (segment.Y > segmentFrom.Y)
                ? segment.Y * CddaMap.SEGMENT_SIZE_IN_OVERMAP_TILES
                : MapTopLeftCoords.OvermapTile.Y
                    + (MapTopLeftCoords.RelPosInSubmap.Y == 0 ? 0 : 1);

            var overmapTileFileYTo = (segment.Y < segmentTo.Y)
                ? (segment.Y + 1) * CddaMap.SEGMENT_SIZE_IN_OVERMAP_TILES
                : MapBotRghtCoords.OvermapTile.Y
                    - (MapTopLeftCoords.RelPosInSubmap.Y == CddaMap.SUBMAP_SIZE - 1 ? 0 : 1);

            var overmapTileFileYRange = EnumExt.RangeFromTo(overmapTileFileYFrom, overmapTileFileYTo);

            foreach (var overmapTileFileX in overmapTileFileXRange)
                foreach (var overmapTileFileY in overmapTileFileYRange)
                    WriteOvermapTileFile(
                        segmentPath,
                        new Point(overmapTileFileX, overmapTileFileY)
                    );
        }

        private void WriteOvermapTileFile(
            string segmentPath,
            Point overmapTileFile
        )
        {
            var overmapTileData = new List<object>();
            foreach (var submapIdxX in EnumExt.Range(2))
                foreach (var submapIdxY in EnumExt.Range(2))
                    overmapTileData.Add(GetSubmap(
                        overmapTileFile,
                        new Point(submapIdxX, submapIdxY)
                    ));

            var overmapTileFilename = $"{overmapTileFile.X}.{overmapTileFile.Y}.0{OVERMAP_TILE_FILE_EXT}";
            JsonIO.WriteJson(Path.Combine(segmentPath, overmapTileFilename), overmapTileData);
        }

        private object GetSubmap(Point overmapTileFile, Point submapIdx)
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

            var terrain = GetSubmapTerrain(overmapTileFile, submapIdx);

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

        private object[] GetSubmapTerrain(Point overmapTileFile, Point submapIdx)
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
                        tileAbsPos.X - MapTopLeftCoords.Abspos.X,
                        tileAbsPos.Y - MapTopLeftCoords.Abspos.Y
                    );

                    var tileType = TILE_PER_TERRAIN[MapGen.GetTerrain(pixelPos)];

                    terrain.Add(tileType);
                }

            return SimplifyTerrain(terrain);
        }

        private static object[] SimplifyTerrain(List<string> terrain)
            => SimplifyTiles(terrain, ProcessTerrainTile);

        private static void ProcessTerrainTile(
            List<object> simplified,
            (string tile, int count) tmpTileInfo
        ) => ProcessTile(
            simplified,
            tmpTileInfo,
            singleTileAsArray: false
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
