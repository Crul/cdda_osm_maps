using System.Collections.Generic;
using System.Linq;

namespace CddaOsmMaps.Crosscutting
{
    internal static class EnumExt
    {
        public static IEnumerable<int> Range(int count)
            => Enumerable.Range(0, count);

        public static IEnumerable<int> RangeCount(int from, int count)
            => Enumerable.Range(from, (count - from) + 1);
    }
}
