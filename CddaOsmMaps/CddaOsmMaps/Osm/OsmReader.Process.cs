using CddaOsmMaps.MapGen.Entities;
using OsmSharp;
using OsmSharp.API;
using OsmSharp.Complete;
using OsmSharp.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace CddaOsmMaps.Osm
{
    partial class OsmReader
    {
        private IEnumerable<(List<Polygon> polygons, TagsCollection tags)> ProcessData(
            List<ICompleteOsmGeo> ways,
            Func<ICompleteOsmGeo, bool> predicate
        ) => ways.Where(predicate)
                .Select(ProcessWayOrRelation)
                .Select(complexWay => complexWay.GetData(LatLonToXY))
                .Where(data => data.polygons.Count > 0);

        private ComplexWay ProcessWayOrRelation(ICompleteOsmGeo osm)
            => ProcessWayOrRelation(osm, null); // no overloading because lambda functions

        private ComplexWay ProcessWayOrRelation(ICompleteOsmGeo osm, IEnumerable<Tag> tags)
        {
            if (osm is CompleteWay)
            {
                var way = (CompleteWay)osm;
                AddTagsToWay(tags, way);

                return new ComplexWay(way);
            }

            var relation = (CompleteRelation)osm;
            var allTags = relation.Tags.ToList();
            if (tags != null)
                allTags.AddRange(tags);

            var openWays = relation.Members
                .Where(relmember => relmember.Member.Type == OsmGeoType.Way)
                .Select(relmember => (CompleteWay)relmember.Member)
                .Where(way => !way.IsClosed())
                .ToList();

            var unprocessedMembers = relation.Members.ToList();
            var waysInfos = new List<CompleteWayInfo>();
            while (unprocessedMembers.Count > 0)
            {
                var relMember = unprocessedMembers.First();
                unprocessedMembers.Remove(relMember);
                switch (relMember.Member.Type)
                {
                    case OsmGeoType.Way:
                        var way = (CompleteWay)relMember.Member;
                        AddTagsToWay(allTags, way);

                        var isOuterRole = RELATION_OUTER_ROLES.Contains(relMember.Role);
                        var isInnerRole = relMember.Role == RELATION_INNER_ROLE;
                        if (!isOuterRole && !isInnerRole)
                        {
                            // https://wiki.openstreetmap.org/wiki/Types_of_relation
                            if (Log) Console.WriteLine($"WARNING: Unhandled relation role: {relMember.Role}");
                            continue;
                        }

                        if (way.IsClosed() || TryCloseOpenWay(relMember, unprocessedMembers, openWays))
                            waysInfos.Add(new CompleteWayInfo(way, isOuterRole));

                        break;

                    case OsmGeoType.Relation:
                        var isRelationOuterRole = RELATION_OUTER_ROLES.Contains(relMember.Role);
                        var relationComplexWay = ProcessWayOrRelation(
                            relMember.Member,
                            (tags ?? Enumerable.Empty<Tag>()).Concat(relation.Tags)
                        );
                        waysInfos.AddRange(relationComplexWay.WayInfos);
                        break;

                    case OsmGeoType.Node: default: break; // nodes ignored
                }
            }

            return new ComplexWay(waysInfos, allTags);
        }

        private bool TryCloseOpenWay(
            CompleteRelationMember openWayRelMember,
            List<CompleteRelationMember> unprocessedMembers,
            List<CompleteWay> openWays
        )
        {
            var way = (CompleteWay)openWayRelMember.Member;
            openWays.Remove(way);

            var merginWayNodes = way.Nodes.ToList();
            while (true)
            {
                var firstNode = merginWayNodes.First();
                var lastNode = merginWayNodes.Last();
                if (firstNode == lastNode)
                {
                    way.Nodes = merginWayNodes.Skip(1).ToArray();
                    return true;
                }

                var adjacentWays = openWays
                    .Where(orw =>
                        orw.Nodes.First() == lastNode
                        || orw.Nodes.Last() == firstNode
                        || orw.Nodes.First() == firstNode
                        || orw.Nodes.Last() == lastNode
                    )
                    .ToList();

                // whe 3 open ways form a closed one, the first one will find 2 adjacen ways
                var adjacentWay = adjacentWays.FirstOrDefault();

                if (adjacentWay == null)
                {
                    if (Log)
                    {
                        Console.WriteLine($"WARNING: not fully closed way [Id: {way.Id}].");
                        // Console.WriteLine(string.Join(",", way.Tags.Select(t => $"{t.Key}={t.Value}")));
                        // Console.WriteLine(JsonSerializer.Serialize(way));
                    }
                    return false;
                }

                if (lastNode == adjacentWay.Nodes.First())
                    merginWayNodes.AddRange(adjacentWay.Nodes.Skip(1));

                else if (firstNode == adjacentWay.Nodes.Last())
                    merginWayNodes = adjacentWay.Nodes
                        .Concat(merginWayNodes.Skip(1))
                        .ToList();

                else if (lastNode == adjacentWay.Nodes.Last())
                    merginWayNodes.AddRange(adjacentWay.Nodes.Reverse().Skip(1));

                else if (firstNode == adjacentWay.Nodes.First())
                    merginWayNodes = adjacentWay.Nodes
                        .Reverse()
                        .Concat(merginWayNodes.Skip(1))
                        .ToList();

                openWays.Remove(adjacentWay);
                unprocessedMembers.Remove(
                    unprocessedMembers.Single(um => um.Member == adjacentWay)
                );
            }
        }

        private static void AddTagsToWay(IEnumerable<Tag> tags, CompleteWay way)
        {
            if (tags == null)
                return;

            if (way.Tags == null)
                way.Tags = new TagsCollection(tags);
            else
                foreach (var tag in tags)
                    if (!way.Tags.ContainsKey(tag.Key))
                        way.Tags.Add(tag);
        }

        public Vector2 LatLonToXY(Vector2 coords)
            => Scale((
                coords.X - Bounds.MinLatitude ?? 0,
                coords.Y - Bounds.MinLongitude ?? 0
            ));

        private Vector2 LatLonToXY(Node node)
            => LatLonToXY(new Vector2((float)node.Latitude, (float)node.Longitude));

        private Vector2 Scale((float lat, float lon) coords)
            => new Vector2(
                Scales.lat * coords.lat,
                Scales.lon * coords.lon
            );
    }
}
