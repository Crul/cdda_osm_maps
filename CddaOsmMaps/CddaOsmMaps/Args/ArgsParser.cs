using System;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace CddaOsmMaps.Args
{
    internal class ArgsParser
    {
        public static int ParseAndRun(string[] args, Action<ParsedArgs> run)
        {
            var rootCommand = new RootCommand
            {
                new Option<string>(
                    new string[] { "--cdda-path", "-cdda" },
                    description: "Path to Cataclysm DDA folder")
                    { IsRequired = true },

                new Option<string>(
                    new string[] { "--save-game", "-save" },
                    description: "Name of the CDDA world save in which the map will be generated"
                        + $"{Environment.NewLine}WARNING: All content will be deleted"
                        + $"{Environment.NewLine}If not provided only the image will be generated"),

                new Option<string>(
                    new string[] { "--osm-filepath", "-osm" },
                    description: "Path to the OSM file (PBF or XML format)")
                    { IsRequired = true },

                new Option<float[]>(
                    new string[] { "--gis-bounds", "-bounds" },
                    description: "Bounding box for the map, 4 values in this order: "
                        + $"{Environment.NewLine}[MinLatitude], [MinLongitude], [MaxLatitude], [MaxLongitude]"
                        + $"{Environment.NewLine}REQUIRED only if OSM file does not contain <bounds> element"),

                new Option<float[]>(
                    new string[] { "--spawn-point", "-spawn" },
                    description: "Latitude and longitude of player position. Default value: Bounding box center"),

                new Option<float>(
                    new string[] { "--pixels-per-meter", "-ppm" },
                    getDefaultValue: () => 1.2f,
                    description: "Map resolution. One pixel corresponds to one CDDA tile"),

                new Option<string>(
                    new string[] { "--image-filepath", "-img" },
                    description: "Intermediate image (PNG) will be saved to this file"),

                new Option<bool>(
                    new string[] { "--verbose", "-v" },
                    description: "Logs all warning messages"),
            };

            rootCommand.Description = "Generates Cataclysm CDDA maps from OpenStreetMap data (OSM XML and PBF)"
                + $"{Environment.NewLine}"
                + $"{Environment.NewLine}Example:"
                + $"{Environment.NewLine}  CddaOsmMaps.exe ^"
                + $"{Environment.NewLine}    -cdda \"C:\\CDDA Game Launcher\\cdda\" ^"
                + $"{Environment.NewLine}    -save \"Real World\" ^"
                + $"{Environment.NewLine}    -osm \"Boston.osm.pbf\" ^"
                + $"{Environment.NewLine}    -bounds 42.35 -71.06 42.37 -71.02 ^"
                + $"{Environment.NewLine}    -spawn 42.36 -71.04 ^"
                + $"{Environment.NewLine}    -ppm 1.2 ^"
                + $"{Environment.NewLine}    -img bostom-map.png^"
                + $"{Environment.NewLine}    -v";

            rootCommand.Handler = CommandHandler.Create(run);

            var result = rootCommand.InvokeAsync(args).Result;

            return result;
        }
    }
}
