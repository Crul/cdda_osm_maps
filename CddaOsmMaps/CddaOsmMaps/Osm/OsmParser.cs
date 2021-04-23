using CddaOsmMaps.Crosscutting;
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
    internal class OsmReader
    {
        private readonly static Regex BOUNDS_TAG_REGEX =
            new Regex("<bounds (minlat|minlon|maxlat|maxlon)=\"(-?\\d+\\.\\d+)\" (minlat|minlon|maxlat|maxlon)=\"(-?\\d+\\.\\d+)\" (minlat|minlon|maxlat|maxlon)=\"(-?\\d+\\.\\d+)\" (minlat|minlon|maxlat|maxlon)=\"(-?\\d+\\.\\d+)\"/>");

        private const float PIXELS_PER_METER = 1;
        private const float METERS_PER_LAT_DEG = 111319;

        private const string MIN_LAT_KEY = "minlat";
        private const string MIN_LON_KEY = "minlon";
        private const string MAX_LAT_KEY = "maxlat";
        private const string MAX_LON_KEY = "maxlon";
        private const string TAG_HIGHWAY_ATTR_VALUE = "highway";

        private readonly float DEFAULT_ROAD_TYPE_WIDTH = 8;
        private readonly Dictionary<string, float> ROAD_TYPE_WIDTHS = new Dictionary<string, float>()
        {
            // https://wiki.openstreetmap.org/wiki/Key:highway
            { "motorway",       12 },
            { "motorway_link",  10 },
            { "trunk",          10 },
            { "trunk_link",      8 },
            { "primary",         9 },
            { "secondary",       8 },
            { "tertiary",        8 },
            { "tertiary_link",   6 },
            { "unclassified",    8 },
            { "residential",     8 },
            { "living_street",   8 },
            { "service",         6 },
            { "construction",    6 },
            { "track",           5 },
            { "pedestrian",      4 },
            { "cycleway",        4 },
            { "path",            4 },
            { "footway",         4 },
            { "steps",           3 },
        };

        private readonly string OsmXmlFilepath;
        private readonly Bounds Bounds;
        private readonly (float lat, float lon) Scales;

        public (int width, int height) OutputSize { get; set; }

        public OsmReader(string osmXmlFilepath)
        {
            OsmXmlFilepath = osmXmlFilepath;

            // OsmSharp ignores <bounds/> element
            // https://github.com/OsmSharp/core/issues/116
            Bounds = GetBounds();

            var avgLat = (Bounds.MinLatitude + Bounds.MaxLatitude) / 2 ?? 0;
            var metersPerLonDegree = MetersPerLonDegree(avgLat);
            Scales = (
                lat: PIXELS_PER_METER * METERS_PER_LAT_DEG,
                lon: PIXELS_PER_METER * metersPerLonDegree
            );

            var boundSizes = (
                lat: Bounds.MaxLatitude - Bounds.MinLatitude ?? 0,
                lon: Bounds.MaxLongitude - Bounds.MinLongitude ?? 0
            );
            var outputSize = Scale(boundSizes);
            OutputSize = ((int)outputSize.lon, (int)outputSize.lat); // reversed lat <-> lon
        }

        public void DrawWays(ImageBuilder image)
            => GetWays().ForEach(way => DrawWay(image, way));

        private void DrawWay(ImageBuilder image, CompleteWay way)
        {
            if (way.Nodes == null || way.Nodes.Length == 0)
                return;

            var roadType = way.Tags[TAG_HIGHWAY_ATTR_VALUE];
            var roadWidth = PIXELS_PER_METER * (
                ROAD_TYPE_WIDTHS.ContainsKey(roadType)
                ? ROAD_TYPE_WIDTHS[roadType]
                : DEFAULT_ROAD_TYPE_WIDTH
            );

            image.DrawPoints(
                way.Nodes.Select(Scale).ToList(),
                Common.ROAD_COLOR,
                roadWidth
            );
        }

        private List<CompleteWay> GetWays()
        {
            using var fileStream = File.OpenRead(OsmXmlFilepath);
            var source = new XmlOsmStreamSource(fileStream);

            var all = source
                .Where(el =>
                    el.Type == OsmGeoType.Node
                    || (
                        el.Type == OsmGeoType.Way
                        && (el.Tags?.ContainsKey(TAG_HIGHWAY_ATTR_VALUE) ?? false)
                    )
                )
                .ToComplete();

            var ways = all
                .Where(osm => osm.Type == OsmGeoType.Way)
                .Select(way => (CompleteWay)way)
                .ToList();

            return ways;
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

        private static float MetersPerLonDegree(float lat)
            => 40075000 * (float)Math.Cos(ToRadians(lat)) / 360;

        private static float ToRadians(float angleInDegrees)
            => (float)(angleInDegrees * Math.PI) / 180;
    }
}
