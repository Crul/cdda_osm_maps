using CddaOsmMaps.MapGen.Entities;
using System.Drawing;

namespace CddaOsmMaps.MapGen.Contracts
{
    internal interface IMapGenerator
    {
        Size OvermapSize { get; }
        Size MapSize { get; }

        TerrainType GetTerrain(Point pixelPos);
    }
}
