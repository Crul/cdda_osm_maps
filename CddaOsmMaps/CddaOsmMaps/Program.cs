using CddaOsmMaps.Cdda;
using CddaOsmMaps.MapGen;
using CddaOsmMaps.Osm;
using OsmSharp.API;

namespace CddaOsmMaps
{
    class Program
    {
        private const string OSM_FILEPATH =
            @"C:\workspace\Games\CDDA.crul\cdda_osm_maps\.doc-osm - Boston.osm.pbf";

        private static readonly Bounds OSM_BOUNDS = new Bounds
        {
            // Full Boston
            MinLatitude = 42.30324920325728f,
            MinLongitude = -71.12311647065927f,
            MaxLatitude = 42.42349179160527f,
            MaxLongitude = -70.94733522065927f
        };

        private const string ROAD_IGM_FILEPATH = "";

        private const string CDDA_FOLDER = @"C:\workspace\Games\CDDA Game Launcher\cdda";
        private const string SAVEGAME = "Empty World";

        static void Main(string[] args)
        {
            var osmParser = new OsmReader(OSM_FILEPATH, OSM_BOUNDS);
            var mapGen = new MapGenerator(osmParser);
            mapGen.Generate(imgPath: ROAD_IGM_FILEPATH);

            var cddaGenerator = new CddaGenerator(mapGen, CDDA_FOLDER, SAVEGAME);
            cddaGenerator.Generate();
        }
    }
}