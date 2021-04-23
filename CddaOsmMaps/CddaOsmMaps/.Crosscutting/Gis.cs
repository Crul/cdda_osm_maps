using System;

namespace CddaOsmMaps.Crosscutting
{
    internal class Gis
    {
        public const float METERS_PER_LAT_DEG = 111319;

        public static float MetersPerLonDegree(float lat)
            => 40075000 * (float)Math.Cos(MathExt.ToRadians(lat)) / 360;
    }
}
