using CddaOsmMaps.Crosscutting;
using System;

namespace CddaOsmMaps.Cdda
{
    internal class CddaTileCoords
    {
        public (int x, int y) Abspos { get; private set; }
        public Point3D OvermapRegion { get; internal set; }
        public Point3D Segment { get; private set; }
        public Point3D OvermapTile { get; private set; }
        public Point3D Submap { get; private set; }
        public Point3D SubmapIdx { get; private set; }
        public Point3D RelPosInSubmap { get; set; }

        public CddaTileCoords((int x, int y) abspos)
        {
            Abspos = abspos;

            // OVERMAP REGION
            // floor(abspos / 4302)
            static int toOvermapRegion(int abspos)
                => (int)Math.Floor((double)abspos / CddaMap.OVERMAP_REGION_SIZE);

            var overmapRegion = (
                x: toOvermapRegion(abspos.x),
                y: toOvermapRegion(abspos.y)
            );

            // SEGMENT
            // floor(abspos / 768)
            static int toSegment(int abspos)
                => (int)Math.Floor((double)abspos / CddaMap.SEGMENT_SIZE);

            var segment = (
                x: toSegment(abspos.x),
                y: toSegment(abspos.y)
            );

            // IN SEGMENT RELATIVE POSITION relposInSegment
            // abspos % 768
            static int toPosInSegment(int abspos)
                => MathExt.MathMod(abspos, CddaMap.SEGMENT_SIZE);

            var posInSegment = (
                x: toPosInSegment(abspos.x),
                y: toPosInSegment(abspos.y)
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
                => (int)Math.Floor((double)relOvermapTile + (segment * CddaMap.OVERMAP_TILES_PER_SEGMENT));

            var overmapTile = (
                x: toOvermapTileFile(overmapTileInSegment.x, segment.x),
                y: toOvermapTileFile(overmapTileInSegment.y, segment.y)
            );

            // SUBMAP INDEX IN OVERMAP TILE (4x SUBMAP) FILE
            // floor(relposInSegment / 12)
            static int toSubmapIdxInOvermapTile(int relposInSegment, int overmapTile)
                => (int)Math.Floor((double)relposInSegment / CddaMap.SUBMAP_SIZE)
                    - (2 * MathExt.MathMod(overmapTile, CddaMap.OVERMAP_TILES_PER_SEGMENT));

            var submapIdxInOvermapTile = (
                x: toSubmapIdxInOvermapTile(posInSegment.x, overmapTile.x),
                y: toSubmapIdxInOvermapTile(posInSegment.y, overmapTile.y)
            );

            // IN SUBMAP RELATIVE POSITION
            // abspos % 12
            static int toRelposSubmap(int abspos)
                => MathExt.MathMod(abspos, CddaMap.SUBMAP_SIZE);

            var relPosInSubmap = (
                x: toRelposSubmap(abspos.x),
                y: toRelposSubmap(abspos.y)
            );

            OvermapRegion = new Point3D(overmapRegion);
            Segment = new Point3D(segment);
            OvermapTile = new Point3D(overmapTile);
            SubmapIdx = new Point3D(submapIdxInOvermapTile);
            RelPosInSubmap = new Point3D(relPosInSubmap);
        }
    }
}
