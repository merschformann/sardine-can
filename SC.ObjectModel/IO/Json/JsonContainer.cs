using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SC.ObjectModel.IO.Json
{
    /// <summary>
    /// JSON representation of container.
    /// </summary>
    public class JsonContainer
    {
        [JsonPropertyName("id")]
        public int ID { get; set; }
        [JsonPropertyName("length")]
        public double Length { get; set; }
        [JsonPropertyName("width")]
        public double Width { get; set; }
        [JsonPropertyName("height")]
        public double Height { get; set; }
        [JsonPropertyName("maxWeight")]
        public double MaxWeight { get; set; } = double.PositiveInfinity;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public JsonElement Data { get; set; }
    }
}
