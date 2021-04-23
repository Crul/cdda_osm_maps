using CddaOsmMaps.Crosscutting;
using CddaOsmMaps.MapGen.Contracts;
using CddaOsmMaps.MapGen.Dtos;
using CddaOsmMaps.MapGen.Entities;
using OsmSharp;
using OsmSharp.API;
using OsmSharp.Complete;
using OsmSharp.Streams;
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

        private const float PIXELS_PER_METER = 1;

        private const string MIN_LAT_KEY = "minlat";
        private const string MIN_LON_KEY = "minlon";
        private const string MAX_LAT_KEY = "maxlat";
        private const string MAX_LON_KEY = "maxlon";
        private const string TAG_HIGHWAY_ATTR_VALUE = "highway";
        private const string TAG_BUILDING_ATTR_KEY = "building";
        private const string TAG_LANDUSE_ATTR_KEY = "landuse";
        private const string YES_VALUE = "yes";

        private readonly string OsmXmlFilepath;
        private readonly Bounds Bounds;
        private readonly (float lat, float lon) Scales;

        public (int width, int height) MapSize { get; private set; }
        public float PixelsPerMeter => PIXELS_PER_METER;

        public OsmReader(string osmXmlFilepath)
        {
            OsmXmlFilepath = osmXmlFilepath;

            // OsmSharp ignores <bounds/> element
            // https://github.com/OsmSharp/core/issues/116
            Bounds = GetBounds();

            var avgLat = (Bounds.MinLatitude + Bounds.MaxLatitude) / 2 ?? 0;
            var metersPerLonDegree = Gis.MetersPerLonDegree(avgLat);
            Scales = (
                lat: PIXELS_PER_METER * Gis.METERS_PER_LAT_DEG,
                lon: PIXELS_PER_METER * metersPerLonDegree
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
            var source = new XmlOsmStreamSource(fileStream);

            return new MapElements
            {
                LandAreas = GetAreas(source),
                Roads = GetRoads(source),
                Buildings = GetBuildings(source)
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

        private List<LandArea> GetAreas(XmlOsmStreamSource source)
            => source.Where(el =>
                    el.Type == OsmGeoType.Node
                    || (
                        el.Type == OsmGeoType.Way
                        && (el.Tags?.ContainsKey(TAG_LANDUSE_ATTR_KEY) ?? false)
                    )
                )
                .ToComplete()
                .Where(osm => osm.Type == OsmGeoType.Way)
                .Select(way => (CompleteWay)way)
                .Select(way => new LandArea(
                    way.Tags[TAG_LANDUSE_ATTR_KEY],
                    way.Nodes.Select(Scale).ToList()
                ))
                .ToList();

        private List<Road> GetRoads(XmlOsmStreamSource source)
            => source.Where(el =>
                    el.Type == OsmGeoType.Node
                    || (
                        el.Type == OsmGeoType.Way
                        && (el.Tags?.ContainsKey(TAG_HIGHWAY_ATTR_VALUE) ?? false)
                    )
                )
                .ToComplete()
                .Where(osm => osm.Type == OsmGeoType.Way)
                .Select(way => (CompleteWay)way)
                .Select(way => new Road(
                    way.Tags[TAG_HIGHWAY_ATTR_VALUE],
                    way.Nodes.Select(Scale).ToList()
                ))
                .ToList();

        private List<Building> GetBuildings(XmlOsmStreamSource source)
            => source.Where(el =>
                    el.Type == OsmGeoType.Node
                    || (
                        el.Type == OsmGeoType.Way
                        && (el.Tags?.ContainsKey(TAG_BUILDING_ATTR_KEY) ?? false)
                    )
                )
                .ToComplete()
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
                .Where(osm => osm.Type == OsmGeoType.Way)
                .Select(way => (CompleteWay)way)
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
