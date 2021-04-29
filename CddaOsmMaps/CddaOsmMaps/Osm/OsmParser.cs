using CddaOsmMaps.Crosscutting;
using CddaOsmMaps.MapGen.Contracts;
using CddaOsmMaps.MapGen.Dtos;
using CddaOsmMaps.MapGen.Entities;
using OsmSharp;
using OsmSharp.API;
using OsmSharp.Complete;
using OsmSharp.Streams;
using OsmSharp.Tags;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CddaOsmMaps.Osm
{
    internal class OsmReader : IMapProvider
    {
        private readonly static Regex BOUNDS_TAG_REGEX =
            new Regex("<bounds (minlat|minlon|maxlat|maxlon)=\"(-?\\d+\\.\\d+)\" (minlat|minlon|maxlat|maxlon)=\"(-?\\d+\\.\\d+)\" (minlat|minlon|maxlat|maxlon)=\"(-?\\d+\\.\\d+)\" (minlat|minlon|maxlat|maxlon)=\"(-?\\d+\\.\\d+)\"/>");

        private const float DEFAULT_PIXELS_PER_METER = 1.2f;

        private const string MIN_LAT_KEY = "minlat";
        private const string MIN_LON_KEY = "minlon";
        private const string MAX_LAT_KEY = "maxlat";
        private const string MAX_LON_KEY = "maxlon";
        private const string RELATION_OUTER_ROLE = "outer";
        private static readonly string[] RELATION_OUTER_ROLES = new string[]
        {
            string.Empty, RELATION_OUTER_ROLE, "main_stream", "side_stream", "spring", "tributary"
            // "riverbank", "waterbody"
        };
        private const string RELATION_INNER_ROLE = "inner";
        private const string TAG_WATER_ATTR_KEY = "water";
        private const string TAG_WATERWAY_ATTR_KEY = "waterway";
        private const string TAG_NATURAL_ATTR_KEY = "natural";
        private const string TAG_COASTLINE_ATTR_VALUE = "coastline";
        // https://wiki.openstreetmap.org/wiki/Key:natural
        private readonly string[] TAG_WATER_ATTR_VALUES = new string[]
            { "water", "wetland", "glacier", "bay", "spring", "hot_spring" };
        private const string TAG_HIGHWAY_ATTR_KEY = "highway";
        private readonly string[] TAG_TUNNEL_ROAD_ATTR_KEYS = new string[]
            { "tunnel", "corridor" };
        private const string TAG_BRIDGE_ATTR_KEY = "bridge";
        private const string TAG_BUILDING_ATTR_KEY = "building";
        private const string TAG_LANDUSE_ATTR_KEY = "landuse";
        private const string YES_VALUE = "yes";

        private readonly string OsmXmlFilepath;
        private readonly Bounds Bounds;
        private readonly (float lat, float lon) Scales;
        private readonly bool Log;

        public (int width, int height) MapSize { get; private set; }
        public float PixelsPerMeter { get; private set; }

        public OsmReader(
            string osmXmlFilepath,
            Bounds bounds = null,
            float? pixelsPerMeter = null,
            bool log = false
        )
        {
            OsmXmlFilepath = osmXmlFilepath;
            Bounds = bounds ?? GetBoundsFromXml();
            PixelsPerMeter = pixelsPerMeter ?? DEFAULT_PIXELS_PER_METER;
            Log = log;

            var avgLat = (Bounds.MinLatitude + Bounds.MaxLatitude) / 2 ?? 0;
            var metersPerLonDegree = Gis.MetersPerLonDegree(avgLat);
            Scales = (
                lat: PixelsPerMeter * Gis.METERS_PER_LAT_DEG,
                lon: PixelsPerMeter * metersPerLonDegree
            );

            var boundSizes = (
                lat: Bounds.MaxLatitude - Bounds.MinLatitude ?? 0,
                lon: Bounds.MaxLongitude - Bounds.MinLongitude ?? 0
            );
            var mapSize = Scale(boundSizes);
            MapSize = ((int)mapSize.lon, (int)mapSize.lat); // reversed lat <-> lon
        }

        public MapElements GetMapElements()
        {
            using var fileStream = File.OpenRead(OsmXmlFilepath);

            var source = (
                    OsmXmlFilepath.EndsWith("pbf")
                    ? (OsmStreamSource)new PBFOsmStreamSource(fileStream)
                    : new XmlOsmStreamSource(fileStream)
                )
                /*
                ? Does not work
                .FilterBox(
                    Bounds.MinLongitude.Value,
                    Bounds.MinLatitude.Value,
                    Bounds.MaxLongitude.Value,
                    Bounds.MaxLatitude.Value,
                    completeWays: true
                )*/
                .ToComplete()
                .Where(el => el.Type == OsmGeoType.Way || el.Type == OsmGeoType.Relation)
                .ToList();

            return new MapElements
            {
                Coastlines = GetCoastlines(source),
                LandAreas = GetWaterAreas(source) // water first 
                    .Concat(GetAreas(source)),    // to prioritize land
                Rivers = GetRivers(source),
                Roads = GetRoads(source),
                Buildings = GetBuildings(source)
            };
        }

        private Bounds GetBoundsFromXml()
        {
            using var fileStream = new StreamReader(OsmXmlFilepath);
            string line;
            while ((line = fileStream.ReadLine()) != null)
            {
                var match = BOUNDS_TAG_REGEX.Match(line);
                if (match.Success)
                {
                    var groups = match.Groups;
                    var bounds = Enumerable
                        .Range(0, 4)
                        .ToDictionary(
                            i => groups[i * 2 + 1].Value,
                            i => float.Parse(groups[(i + 1) * 2].Value.Replace(".", ","))
                        );

                    return new Bounds
                    {
                        MinLatitude = bounds[MIN_LAT_KEY],
                        MinLongitude = bounds[MIN_LON_KEY],
                        MaxLatitude = bounds[MAX_LAT_KEY],
                        MaxLongitude = bounds[MAX_LON_KEY]
                    };
                }
            }

            return null;
        }

        private IEnumerable<Coastline> GetCoastlines(List<ICompleteOsmGeo> ways)
            => ProcessData(ways, way =>
                    (way.Tags?.ContainsKey(TAG_NATURAL_ATTR_KEY) ?? false)
                    && way.Tags[TAG_NATURAL_ATTR_KEY] == TAG_COASTLINE_ATTR_VALUE
                )
                .Select(data => new Coastline(data.polygons))
                .ToList();

        private IEnumerable<LandArea> GetAreas(List<ICompleteOsmGeo> ways)
            => ProcessData(ways, way => way.Tags?.ContainsKey(TAG_LANDUSE_ATTR_KEY) ?? false)
                .Select(data => new LandArea(
                    data.polygons,
                    data.tags[TAG_LANDUSE_ATTR_KEY]
                ));

        private IEnumerable<LandArea> GetWaterAreas(List<ICompleteOsmGeo> ways)
            => ProcessData(ways, way =>
                    (way.Tags?.ContainsKey(TAG_WATER_ATTR_KEY) ?? false)
                    || (
                        (way.Tags?.ContainsKey(TAG_NATURAL_ATTR_KEY) ?? false)
                        && TAG_WATER_ATTR_VALUES.Contains(way.Tags[TAG_NATURAL_ATTR_KEY])
                    )
                )
                .Select(data => new LandArea(
                    data.polygons,
                    data.tags[TAG_NATURAL_ATTR_KEY]
                ));

        private IEnumerable<River> GetRivers(List<ICompleteOsmGeo> ways)
            => ProcessData(ways, way => way.Tags?.ContainsKey(TAG_WATERWAY_ATTR_KEY) ?? false)
                .Select(data => new River(
                    data.polygons,
                    data.tags[TAG_WATERWAY_ATTR_KEY]
                ));

        private IEnumerable<Road> GetRoads(List<ICompleteOsmGeo> ways)
            => ProcessData(ways, way => way.Tags?.ContainsKey(TAG_HIGHWAY_ATTR_KEY) ?? false)
                // TODO <tag k="footway" v="sidewalk | crossing"/>
                .Select(data => new Road(
                    data.polygons,
                    data.tags[TAG_HIGHWAY_ATTR_KEY],
                    // TODO get Layer & level (can be both with different values)
                    // https://wiki.openstreetmap.org/wiki/Key:layer
                    // https://wiki.openstreetmap.org/wiki/Key:level
                    TAG_TUNNEL_ROAD_ATTR_KEYS.Any(tag => data.tags.ContainsKey(tag)),
                    data.tags.ContainsKey(TAG_BRIDGE_ATTR_KEY)
                ));

        private IEnumerable<Building> GetBuildings(List<ICompleteOsmGeo> ways)
            => ProcessData(ways, way => way.Tags?.ContainsKey(TAG_BUILDING_ATTR_KEY) ?? false)
                // TODO get buildings from nodes ????
                /*
                    <node id="1762918480" visible="true" version="2" changeset="46461390" 
                          timestamp="2017-02-28T08:27:29Z" user="Marion_Moseby" uid="5379200" 
                          lat="40.4447497" lon="-3.8086511">
                      <tag k="amenity" v="cafe"/>
                      <tag k="cuisine" v="coffee_shop"/>
                      <tag k="name" v="Hielo Picado"/>
                     </node>
                */
                // TODO get other building types, examples:
                // <tag k="building" v="yes"/> + <tag k="railway" v="station"/>
                // <tag k="amenity" v="theatre"/>
                // <tag k="amenity" v="school"/>
                // <tag k="amenity" v="restaurant"/>
                // <tag k="leisure" v="sports_centre"/>
                // <tag k="cuisine" v="gallega"/>
                // <tag k="shop" v="sports"/>
                // <tag k="shop" v="supermarket"/>

                // TODO get building name

                // TODO get building:levels
                .Select(data =>
                {
                    var buildingType = data.tags[TAG_BUILDING_ATTR_KEY];
                    var building = new Building(
                        data.polygons,
                        buildingType == YES_VALUE ? string.Empty : buildingType
                    );

                    return building;
                })
                .ToList();

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

        public PointFloat Scale(PointFloat point)
        {
            if (point != null)
            {
                var (x, y) = LatLonToXY((point.X, point.Y));
                point.X = x;
                point.Y = y;
            }

            return point;
        }

        private (float lat, float lon) Scale((float lat, float lon) coords)
            => (
                lat: Scales.lat * coords.lat,
                lon: Scales.lon * coords.lon
            );

        private (float x, float y) LatLonToXY(Node node)
            => LatLonToXY(((float)node.Latitude, (float)node.Longitude));

        private (float x, float y) LatLonToXY((float lat, float lon) coords)
            => Scale((
                coords.lat - Bounds.MinLatitude ?? 0,
                coords.lon - Bounds.MinLongitude ?? 0
            ));
    }
}
