using CddaOsmMaps.Crosscutting;

namespace CddaOsmMaps.Cdda
{
    internal class CddaCoords
    {
        public Point3D Segment { get; set; }
        public Point3D Submap4xFile { get; set; }
        public Point3D SubmapIdx { get; set; }
        public Point3D SubmapRelPos { get; set; }
    }
}
