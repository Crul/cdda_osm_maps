using System;

namespace CddaOsmMaps.Crosscutting
{
    internal static class Geom
    {
        // https://stackoverflow.com/a/12892493
        public static float GetAngle(
            (float lat, float lon) point1,
            (float lat, float lon) point2
        )
        {
            var xDiff = point1.lat - point2.lat;
            var yDiff = point1.lon - point2.lon;
            var angle = MathExt.ToDegrees((float)Math.Atan2(yDiff, xDiff));

            return angle;
        }
    }
}
