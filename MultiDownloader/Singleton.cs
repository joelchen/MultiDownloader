using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace MultiDownloader
{
    /// <summary>
    /// Singleton for shared configurations.
    /// </summary>
    class Singleton
    {
        private static readonly JObject settingsObject = JObject.Parse(File.ReadAllText(@"Settings.json"));
        private static readonly JsonSerializer serializer = new JsonSerializer();
        public static Settings Settings { get; } = serializer.Deserialize<Settings>(settingsObject.CreateReader());
    }
}
