using System;

namespace CddaOsmMaps.Crosscutting
{
    internal class MathExt
    {
        public static int MathMod(int a, int b)
            => (Math.Abs(a * b) + a) % b;

        public static float ToRadians(float angleInDegrees)
            => (float)(angleInDegrees * Math.PI) / 180;
    }
}
