using CddaOsmMaps.MapGen.Entities;
using System.Collections.Generic;

namespace CddaOsmMaps.MapGen.Dtos
{
    internal class MapElements
    {
        public List<Road> Roads { get; set; }
        public List<Building> Buildings { get; set; }
    }
}
