using CddaOsmMaps.Args;
using CddaOsmMaps.Cdda;
using CddaOsmMaps.MapGen;
using CddaOsmMaps.Osm;

namespace CddaOsmMaps
{
    class Program
    {
        static int Main(string[] args)
            => ArgsParser.ParseAndRun(args, Run);

        static void Run(ParsedArgs args)
        {
            if (!args.Validate())
                return;

            var osmParser = new OsmReader(args.OsmFilepath, args.GetBounds(), args.PixelsPerMeter);
            var mapGen = new MapGenerator(osmParser);
            mapGen.Generate(imgPath: args.ImageFilePath);

            var cddaGenerator = new CddaGenerator(mapGen, args.CddaPath, args.SaveGame);
            cddaGenerator.Generate();
        }
    }
}