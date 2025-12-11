using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SC.Core.ObjectModel.IO.Json
{
    public class JsonRuleSet
    {
        [JsonPropertyName("flagRules")]
        public List<JsonFlagRule> FlagRules { get; set; } = new List<JsonFlagRule>();
    }
}
