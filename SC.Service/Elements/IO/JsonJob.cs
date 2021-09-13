using SC.ObjectModel.IO.Json;
using System.Text.Json.Serialization;

namespace SC.Service.Elements.IO
{
    public class JsonJob : JsonCalculation
    {
        [JsonPropertyName("priority")]
        public int Priority { get; set; } = 5;
    }
}
