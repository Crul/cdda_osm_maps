using OsmSharp.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CddaOsmMaps.Args
{
    internal class ParsedArgs
    {
        public string CddaPath { get; set; }
        public string SaveGame { get; set; }
        public string OsmFilepath { get; set; }
        public float[] GisBounds { get; set; }
        public float PixelsPerMeter { get; set; }
        public string ImageFilePath { get; set; }
        public bool Verbose { get; set; }

        private static readonly int[] VALID_GIS_BOUND_COUNTS = new int[] { 0, 4 };

        public bool Validate()
        {
            var isValid = true;
            var errorMessages = new List<string>();

            var boundsValueCount = GisBounds.Length;
            if (!VALID_GIS_BOUND_COUNTS.Contains(boundsValueCount))
            {
                errorMessages.Add(
                    $"ERROR: Gis Bound box needs exactly 4 values.{Environment.NewLine}"
                    + $"       {boundsValueCount} values found: {string.Join(", ", GisBounds)}"
                );
                isValid = false;
            }

            if (string.IsNullOrEmpty(SaveGame + ImageFilePath))
            {
                errorMessages.Add("ERROR: CDDA save game name or image filepath must be provided.");
                isValid = false;
            }

            LogErrors(errorMessages);

            return isValid;
        }

        public Bounds GetBounds()
            => new Bounds
            {
                MinLatitude = GisBounds[0],
                MinLongitude = GisBounds[1],
                MaxLatitude = GisBounds[2],
                MaxLongitude = GisBounds[3]
            };

        private static void LogErrors(List<string> errorMessages)
        {
            if (errorMessages.Count > 0)
                Console.WriteLine(
                    Environment.NewLine +
                    string.Join(Environment.NewLine + Environment.NewLine, errorMessages)
                    + Environment.NewLine
                );
        }
    }
}
