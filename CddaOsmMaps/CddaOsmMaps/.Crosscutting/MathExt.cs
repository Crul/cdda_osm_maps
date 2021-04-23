using System;

namespace CddaOsmMaps.Crosscutting
{
    internal class MathExt
    {
        public static int MathMod(int a, int b)
            => (Math.Abs(a * b) + a) % b;
    }
}
