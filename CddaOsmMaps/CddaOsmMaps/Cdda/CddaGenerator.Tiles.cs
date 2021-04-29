using CddaOsmMaps.Crosscutting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace CddaOsmMaps.Cdda
{
    partial class CddaGenerator
    {
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
