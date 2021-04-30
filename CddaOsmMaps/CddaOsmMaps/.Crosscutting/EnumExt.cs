using System.Collections.Generic;
using System.Linq;

namespace CddaOsmMaps.Crosscutting
{
    internal static class EnumExt
    {
        public static IEnumerable<int> Range(int count)
            => Enumerable.Range(0, count);

        public static IEnumerable<int> RangeFromTo(int from, int to)
            => Enumerable.Range(from, (to - from) + 1);
    }
}
