using CddaOsmMaps.Crosscutting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CddaOsmMaps.MapGen.Entities
{
    internal class Road : TypedMapElement
    {
        public float Width { get; private set; }
        public float SidewalkWidth { get; private set; }
        public bool HasSidewalk { get; private set; }
        public bool IsTunnel { get; private set; }
        public bool IsBridge { get; private set; }

        private const float SIDEWALK_WIDTH_FACTOR = 1.5f;

        private static readonly string[] ROAD_TYPES_WITH_SIDEWALK = new string[]
        {
            // https://wiki.openstreetmap.org/wiki/Key:highway
            // TODO <tag k="footway" v="sidewalk"/>
            // TODO <tag k="sidewalk" v="both | right | left | no"/>
            "secondary", "tertiary", "residential",
            "living_street", "road", "rest_area", "service"
        };

        // TODO check all ROAD_TYPES_FOR_OVERMAP have MapColors.ROAD_COLORS == PAVEMENT_COLOR
        public static readonly string[] ROAD_TYPES_FOR_OVERMAP = new string[]
        {
            "motorway", "motorway_link",
            "trunk", "trunk_link",
            "primary", "secondary", "tertiary",
            "residential", "living_street"
        };

        // TODO check all FOREST_TRAIL_TYPES_FOR_OVERMAP have MapColors.ROAD_COLORS == DIRT_FLOOR_COLOR
        public static readonly string[] FOREST_TRAIL_TYPES_FOR_OVERMAP = new string[]
        {
            "track", /*"footway", */ "path", "cycleway", "construction"
        };

        public Road(List<Polygon> polygons, string type, bool isTunnel, bool isBridge)
            : base(polygons, type)
        {
            Width = ROAD_TYPE_WIDTHS.ContainsKey(Type)
                ? ROAD_TYPE_WIDTHS[Type]
                : DEFAULT_ROAD_TYPE_WIDTH;

            SidewalkWidth = Width * SIDEWALK_WIDTH_FACTOR;
            HasSidewalk = ROAD_TYPES_WITH_SIDEWALK.Contains(type);
            IsTunnel = isTunnel; // TODO not used
            IsBridge = isBridge; // TODO not used
        }

        private const float DEFAULT_ROAD_TYPE_WIDTH = 8;

        private static readonly Dictionary<string, float> ROAD_TYPE_WIDTHS = new Dictionary<string, float>()
        {
            // https://wiki.openstreetmap.org/wiki/Key:highway
            { "motorway",       14 },
            { "motorway_link",  12 },
            { "trunk",          12 },
            { "trunk_link",     10 },
            { "primary",         9 },
            { "secondary",       7 },
            { "tertiary",        6 },
            { "tertiary_link",   4 },
            { "unclassified",    6 },
            { "residential",     6 },
            { "living_street",   6 },
            { "service",         5 },
            { "construction",    5 },
            { "track",           4 },
            { "pedestrian",      4 },
            { "cycleway",        4 },
            { "path",            4 },
            { "footway",         3 },
            { "steps",           2 },
        };

        public List<float[]> GetRoadWidths(float pixelsPerMeter)
            => Polygons
                .Select(p => GetPolygonWidths(p, Width, pixelsPerMeter))
                .ToList();

        public List<float[]> GetSidewalkWidths(float pixelsPerMeter)
            => Polygons
                .Select(p => GetPolygonWidths(p, SidewalkWidth, pixelsPerMeter))
                .ToList();

        private static float[] GetPolygonWidths(Polygon polygon, float baseWidth, float pixelsPerMeter)
        {
            var widths = new float[polygon.Count - 1];
            for (var i = 0; i < polygon.Count - 1; i++)
            {
                // because CDDA vehicle diagonal movment, diagonal roads need to be wider

                var segment = polygon.Skip(i).Take(2).ToList();
                var angle = Geom.GetAngle(segment[0], segment[1]);
                // https://www.wolframalpha.com/input/?i=plot+1%2B+%28%28sqrt%282%29-1%29+%281-cos%284*x%29%29%2F2%29+from+x+%3D+0+to+2*pi
                var angleFactor = 1f + (float)((Math.Sqrt(2) - 1) * (1 - Math.Cos(4 * angle)) / 2);

                widths[i] = pixelsPerMeter * baseWidth * angleFactor;
            }

            return widths;
        }
    }
}
