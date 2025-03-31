using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SC.ObjectModel.IO.Json
{
    /// <summary>
    /// JSON representation of a solution.
    /// </summary>
    public class JsonSolution
    {
        [JsonPropertyName("containers")]
        public List<JsonSolutionContainer> Containers { get; set; }
        [JsonPropertyName("offload")]
        public List<int> Offload { get; set; }
        [JsonPropertyName("data")]
        public JsonElement Data { get; set; }
    }
}
