using SC.ObjectModel.Configuration;
using SC.ObjectModel.IO.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SC.Service.Elements.IO
{
    public class JsonCalculation
    {
        [JsonPropertyName("priority")]
        public int Priority { get; set; } = 5;
        [JsonPropertyName("configuration")]
        public Configuration Configuration { get; set; }
        [JsonPropertyName("instance")]
        public JsonInstance Instance { get; set; }
    }
}
