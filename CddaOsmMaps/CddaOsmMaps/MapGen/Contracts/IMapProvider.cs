using CddaOsmMaps.MapGen.Entities;
using System.Collections.Generic;

namespace CddaOsmMaps.MapGen.Contracts
{
    internal interface IMapProvider
    {
        float PixelsPerMeter { get; }
        (int width, int height) MapSize { get; }

        List<Road> GetRoads();
    }
}
