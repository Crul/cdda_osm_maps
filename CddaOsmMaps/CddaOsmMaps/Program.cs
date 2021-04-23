using CddaOsmMaps.Cdda;
using CddaOsmMaps.Crosscutting;
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
            var image = ParseOsm();
            GenerateCdda(image);
        }

        private static ImageBuilder ParseOsm()
        {
            var osmParser = new OsmReader(OSM_XML_FILEPATH);
            var image = new ImageBuilder(osmParser.OutputSize);
            osmParser.DrawWays(image);
            image.Save(ROAD_IGM_FILEPATH);
            image.DisposeBuldingProperties();

            return image;
        }

        private static void GenerateCdda(ImageBuilder image)
        {
            var cddaGenerator = new CddaGenerator(CDDA_FOLDER, SAVEGAME);
            cddaGenerator.Generate(image);
        }
    }
}
