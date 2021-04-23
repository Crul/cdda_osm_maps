using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace CddaOsmMaps.Crosscutting
{
    internal static class JsonIO
    {
        private static JsonSerializerOptions jso = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static T ReadJson<T>(string filepath, int skipLines = 0)
        {
            if (skipLines == 0)
                return JsonSerializer.Deserialize<T>(filepath);

            var lines = File.ReadAllLines(filepath);
            var jsonText = string.Join(string.Empty, lines.Skip(1));

            return JsonSerializer.Deserialize<T>(jsonText);
        }

        public static void WriteJson<T>(string filepath, T obj, string header = "")
        {
            var headerBytes = string.IsNullOrEmpty(header)
               ? Array.Empty<byte>()
               : Encoding.UTF8.GetBytes(header);

            var data = headerBytes
                .Concat(JsonSerializer.SerializeToUtf8Bytes(obj, jso))
                .ToArray();

            File.WriteAllBytes(filepath, data);
        }
    }
}
