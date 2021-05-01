using CddaOsmMaps.MapGen.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CddaOsmMaps.Cdda
{
    partial class CddaGenerator
    {
        private void SpawnMonster(
            List<object> monsters,
            TerrainType terrainType,
            Point overmapTileFile,
            Point submapIdx,
            int relPosInSubmapX,
            int relPosInSubmapY
        )
        {
            var isMonsterAllowed = MONSTER_ALLOWED_TERRAIN_TYPES.Contains(terrainType);
            if (!isMonsterAllowed)
                return;

            var isPlayerSpawnTile = (
                // TODO spawn monster in player tile check should take OvermapRegion into account (minor issue)
                PlayerSpawnTileCoords.RelPosInSubmap.X == relPosInSubmapX
                && PlayerSpawnTileCoords.RelPosInSubmap.Y == relPosInSubmapY
                && PlayerSpawnTileCoords.OvermapTile.X == overmapTileFile.X
                && PlayerSpawnTileCoords.OvermapTile.Y == overmapTileFile.Y
                && PlayerSpawnTileCoords.SubmapIdx.X == submapIdx.X
                && PlayerSpawnTileCoords.SubmapIdx.Y == submapIdx.Y
            );

            if (isPlayerSpawnTile)
                return;

            if (monsters.Count == 0)
                monsters.Add(
                    new List<object>()
                    {
                        "mon_zombie_brainless",
                        1, // count
                        relPosInSubmapX, // posx
                        relPosInSubmapY, // posy
                        -1, // faction_id
                        -1, // mission_id
                        false, // friendly
                        "NONE", // name
                    }
                );
        }
    }
}
