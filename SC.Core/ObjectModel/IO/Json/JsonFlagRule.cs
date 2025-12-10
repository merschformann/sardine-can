using SC.Core.ObjectModel.Rules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SC.Core.ObjectModel.IO.Json
{
    /// <summary>
    /// JSON representation of a flag rule.
    /// </summary>
    public class JsonFlagRule
    {
        [JsonPropertyName("flagId")]
        public int FlagId { get; set; }
        [JsonPropertyName("ruleType")]
        public FlagRuleType RuleType { get; set; }
        [JsonPropertyName("parameter")]
        public int Parameter { get; set; }
    }
}
