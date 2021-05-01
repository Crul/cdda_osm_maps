using CddaOsmMaps.Cdda;
using CddaOsmMaps.MapGen.Entities;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CddaOsmMaps.MapGen
{
    partial class MapGenerator
    {
        private void GenerateBuildings(List<Building> buildings)
        {
            buildings.ForEach(GenerateBuilding);
            OvermapImage.CacheBitmap();

            for (int x = 0; x < OvermapSize.Width; x++)
                for (int y = 0; y < OvermapSize.Height; y++)
                {
                    if (Overmap[x, y] != OvermapTerrainType.Default)
                        continue;

                    var pixelColor = OvermapImage.GetPixelColor(new Point(x, y));
                    if (pixelColor == MapColors.FLOOR_COLOR)
                        Overmap[x, y] = OvermapTerrainType.HouseDefault;
                }
        }

        private void GenerateBuilding(Building building)
        {
            MapImage.DrawComplexArea(building.Polygons, MapColors.FLOOR_COLOR);
            MapImage.DrawComplexPath(building.Polygons, MapColors.WALL_COLOR, Building.WALL_WIDTH);

            var overmapPolygons = building
                .Polygons
                .Select(polygon =>
                    new Polygon(polygon.Select(point => point / CddaMap.OVERMAP_TILE_SIZE))
                )
                .ToList();

            OvermapImage.DrawComplexArea(overmapPolygons, MapColors.FLOOR_COLOR);
        }
    }
}
