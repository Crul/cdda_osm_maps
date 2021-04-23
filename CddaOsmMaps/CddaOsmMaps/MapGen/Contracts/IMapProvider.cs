using CddaOsmMaps.MapGen.Dtos;

namespace CddaOsmMaps.MapGen.Contracts
{
    internal interface IMapProvider
    {
        float PixelsPerMeter { get; }
        (int width, int height) MapSize { get; }

        MapElements GetMapElements();
    }
}
