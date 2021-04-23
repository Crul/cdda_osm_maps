using CddaOsmMaps.MapGen.Entities;

namespace CddaOsmMaps.MapGen.Contracts
{
    internal interface IMapGenerator
    {
        (int width, int height) MapSize { get; }

        TerrainType GetTerrain((int x, int y) pixelPos);
    }
}
