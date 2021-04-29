namespace CddaOsmMaps.Cdda
{
    internal class CddaMap
    {
        public const int SUBMAP_SIZE = 12;
        public const int OVERMAP_TILE_SIZE = SUBMAP_SIZE * 2;
        public const int OVERMAP_TILES_PER_SEGMENT = 32;
        public const int SEGMENT_SIZE = OVERMAP_TILES_PER_SEGMENT * OVERMAP_TILE_SIZE;

        private const int OVERMAP_TILES_PER_REGION = 180;
        public const int OVERMAP_REGION_SIZE = OVERMAP_TILES_PER_REGION * OVERMAP_TILE_SIZE;
        public const int SUBMAP_TILES_PER_REGION = OVERMAP_TILES_PER_REGION * 2;

        public const int REALITY_BUBBLE_RADIUS = 60;
    }
}
