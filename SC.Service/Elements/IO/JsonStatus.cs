using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SC.Service.Elements.IO
{
    public class JsonStatus
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("status")]
        public StatusCodes Status { get; set; } = StatusCodes.Pending;
        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; } = "";
        [JsonPropertyName("statusUrl")]
        public string StatusUrl { get; set; }
        [JsonPropertyName("problemUrl")]
        public string ProblemUrl { get; set; }
        [JsonPropertyName("resultUrl")]
        public string SolutionUrl { get; set; }
    }
}
