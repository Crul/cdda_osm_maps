using CddaOsmMaps.MapGen.Entities;
using OsmSharp;
using OsmSharp.Complete;
using OsmSharp.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace CddaOsmMaps.Osm
{
    internal class ComplexWay
    {
        public List<CompleteWayInfo> WayInfos { get; private set; }

        private readonly TagsCollection Tags;

        public ComplexWay(
            List<CompleteWayInfo> ways,
            TagsCollection tags
        )
        {
            WayInfos = ways;
            Tags = tags ?? new TagsCollection();
        }

        public ComplexWay(
            List<CompleteWayInfo> ways,
            IEnumerable<Tag> tags
        ) : this(ways, new TagsCollection(tags)) { }

        public ComplexWay(CompleteWay way)
            : this(
                  new List<CompleteWayInfo> {
                      new CompleteWayInfo(way, true)
                  },
                  way.Tags
            )
        { }

        public (List<Polygon> polygons, TagsCollection tags) GetData(
            Func<Node, Vector2> scaleFn
        ) => (
                WayInfos.Select(wayInfo =>
                    new Polygon(
                        wayInfo.Way.Nodes.Select(scaleFn).ToList(),
                        wayInfo.IsOuter
                    )
                ).ToList(),
                Tags
            );
    }
}
