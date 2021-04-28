using CddaOsmMaps.Crosscutting;

namespace CddaOsmMaps.Cdda
{
    internal class CddaCoords
    {
        public (int x, int y) Abspos { get; private set; }
        public Point3D Segment { get; private set; }
        public Point3D OvermapTileFile { get; private set; }
        public Point3D SubmapIdx { get; private set; }
        public Point3D SubmapRelPos { get; private set; }

        public CddaCoords(
            (int x, int y) abspos,
            Point3D segment,
            Point3D overmapTileFile,
            Point3D submapIdx,
            Point3D submapRelPos
        )
        {
            Abspos = abspos;
            Segment = segment;
            OvermapTileFile = overmapTileFile;
            SubmapIdx = submapIdx;
            SubmapRelPos = submapRelPos;
        }
    }
}
