using CddaOsmMaps.Crosscutting;
using System;
using System.Drawing;

namespace CddaOsmMaps.Cdda
{
    public class CddaPlayerCoords
    {
        public Point Abspos { get; private set; }
        public Point3DInt OvermapRegion { get; private set; }
        public Point3DInt SavegameLev { get; private set; }
        public Point3DInt SavegamePos { get; private set; }

        public CddaPlayerCoords(Point abspos)
        {
            // https://discourse.cataclysmdda.org/t/maps-coordinate-systems-and-files/24935/7
            // https://discourse.cataclysmdda.org/t/save-corruption-proble/23688/3
            // https://discourse.cataclysmdda.org/t/player-position-data-on-save-game-how-does-it-work/

            Abspos = abspos;

            var playerTileMinusRealityBubbleCoords = new CddaTileCoords(
                new Point(
                    abspos.X - CddaMap.REALITY_BUBBLE_RADIUS,
                    abspos.Y - CddaMap.REALITY_BUBBLE_RADIUS
                )
            );

            var realityBubbleTopLefCoords = new CddaTileCoords(
                new Point(
                    abspos.X - CddaMap.REALITY_BUBBLE_RADIUS
                             - playerTileMinusRealityBubbleCoords.RelPosInSubmap.X,
                    abspos.Y - CddaMap.REALITY_BUBBLE_RADIUS
                             - playerTileMinusRealityBubbleCoords.RelPosInSubmap.Y
                )
            );

            OvermapRegion = realityBubbleTopLefCoords.OvermapRegion;

            static int toSavegameLev(
                int realityBubbleTopLefOvermapTile,
                int realityBubbleTopLefSubmapIdx
            ) => MathExt.MathMod(
                realityBubbleTopLefOvermapTile * 2,
                CddaMap.SUBMAP_TILES_PER_REGION
            ) + realityBubbleTopLefSubmapIdx;

            SavegameLev = new Point3DInt((
                toSavegameLev(
                    realityBubbleTopLefCoords.OvermapTile.X,
                    realityBubbleTopLefCoords.SubmapIdx.X
                ),
                toSavegameLev(
                    realityBubbleTopLefCoords.OvermapTile.Y,
                    realityBubbleTopLefCoords.SubmapIdx.Y
                )
            ));

            SavegamePos = new Point3DInt((
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
            Abspos = new Point(abspos.x, abspos.y);
            OvermapRegion = new Point3DInt(overmapRegion);
            SavegameLev = new Point3DInt(savegameLev);
            SavegamePos = new Point3DInt(savegamePos);
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
