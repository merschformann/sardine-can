using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SC.ObjectModel.IO.Json
{
    /// <summary>
    /// JSON representation of an assignment.
    /// </summary>
    public class JsonAssignment
    {
        [JsonPropertyName("piece")]
        public int Piece { get; set; }
        [JsonPropertyName("position")]
        public JsonPosition Position { get; set; }
        [JsonPropertyName("cubes")]
        public List<JsonCube> Cubes { get; set; }
    }
}
