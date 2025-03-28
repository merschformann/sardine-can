using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SC.ObjectModel.IO.Json
{
    /// <summary>
    /// JSON representation of piece.
    /// </summary>
    public class JsonPiece
    {
        [JsonPropertyName("id")]
        public int ID { get; set; }
        [JsonPropertyName("weight")]
        public double Weight { get; set; }
        [JsonPropertyName("flags")]
        public List<JsonFlag> Flags { get; set; }
        [JsonPropertyName("forbidden_orientations")]
        public List<int> ForbiddenOrientations { get; set; }
        [JsonPropertyName("cubes")]
        public List<JsonCube> Cubes { get; set; }
    }
}
