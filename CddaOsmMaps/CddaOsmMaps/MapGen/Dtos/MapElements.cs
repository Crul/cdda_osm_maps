using CddaOsmMaps.MapGen.Entities;
using System.Collections.Generic;

namespace CddaOsmMaps.MapGen.Dtos
{
    internal class MapElements
    {
        public IEnumerable<Coastline> Coastlines { get; set; }
        public IEnumerable<LandArea> LandAreas { get; set; }
        public IEnumerable<River> Rivers { get; set; }
        public IEnumerable<Road> Roads { get; set; }
        public IEnumerable<Building> Buildings { get; set; }
    }
}
