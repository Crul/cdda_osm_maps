namespace CddaOsmMaps.MapGen.Entities
{
    internal class OvermapRoad
    {
        public bool HasNorth { get; set; }
        public bool HasSouth { get; set; }
        public bool HasEast { get; set; }
        public bool HasWest { get; set; }

        public OvermapTerrainType? GetOvermapTerrainType()
        {
            var hash = (
                (HasNorth ? 1 : 0)
                + (HasSouth ? 2 : 0)
                + (HasEast ? 4 : 0)
                + (HasWest ? 8 : 0)
            );

            return hash switch
            {
                0 => null,
                1 => OvermapTerrainType.RoadNorth,
                2 => OvermapTerrainType.RoadSouth,
                3 => OvermapTerrainType.RoadNorthSouth,
                4 => OvermapTerrainType.RoadEast,
                5 => OvermapTerrainType.RoadNorthEast,
                6 => OvermapTerrainType.RoadEastSouth,
                7 => OvermapTerrainType.RoadNorthEastSouth,
                8 => OvermapTerrainType.RoadWest,
                9 => OvermapTerrainType.RoadWestNorth,
                10 => OvermapTerrainType.RoadSouthWest,
                11 => OvermapTerrainType.RoadNorthSouthWest,
                12 => OvermapTerrainType.RoadEastWest,
                13 => OvermapTerrainType.RoadNorthEastWest,
                14 => OvermapTerrainType.RoadEastSouthWest,
                15 => OvermapTerrainType.RoadNorthEastSouthWest,
                _ => null,
            };
        }
    }
}
