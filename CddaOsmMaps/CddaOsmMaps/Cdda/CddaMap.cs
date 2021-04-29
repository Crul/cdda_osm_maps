namespace CddaOsmMaps.Cdda
{
    internal static class CddaMap
    {
        public const int SUBMAP_SIZE = 12;
        public const int OVERMAP_TILE_SIZE = SUBMAP_SIZE * 2;
        public const int SEGMENT_SIZE_IN_OVERMAP_TILES = 32;
        public const int SEGMENT_SIZE = SEGMENT_SIZE_IN_OVERMAP_TILES * OVERMAP_TILE_SIZE;

        public const int OVERMAP_REGION_SIZE_IN_OVERMAP_TILES = 180;
        public const int REGION_SIZE_IN_SUBMAP_TILES = OVERMAP_REGION_SIZE_IN_OVERMAP_TILES * 2;
        public const int OVERMAP_REGION_SIZE = OVERMAP_REGION_SIZE_IN_OVERMAP_TILES * OVERMAP_TILE_SIZE;

        public const int REALITY_BUBBLE_RADIUS = 60;
    }
}
