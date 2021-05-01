namespace CddaOsmMaps.MapGen.Entities
{
    internal class OvermapRoad
    {
        public bool HasNorth { get; set; }
        public bool HasSouth { get; set; }
        public bool HasEast { get; set; }
        public bool HasWest { get; set; }

        public OvermapTerrainType? GetOvermapRoadTerrainType()
            => GetHash() switch
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

        public OvermapTerrainType? GetOvermapForestTrailTerrainType()
            => GetHash() switch
            {
                0 => null,
                1 => OvermapTerrainType.ForestTrailNorth,
                2 => OvermapTerrainType.ForestTrailSouth,
                3 => OvermapTerrainType.ForestTrailNorthSouth,
                4 => OvermapTerrainType.ForestTrailEast,
                5 => OvermapTerrainType.ForestTrailNorthEast,
                6 => OvermapTerrainType.ForestTrailEastSouth,
                7 => OvermapTerrainType.ForestTrailNorthEastSouth,
                8 => OvermapTerrainType.ForestTrailWest,
                9 => OvermapTerrainType.ForestTrailWestNorth,
                10 => OvermapTerrainType.ForestTrailSouthWest,
                11 => OvermapTerrainType.ForestTrailNorthSouthWest,
                12 => OvermapTerrainType.ForestTrailEastWest,
                13 => OvermapTerrainType.ForestTrailNorthEastWest,
                14 => OvermapTerrainType.ForestTrailEastSouthWest,
                15 => OvermapTerrainType.ForestTrailNorthEastSouthWest,
                _ => null,
            };

        private int GetHash()
            => (
                (HasNorth ? 1 : 0)
                + (HasSouth ? 2 : 0)
                + (HasEast ? 4 : 0)
                + (HasWest ? 8 : 0)
            );
    }
}
