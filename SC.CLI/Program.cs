using CommandLine;
using SC.ObjectModel;
using SC.ObjectModel.IO;
using System;
using System.IO;
using SC.ExecutionHandler;
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
            // Read input file (either from file or from stdin)
            var instance = JsonIO.From<JsonCalculation>(string.IsNullOrWhiteSpace(opts.Input) ? Console.In.ReadToEnd() : File.ReadAllText(opts.Input));

            // >> Run calculation
            Action<string> logger = string.IsNullOrWhiteSpace(opts.Output) ? null : Console.Write;
            instance.Configuration ??= new Configuration(MethodType.ExtremePointInsertion, false);
            var result = Executor.Execute(Instance.FromJsonInstance(instance.Instance), instance.Configuration, logger);

            // Output result
            if (string.IsNullOrWhiteSpace(opts.Output))
                Console.WriteLine(JsonIO.To(result.Solution.ToJsonSolution()));
            else
                File.WriteAllText(opts.Output, JsonIO.To(result.Solution.ToJsonSolution()));
        }
    }
}
