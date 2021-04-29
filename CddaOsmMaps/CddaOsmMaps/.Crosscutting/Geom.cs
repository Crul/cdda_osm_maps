using System;
using System.Numerics;

namespace CddaOsmMaps.Crosscutting
{
    internal static class Geom
    {
        // https://stackoverflow.com/a/12892493
        public static float GetAngle(Vector2 point1, Vector2 point2)
        {
            var xDiff = point1.X - point2.X;
            var yDiff = point1.Y - point2.Y;
            var angle = MathExt.ToDegrees((float)Math.Atan2(yDiff, xDiff));

            return angle;
        }
    }
}
