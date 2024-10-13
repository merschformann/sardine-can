using System.Collections.Generic;
using SC.ObjectModel.IO.Json;
using System.Text.Json.Serialization;
using SC.ObjectModel;
using SC.ObjectModel.Configuration;
using Swashbuckle.AspNetCore.Filters;

namespace SC.Service.Elements.IO
{
    public class JsonJob : JsonCalculation
    {
        [JsonPropertyName("priority")] public int Priority { get; set; } = 5;
    }

    public class JsonJobExample : IExamplesProvider<JsonJob>
    {
        public JsonJob GetExamples()
        {
            // Return a simple example with minimal overload to get users started easily
            return new JsonJob()
            {
                Instance = new JsonInstance()
                {
                    Name = "example",
                    Containers = new List<JsonContainer> { new() { ID = 0, Length = 10, Width = 5, Height = 3, MaxWeight = 10 } },
                    Pieces = new List<JsonPiece>
                    {
                        new() { ID = 0, Weight = 1, Cubes = new List<JsonCube> { new () {Length = 6, Width = 3, Height = 2 } }},
                        new() { ID = 1, Weight = 2, Cubes = new List<JsonCube> { new () {Length = 4, Width = 2, Height = 1 } }},
                        new() { ID = 2, Weight = 3, Cubes = new List<JsonCube> { new () {Length = 4, Width = 2, Height = 1 } }},
                    },
                }
            };
        }
    }
}
