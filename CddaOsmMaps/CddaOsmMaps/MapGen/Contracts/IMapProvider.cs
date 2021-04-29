using CddaOsmMaps.MapGen.Dtos;
using System.Drawing;

namespace CddaOsmMaps.MapGen.Contracts
{
    internal interface IMapProvider
    {
        float PixelsPerMeter { get; }
        Size MapSize { get; }

        MapElements GetMapElements();
    }
}
