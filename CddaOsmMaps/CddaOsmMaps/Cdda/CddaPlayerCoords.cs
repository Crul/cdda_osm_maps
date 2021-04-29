using CddaOsmMaps.Crosscutting;
using System;

namespace CddaOsmMaps.Cdda
{
    public class CddaPlayerCoords
    {
        public (int x, int y) Abspos { get; private set; }
        public Point3D OvermapRegion { get; private set; }
        public Point3D SavegameLev { get; private set; }
        public Point3D SavegamePos { get; private set; }

        public CddaPlayerCoords((int x, int y) abspos)
        {
            // https://discourse.cataclysmdda.org/t/maps-coordinate-systems-and-files/24935/7
            // https://discourse.cataclysmdda.org/t/save-corruption-proble/23688/3
            // https://discourse.cataclysmdda.org/t/player-position-data-on-save-game-how-does-it-work/

            Abspos = abspos;

            var playerTileMinusRealityBubbleCoords = new CddaTileCoords((
                abspos.x - CddaMap.REALITY_BUBBLE_RADIUS,
                abspos.y - CddaMap.REALITY_BUBBLE_RADIUS
            ));

            var realityBubbleTopLefCoords = new CddaTileCoords((
                abspos.x - CddaMap.REALITY_BUBBLE_RADIUS
                         - playerTileMinusRealityBubbleCoords.RelPosInSubmap.X,
                abspos.y - CddaMap.REALITY_BUBBLE_RADIUS
                         - playerTileMinusRealityBubbleCoords.RelPosInSubmap.Y
            ));

            OvermapRegion = realityBubbleTopLefCoords.OvermapRegion;

            static int toSavegameLev(
                int realityBubbleTopLefOvermapTile,
                int realityBubbleTopLefSubmapIdx
            ) => MathExt.MathMod(
                realityBubbleTopLefOvermapTile * 2,
                CddaMap.SUBMAP_TILES_PER_REGION
            ) + realityBubbleTopLefSubmapIdx;

            SavegameLev = new Point3D((
                toSavegameLev(
                    realityBubbleTopLefCoords.OvermapTile.X,
                    realityBubbleTopLefCoords.SubmapIdx.X
                ),
                toSavegameLev(
                    realityBubbleTopLefCoords.OvermapTile.Y,
                    realityBubbleTopLefCoords.SubmapIdx.Y
                )
            ));

            SavegamePos = new Point3D((
                CddaMap.REALITY_BUBBLE_RADIUS
                    + playerTileMinusRealityBubbleCoords.RelPosInSubmap.X,
                CddaMap.REALITY_BUBBLE_RADIUS
                    + playerTileMinusRealityBubbleCoords.RelPosInSubmap.Y
            ));
        }

        public CddaPlayerCoords( // for testing purposes
            (int x, int y) abspos,
            (int x, int y) overmapRegion,
            (int x, int y) savegameLev,
            (int x, int y) savegamePos
        )
        {
            Abspos = abspos;
            OvermapRegion = new Point3D(overmapRegion);
            SavegameLev = new Point3D(savegameLev);
            SavegamePos = new Point3D(savegamePos);
        }

        public static bool operator ==(CddaPlayerCoords left, CddaPlayerCoords right)
            => left.Equals(right);

        public static bool operator !=(CddaPlayerCoords left, CddaPlayerCoords right)
            => !(left == right);

        public override bool Equals(object obj)
            => obj is CddaPlayerCoords
                ? Equals((CddaPlayerCoords)obj)
                : false;

        public override int GetHashCode()
            => HashCode.Combine(
                Abspos,
                OvermapRegion.X,
                OvermapRegion.Y,
                SavegameLev.X,
                SavegameLev.Y,
                SavegamePos.X,
                SavegamePos.Y
            );

        private bool Equals(CddaPlayerCoords obj)
            => obj.Abspos == Abspos
            && obj.OvermapRegion == OvermapRegion
            && obj.SavegameLev == SavegameLev
            && obj.SavegamePos == SavegamePos;
    }
}
