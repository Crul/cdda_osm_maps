using CddaOsmMaps.Crosscutting;
using CddaOsmMaps.MapGen.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CddaOsmMaps.Cdda
{
    partial class CddaGenerator
    {
        // TODO set spawn rate via command line arg
        private const float GLOBAL_MONSTER_SPAWN_RATE = 0.004f; // 0.4%
        private static readonly List<(string monster, double freqWeight)> MONSTER_SPAWN_CONFIG =
            new List<(string, double)>
            {
                ( "mon_zombie",              30d ),
                ( "mon_zombie_child",        10d ),
                ( "mon_zombie_tough",         5d ),
                ( "mon_zombie_fat",           5d ),
                ( "mon_zombie_rot",           5d ),
                ( "mon_zombie_runner",       10d ),
                ( "mon_feral_human_pipe",     2d ),
                ( "mon_feral_human_crowbar",  2d ),
                ( "mon_feral_human_axe",      2d ),
                ( "mon_zombie_crawler",       5d ),
                ( "mon_zombie_brainless",     5d ),
                ( "mon_zombie_dog",           5d ),
            };

        private static readonly double TOTAL_MONSTER_SPAWN_FREQ_WEIGHT =
            MONSTER_SPAWN_CONFIG.Sum(m => m.freqWeight);

        private static readonly List<(string monster, double threshold)> MONSTER_SPAWN_PROBABILTY =
            MONSTER_SPAWN_CONFIG
                .Select(m => (
                    m.monster,
                    GLOBAL_MONSTER_SPAWN_RATE * m.freqWeight / TOTAL_MONSTER_SPAWN_FREQ_WEIGHT
                ))
                .ToList();

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

            var monster = GetRandomMonster();
            if (!string.IsNullOrEmpty(monster))
                monsters.Add(
                    new List<object>()
                    {
                        monster,
                        1,               // count
                        relPosInSubmapX, // posx
                        relPosInSubmapY, // posy
                        -1,              // faction_id
                        -1,              // mission_id
                        false,           // friendly
                        "NONE"           // name
                    }
                );
        }

        private static string GetRandomMonster()
        {
            var randValue = RandomSingleton.Instance.Rnd.NextDouble();
            var acc = 0d;
            for (var i = 0; i < MONSTER_SPAWN_PROBABILTY.Count; i++)
            {
                var (monster, threshold) = MONSTER_SPAWN_PROBABILTY[i];
                acc += threshold; // TODO this can be done on initialization

                if (acc > randValue)
                    return monster;
            }

            return string.Empty;
        }
    }
}
