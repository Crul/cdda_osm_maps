using System.Collections.Generic;

namespace CddaOsmMaps.MapGen.Entities
{
    internal class Road : MapElement
    {
        public float Width { get; private set; }

        public Road(string type, List<(float x, float y)> path)
            : base(type, path)
        {
            Width = ROAD_TYPE_WIDTHS.ContainsKey(Type)
                ? ROAD_TYPE_WIDTHS[Type]
                : DEFAULT_ROAD_TYPE_WIDTH;
        }

        private const float DEFAULT_ROAD_TYPE_WIDTH = 8;

        private static readonly Dictionary<string, float> ROAD_TYPE_WIDTHS = new Dictionary<string, float>()
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
    }
}
