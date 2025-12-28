using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SC.Core.ObjectModel.IO.Json
{
    /// <summary>
    /// JSON representation of a piece not placed in a container.
    /// </summary>
    public class JsonOffload
    {
        [JsonPropertyName("piece")]
        public int Piece { get; set; }
        [JsonPropertyName("cubes")]
        public List<JsonCube> Cubes { get; set; }
        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public JsonElement Data { get; set; }
    }
}
