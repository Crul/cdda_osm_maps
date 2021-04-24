using System.Collections.Generic;

namespace CddaOsmMaps.MapGen.Entities
{
    internal class River : MapElement
    {
        public float Width { get; private set; }
        public River(string type, List<(float x, float y)> path)
            : base(type, path)
        {
            Width = RIVER_TYPE_WIDTHS.ContainsKey(Type)
                ? RIVER_TYPE_WIDTHS[Type]
                : DEFAULT_RIVER_TYPE_WIDTH;
        }

        private const float DEFAULT_RIVER_TYPE_WIDTH = 4;

        private static readonly Dictionary<string, float> RIVER_TYPE_WIDTHS = new Dictionary<string, float>()
        {
            // https://wiki.openstreetmap.org/wiki/Key:waterway
            { "river",         50 },
            { "riverbank",      5 },
            { "stream",         8 },
            { "tidal_channel",  6 },
            { "wadi",           3 }, // TODO dirt ?
            { "drystream",      2 }, // TODO dirt
            { "canal",          6 },
            { "pressurised",    2 }, // TODO pipe
            { "ditch",          3 },
            { "drain",          2 },
            { "fairway",        3 },
            { "fish_pass",      2 },
            { "dock",           0 }, // TODO not water
            { "boatyard",       0 }, // TODO not water
        };
    }
}
