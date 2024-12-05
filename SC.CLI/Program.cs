using CommandLine;
using SC.ObjectModel;
using SC.ObjectModel.IO;
using System;
using System.IO;
using SC.CLI;
using SC.ObjectModel.Additionals;
using SC.ObjectModel.Configuration;
using SC.ObjectModel.IO.Json;

namespace SC.CLI
{
    class Options
    {
        [Option('i', "input", Required = false, HelpText = "Input file to process")]
        public string Input { get; set; }

        [Option('o', "output", Required = false, HelpText = "Output file to write to")]
        public string Output { get; set; }
    }

    class Program
    {
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(Execute);
        }

        private static void Execute(Options opts)
        {
            // Read the content fully first (either from file or from stdin)
            var content = string.IsNullOrWhiteSpace(opts.Input) ? Console.In.ReadToEnd() : File.ReadAllText(opts.Input);

            // Try to parse the input as a calculation
            var instance = JsonIO.From<JsonCalculation>(content);

            // If nothing useful was found, try to parse it as an unnested instance (without a configuration)
            if (instance == null || instance.Instance == null && instance.Configuration == null)
            {
                var inst = JsonIO.From<JsonInstance>(content);
                if (inst != null)
                    instance = new JsonCalculation() { Instance = inst };
            }

            // If still nothing useful was found, abort
            if (instance == null || instance.Instance == null)
            {
                Console.WriteLine("No 'instance' found in input file.");
                return;
            }

            // >> Run calculation
            Action<string> logger = string.IsNullOrWhiteSpace(opts.Output) ? null : Console.Write;
            instance.Configuration ??= new Configuration(MethodType.ExtremePointInsertion, true);
            var result = Executor.Execute(Instance.FromJsonInstance(instance.Instance), instance.Configuration, logger);

            // Output result
            if (string.IsNullOrWhiteSpace(opts.Output))
                Console.WriteLine(JsonIO.To(result.Solution.ToJsonSolution()));
            else
                File.WriteAllText(opts.Output, JsonIO.To(result.Solution.ToJsonSolution()));
        }
    }
}
