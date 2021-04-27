using CddaOsmMaps.Crosscutting;
using CddaOsmMaps.MapGen.Contracts;
using CddaOsmMaps.MapGen.Dtos;
using CddaOsmMaps.MapGen.Entities;
using OsmSharp;
using OsmSharp.API;
using OsmSharp.Complete;
using OsmSharp.Streams;
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
        private const string TAG_WATER_ATTR_KEY = "water";
        private const string TAG_WATERWAY_ATTR_KEY = "waterway";
        private const string TAG_NATURAL_ATTR_KEY = "natural";
        private const string TAG_COASTLINE_ATTR_VALUE = "coastline";
        // https://wiki.openstreetmap.org/wiki/Key:natural
        private readonly string[] TAG_WATER_ATTR_VALUES = new string[]
            { "water", "wetland", "glacier", "bay", "spring", "hot_spring" };
        private const string TAG_HIGHWAY_ATTR_KEY = "highway";
        private const string TAG_BUILDING_ATTR_KEY = "building";
        private const string TAG_LANDUSE_ATTR_KEY = "landuse";
        private const string YES_VALUE = "yes";

        private readonly string OsmXmlFilepath;
        private readonly Bounds Bounds;
        private readonly (float lat, float lon) Scales;

        public (int width, int height) MapSize { get; private set; }
        public float PixelsPerMeter { get; private set; }

        public OsmReader(string osmXmlFilepath, Bounds bounds = null, float? pixelsPerMeter = null)
        {
            OsmXmlFilepath = osmXmlFilepath;
            Bounds = bounds ?? GetBounds();
            PixelsPerMeter = pixelsPerMeter ?? DEFAULT_PIXELS_PER_METER;

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
                .ToList();

            return new MapElements
            {
                Coastlines = GetCoastlines(source).ToList(),
                LandAreas = GetAreas(source)
                    .Concat(GetWaterAreas(source))
                    .ToList(),
                Rivers = GetRivers(source).ToList(),
                Roads = GetRoads(source).ToList(),
                Buildings = GetBuildings(source).ToList()
            };
        }

        private Bounds GetBounds()
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

        private IEnumerable<Coastline> GetCoastlines(List<ICompleteOsmGeo> source)
            => source
                .Where(osm => (osm.Tags?.ContainsKey(TAG_NATURAL_ATTR_KEY) ?? false)
                    && osm.Tags[TAG_NATURAL_ATTR_KEY] == TAG_COASTLINE_ATTR_VALUE)
                .SelectMany(GetWaysFromWayOrRelation)
                .Select(way => way.Nodes.Select(Scale).ToList())
                .Select(points => new Coastline(points))
                .ToList();

        private IEnumerable<LandArea> GetAreas(List<ICompleteOsmGeo> source)
            => source
                .Where(el =>
                    (el.Type == OsmGeoType.Way || el.Type == OsmGeoType.Relation)
                    && (el.Tags?.ContainsKey(TAG_LANDUSE_ATTR_KEY) ?? false)
                )
                .SelectMany(GetWaysFromWayOrRelation)
                .Select(way => new LandArea(
                    way.Tags[TAG_LANDUSE_ATTR_KEY],
                    way.Nodes.Select(Scale).ToList()
                ));

        private IEnumerable<LandArea> GetWaterAreas(List<ICompleteOsmGeo> source)
            => source
                .Where(el =>
                    (el.Type == OsmGeoType.Way || el.Type == OsmGeoType.Relation)
                    && (
                        (el.Tags?.ContainsKey(TAG_WATER_ATTR_KEY) ?? false)
                        || (
                            (el.Tags?.ContainsKey(TAG_NATURAL_ATTR_KEY) ?? false)
                            && TAG_WATER_ATTR_VALUES.Contains(el.Tags[TAG_NATURAL_ATTR_KEY])
                        )
                    )
                )
                .SelectMany(GetWaysFromWayOrRelation)
                .Select(way => new LandArea(
                    way.Tags[TAG_NATURAL_ATTR_KEY],
                    way.Nodes.Select(Scale).ToList()
                ));

        private IEnumerable<River> GetRivers(List<ICompleteOsmGeo> source)
            => source
                .Where(el =>
                    (el.Type == OsmGeoType.Way || el.Type == OsmGeoType.Relation)
                    && (el.Tags?.ContainsKey(TAG_WATERWAY_ATTR_KEY) ?? false)
                )
                .SelectMany(GetWaysFromWayOrRelation)
                .Select(way => new River(
                    way.Tags[TAG_WATERWAY_ATTR_KEY],
                    way.Nodes.Select(Scale).ToList()
                ));

        private IEnumerable<Road> GetRoads(List<ICompleteOsmGeo> source)
            => source
                // TODO <tag k="footway" v="sidewalk | crossing"/>
                .Where(el =>
                    (el.Type == OsmGeoType.Way || el.Type == OsmGeoType.Relation)
                    && (el.Tags?.ContainsKey(TAG_HIGHWAY_ATTR_KEY) ?? false)
                )
                .SelectMany(GetWaysFromWayOrRelation)
                .Select(way => new Road(
                    way.Tags[TAG_HIGHWAY_ATTR_KEY],
                    way.Nodes.Select(Scale).ToList()
                ));

        private IEnumerable<Building> GetBuildings(List<ICompleteOsmGeo> source)
            => source.Where(el =>
                    (el.Type == OsmGeoType.Way || el.Type == OsmGeoType.Relation)
                    && (el.Tags?.ContainsKey(TAG_BUILDING_ATTR_KEY) ?? false)
                )
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
                .SelectMany(GetWaysFromWayOrRelation)
                .Select(GetBuilding)
                .ToList();

        private Building GetBuilding(CompleteWay way)
        {
            var buildingType = way.Tags[TAG_BUILDING_ATTR_KEY];

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

            var building = new Building(
                buildingType == YES_VALUE ? string.Empty : buildingType,
                way.Nodes.Select(Scale).ToList()
            );

            return building;
        }

        private IEnumerable<CompleteWay> GetWaysFromWayOrRelation(ICompleteOsmGeo osm)
        {
            if (osm is CompleteWay)
                return new CompleteWay[] { (CompleteWay)osm };

            var relation = (CompleteRelation)osm;
            return relation
                .Members
                // TODO remove members w/Role="inner" on "multipolygon"
                .Where(relmember => relmember.Role == RELATION_OUTER_ROLE)
                .Select(relmember => relmember.Member)
                .Where(member => member.Type == OsmGeoType.Way)
                .Select(member =>
                {
                    var way = (CompleteWay)member;
                    if (way.Tags == null)
                        way.Tags = relation.Tags;
                    else
                        relation
                            .Tags
                            .Where(relTag => !way.Tags.ContainsKey(relTag.Key))
                            .ToList()
                            .ForEach(relTag => way.Tags.Add(relTag));

                    return way;
                });
        }

        private (float lat, float lon) Scale((float lat, float lon) coords)
            => (
                lat: Scales.lat * coords.lat,
                lon: Scales.lon * coords.lon
            );

        private (float lat, float lon) Scale(Node node)
            => Scale((
                (float)(node.Latitude - Bounds.MinLatitude ?? 0),
                (float)(node.Longitude - Bounds.MinLongitude ?? 0)
            ));
    }
}
