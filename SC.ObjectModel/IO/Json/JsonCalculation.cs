using System.Text.Json.Serialization;

namespace SC.ObjectModel.IO.Json
{
    public class JsonCalculation
    {
        [JsonPropertyName("configuration")] public Configuration.Configuration Configuration { get; set; }
        [JsonPropertyName("instance")] public JsonInstance Instance { get; set; }
    }
}
