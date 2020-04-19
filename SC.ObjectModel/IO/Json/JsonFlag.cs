using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SC.ObjectModel.IO.Json
{
    public class JsonFlag
    {
        [JsonPropertyName("flagId")]
        public int FlagId { get; set; }
        [JsonPropertyName("flagValue")]
        public int FlagValue { get; set; }
    }
}
