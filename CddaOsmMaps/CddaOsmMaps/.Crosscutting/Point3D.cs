namespace CddaOsmMaps.Crosscutting
{
    internal struct Point3D
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Z { get; private set; }

        public Point3D((int x, int y) point) : this()
        {
            X = point.x;
            Y = point.y;
            Z = 0;
        }
    }
}
