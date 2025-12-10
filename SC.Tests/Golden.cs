using Newtonsoft.Json.Linq;
using FluentAssertions.Json;
using Newtonsoft.Json;
using SC.CLI;
using SC.Core.ObjectModel;
using SC.Core.ObjectModel.Configuration;
using SC.Core.ObjectModel.IO;


namespace SC.Tests;

public class Golden
{
    private static JToken LoadJson(string path) => JToken.Parse(File.ReadAllText(path));

    private static void WriteJson(JToken json, string path) =>
        File.WriteAllText(path, json.ToString(formatting: Formatting.Indented));


    [Theory()]
    [InlineData("data/sample_contained.json")]
    [InlineData("data/sample_offload.json")]
    public void GoldenInputs(string inputFile)
    {
        // Load the input file
        var instance = Instance.ReadJson(File.ReadAllText(inputFile));

        // Run calculation
        var result = Executor.Execute(instance, new Configuration()
        {
            TimeLimitInSeconds = 3,
            ThreadLimit = 1,
            IterationsLimit = 100,
        }, Console.WriteLine);

        var update = Environment.GetEnvironmentVariable("UPDATE") == "1";
        if (update)
        {
            // Just update the golden file
            WriteJson(JToken.Parse(JsonIO.To(result.Solution.ToJsonSolution())), inputFile + ".golden");
        }
        else
        {
            // Compare the actual result with the golden file
            var expectation = LoadJson(inputFile + ".golden");
            var actual = JToken.Parse(JsonIO.To(result.Solution.ToJsonSolution()));
            actual.Should().BeEquivalentTo(expectation);
        }
    }
}
