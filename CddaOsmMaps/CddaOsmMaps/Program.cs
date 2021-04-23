using CddaOsmMaps.Cdda;
using CddaOsmMaps.MapGen;
using CddaOsmMaps.Osm;

namespace CddaOsmMaps
{
    class Program
    {
        private const string OSM_XML_FILEPATH =
            @"C:\workspace\Games\CDDA.crul\cdda_osm_maps\.doc-osm - saint-johnsbury.osm";

        private const string ROAD_IGM_FILEPATH = "";

        private const string CDDA_FOLDER = @"C:\workspace\Games\CDDA Game Launcher\cdda";
        private const string SAVEGAME = "Empty World";

        static void Main(string[] args)
        {
            var osmParser = new OsmReader(OSM_XML_FILEPATH);

            var mapGen = new MapGenerator(osmParser);
            mapGen.Generate(imgPath: ROAD_IGM_FILEPATH);

            var cddaGenerator = new CddaGenerator(mapGen, CDDA_FOLDER, SAVEGAME);
            cddaGenerator.Generate();
        }
    }
}