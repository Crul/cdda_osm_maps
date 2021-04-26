using OsmSharp.Complete;
using System;

namespace CddaOsmMaps.Osm
{
    internal class CompleteWayInfo : Tuple<CompleteWay, bool>
    {
        public CompleteWay Way { get => Item1; }
        public bool IsOuter { get => Item2; }

        public CompleteWayInfo(CompleteWay way, bool isOuter)
            : base(way, isOuter) { }
    }
}
