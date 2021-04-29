using CddaOsmMaps.Crosscutting;
using System;
using System.Drawing;

namespace CddaOsmMaps.Cdda
{
    internal class CddaTileCoords
    {
        public Point Abspos { get; private set; }
        public Point3DInt OvermapRegion { get; internal set; }
        public Point3DInt Segment { get; private set; }
        public Point3DInt OvermapTile { get; private set; }
        public Point3DInt Submap { get; private set; }
        public Point3DInt SubmapIdx { get; private set; }
        public Point3DInt RelPosInSubmap { get; set; }

        public CddaTileCoords(Point abspos)
        {
            Abspos = abspos;

            // OVERMAP REGION
            // floor(abspos / 4302)
            static int toOvermapRegion(int abspos)
                => (int)Math.Floor((double)abspos / CddaMap.OVERMAP_REGION_SIZE);

            var overmapRegion = (
                x: toOvermapRegion(abspos.X),
                y: toOvermapRegion(abspos.Y)
            );

            // SEGMENT
            // floor(abspos / 768)
            static int toSegment(int abspos)
                => (int)Math.Floor((double)abspos / CddaMap.SEGMENT_SIZE);

            var segment = (
                x: toSegment(abspos.X),
                y: toSegment(abspos.Y)
            );

            // IN SEGMENT RELATIVE POSITION relposInSegment
            // abspos % 768
            static int toPosInSegment(int abspos)
                => MathExt.MathMod(abspos, CddaMap.SEGMENT_SIZE);

            var posInSegment = (
                x: toPosInSegment(abspos.X),
                y: toPosInSegment(abspos.Y)
            );

            // IN SEGMENT RELATIVE OVERMAP (4x SUBMAP) relOvermapTile
            // floor(relposInSegment / 24)
            static int toOvermapTileInSegment(int relposInSegment)
                => (int)Math.Floor((double)relposInSegment / CddaMap.OVERMAP_TILE_SIZE);

            var overmapTileInSegment = (
                x: toOvermapTileInSegment(posInSegment.x),
                y: toOvermapTileInSegment(posInSegment.y)
            );

            // ABSOLUTE OVERMAP TILE (4x SUBMAP) FILE
            // relOvermapTile + segment * 32
            static int toOvermapTileFile(int relOvermapTile, int segment)
                => (int)Math.Floor((double)relOvermapTile + (segment * CddaMap.SEGMENT_SIZE_IN_OVERMAP_TILES));

            var overmapTile = (
                x: toOvermapTileFile(overmapTileInSegment.x, segment.x),
                y: toOvermapTileFile(overmapTileInSegment.y, segment.y)
            );

            // SUBMAP INDEX IN OVERMAP TILE (4x SUBMAP) FILE
            // floor(relposInSegment / 12)
            static int toSubmapIdxInOvermapTile(int relposInSegment, int overmapTile)
                => (int)Math.Floor((double)relposInSegment / CddaMap.SUBMAP_SIZE)
                    - (2 * MathExt.MathMod(overmapTile, CddaMap.SEGMENT_SIZE_IN_OVERMAP_TILES));

            var submapIdxInOvermapTile = (
                x: toSubmapIdxInOvermapTile(posInSegment.x, overmapTile.x),
                y: toSubmapIdxInOvermapTile(posInSegment.y, overmapTile.y)
            );

            // IN SUBMAP RELATIVE POSITION
            // abspos % 12
            static int toRelposSubmap(int abspos)
                => MathExt.MathMod(abspos, CddaMap.SUBMAP_SIZE);

            var relPosInSubmap = (
                x: toRelposSubmap(abspos.X),
                y: toRelposSubmap(abspos.Y)
            );

            OvermapRegion = new Point3DInt(overmapRegion);
            Segment = new Point3DInt(segment);
            OvermapTile = new Point3DInt(overmapTile);
            SubmapIdx = new Point3DInt(submapIdxInOvermapTile);
            RelPosInSubmap = new Point3DInt(relPosInSubmap);
        }
    }
}
