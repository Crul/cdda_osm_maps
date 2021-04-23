namespace CddaOsmMaps.MapGen.Contracts
{
    internal interface IMapGenerator
    {
        (int width, int height) MapSize { get; }

        bool IsRoad((int x, int y) pixelPos);
    }
}
