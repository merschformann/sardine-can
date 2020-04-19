using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SC.ObjectModel.IO.Json
{
    /// <summary>
    /// JSON representation of instance.
    /// </summary>
    public class JsonInstance
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("containers")]
        public List<JsonContainer> Containers { get; set; }
        [JsonPropertyName("pieces")]
        public List<JsonPiece> Pieces { get; set; }
        [JsonPropertyName("rules")]
        public JsonRuleSet Rules { get; set; }
    }
}