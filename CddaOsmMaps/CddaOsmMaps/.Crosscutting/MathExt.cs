using System;

namespace CddaOsmMaps.Crosscutting
{
    internal static class MathExt
    {
        public static int MathMod(int a, int b)
            => (Math.Abs(a * b) + a) % b;

        public static float ToRadians(float angleInDegrees)
            => (float)(angleInDegrees * Math.PI) / 180;

        public static float ToDegrees(float angleInRadians)
            => (float)(angleInRadians * 180 / Math.PI);

        public static int LimitToMultipleOf(float numberToLimit, int factor)
            => factor * (int)Math.Floor((double)(numberToLimit / factor));
    }
}
