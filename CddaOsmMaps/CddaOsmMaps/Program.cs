using CddaOsmMaps.Args;
using CddaOsmMaps.Cdda;
using CddaOsmMaps.MapGen;
using CddaOsmMaps.Osm;
using System.Drawing;

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
            mapGen.Generate(imgPath: args.ImageFilePath, args.Verbose);

            if (string.IsNullOrEmpty(args.SaveGame))
                return;

            var cddaGenerator = new CddaGenerator(
                mapGen,
                args.CddaPath,
                args.SaveGame,
                args.Verbose
            );
            var spawnPoint = GetSpawnPoint(args, osmReader);
            cddaGenerator.Generate(spawnPoint);
        }

        private static Point? GetSpawnPoint(ParsedArgs args, OsmReader osmReader)
        {
            var spawnPoint = args.GetSpawnPoint();
            if (!spawnPoint.HasValue)
                return null;

            spawnPoint = osmReader.LatLonToXY(spawnPoint.Value);

            return new Point(
                // reversed x <-> and y = height - y
                (int)spawnPoint.Value.Y,
                (int)(osmReader.MapSize.Height - spawnPoint.Value.X)
            );
        }
    }
}
