using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;

namespace CddaOsmMaps.Crosscutting
{
    internal static class JsonIO
    {
        public static JObject ReadJson(string filepath, int skipLines = 0)
        {
            var lines = File.ReadAllLines(filepath);
            var jsonText = string.Join(string.Empty, lines.Skip(skipLines));
            var mainSaveData = JObject.Parse(jsonText);

            return mainSaveData;
        }

        public static void WriteJson<T>(string filepath, T obj, string header = "")
            => File.WriteAllText(filepath, header + JsonConvert.SerializeObject(obj));
    }
}
