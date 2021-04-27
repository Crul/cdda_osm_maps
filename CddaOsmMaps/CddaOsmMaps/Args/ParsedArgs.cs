using CddaOsmMaps.Crosscutting;
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
        public float[] SpawnPoint { get; set; }
        public float PixelsPerMeter { get; set; }
        public string ImageFilePath { get; set; }
        public bool Verbose { get; set; }

        private static readonly int[] VALID_GIS_BOUND_COUNTS = new int[] { 0, 4 };
        private static readonly int[] VALID_SPAWN_POINT_COUNTS = new int[] { 0, 2 };

        public bool Validate()
        {
            var isValid = true;
            var errorMessages = new List<string>();

            isValid &= CheckArrayValueCount(
                errorMessages,
                VALID_GIS_BOUND_COUNTS,
                GisBounds,
                "Gis Bound box needs exactly 0 or 4 values"
            );

            isValid &= CheckArrayValueCount(
                errorMessages,
                VALID_SPAWN_POINT_COUNTS,
                SpawnPoint,
                "Player position needs exactly 0 or 2 values"
            );

            if (string.IsNullOrEmpty(SaveGame + ImageFilePath))
            {
                errorMessages.Add("ERROR: CDDA save game name or image filepath must be provided.");
                isValid = false;
            }

            LogErrors(errorMessages);

            return isValid;
        }

        public PointFloat GetSpawnPoint()
            => SpawnPoint?.Length == 2
                ? new PointFloat(SpawnPoint[0], SpawnPoint[1])
                : null;

        public Bounds GetBounds()
            => GisBounds == null
                ? null
                : new Bounds
                {
                    MinLatitude = GisBounds[0],
                    MinLongitude = GisBounds[1],
                    MaxLatitude = GisBounds[2],
                    MaxLongitude = GisBounds[3]
                };

        private static bool CheckArrayValueCount(
            List<string> errorMessages,
            int[] VALID_VALUES_COUNTS,
            float[] array,
            string mainErrorMessage
        )
        {
            if (array == null)
                return (VALID_VALUES_COUNTS.Contains(0));

            var arrayValueCount = array.Length;
            if (VALID_VALUES_COUNTS.Contains(arrayValueCount))
                return true;

            errorMessages.Add(
                $"ERROR: {mainErrorMessage}.{Environment.NewLine}"
                + $"       {arrayValueCount} values found: {string.Join(", ", array)}"
            );

            return false;
        }

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
