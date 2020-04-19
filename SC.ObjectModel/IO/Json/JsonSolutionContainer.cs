using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SC.ObjectModel.IO.Json
{
    public class JsonSolutionContainer
    {
        [JsonPropertyName("assignments")]
        public List<JsonAssignment> Assignments { get; set; }
    }
}
