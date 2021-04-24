using CddaOsmMaps.MapGen.Entities;
using System.Collections.Generic;

namespace CddaOsmMaps.MapGen.Dtos
{
    internal class MapElements
    {
        public List<LandArea> LandAreas { get; set; }
        public List<River> Rivers { get; set; }
        public List<Road> Roads { get; set; }
        public List<Building> Buildings { get; set; }
    }
}
