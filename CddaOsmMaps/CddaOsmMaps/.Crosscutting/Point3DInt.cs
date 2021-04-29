using System;

namespace CddaOsmMaps.Crosscutting
{
    public struct Point3DInt
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Z { get; private set; }

        public Point3DInt((int x, int y) point) : this()
        {
            X = point.x;
            Y = point.y;
            Z = 0;
        }

        public static bool operator ==(Point3DInt left, Point3DInt right)
            => left.Equals(right);

        public static bool operator !=(Point3DInt left, Point3DInt right)
            => !(left == right);

        public override bool Equals(object obj)
            => obj is Point3DInt
                ? Equals((Point3DInt)obj)
                : false;

        public override int GetHashCode()
            => HashCode.Combine(X, Y, Z);

        private bool Equals(Point3DInt obj)
            => obj.X == X
            && obj.Y == Y
            && obj.Z == Z;
    }
}
