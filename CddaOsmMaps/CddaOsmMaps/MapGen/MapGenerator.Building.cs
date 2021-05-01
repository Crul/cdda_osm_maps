using CddaOsmMaps.MapGen.Entities;
using System.Collections.Generic;

namespace CddaOsmMaps.MapGen
{
    partial class MapGenerator
    {
        private void GenerateBuildings(List<Building> buildings)
            => buildings.ForEach(GenerateBuilding);

        private void GenerateBuilding(Building building)
        {
            MapImage.DrawComplexArea(building.Polygons, MapColors.FLOOR_COLOR);
            MapImage.DrawComplexPath(building.Polygons, MapColors.WALL_COLOR, Building.WALL_WIDTH);

            var overmapPolygons = ToOvermapPolygons(building.Polygons);
            OvermapImage.DrawComplexArea(overmapPolygons, MapColors.FLOOR_COLOR);
        }
    }
}
