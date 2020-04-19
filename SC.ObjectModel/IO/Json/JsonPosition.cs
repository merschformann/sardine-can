using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SC.ObjectModel.IO.Json
{
    /// <summary>
    /// JSON representation of a piece position and orientation.
    /// </summary>
    public class JsonPosition
    {
        [JsonPropertyName("x")]
        public double X { get; set; }
        [JsonPropertyName("y")]
        public double Y { get; set; }
        [JsonPropertyName("z")]
        public double Z { get; set; }
        [JsonPropertyName("a")]
        public double A { get; set; }
        [JsonPropertyName("b")]
        public double B { get; set; }
        [JsonPropertyName("c")]
        public double C { get; set; }
    }
}
