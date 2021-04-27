using CddaOsmMaps.Args;
using CddaOsmMaps.Cdda;
using CddaOsmMaps.Crosscutting;
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

            var osmReader = new OsmReader(
                args.OsmFilepath,
                args.GetBounds(),
                args.PixelsPerMeter,
                args.Verbose
            );

            var mapGen = new MapGenerator(osmReader);
            mapGen.Generate(imgPath: args.ImageFilePath);

            var cddaGenerator = new CddaGenerator(mapGen, args.CddaPath, args.SaveGame);
            var spawnPoint = GetSpawnPoint(args, osmReader);
            cddaGenerator.Generate(spawnPoint);
        }

        private static PointFloat GetSpawnPoint(ParsedArgs args, OsmReader osmReader)
        {
            var spawnPoint = args.GetSpawnPoint();
            if (spawnPoint != null)
            {
                spawnPoint = osmReader.Scale(spawnPoint);
                // reversed x <-> and y = height - y
                (spawnPoint.X, spawnPoint.Y) =
                (
                    spawnPoint.Y,
                    osmReader.MapSize.height - spawnPoint.X
                );
            }

            return spawnPoint;
        }
    }
}
