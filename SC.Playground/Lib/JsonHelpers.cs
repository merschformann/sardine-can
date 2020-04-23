using SC.ObjectModel;
using SC.ObjectModel.Configuration;
using SC.ObjectModel.Elements;
using SC.ObjectModel.IO;
using SC.ObjectModel.Rules;
using SC.Service.Elements.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SC.Playground.Lib
{
    internal class JsonHelpers
    {
        public static void CreateJson()
        {
            // Init randomizer
            Random rand = new Random(0);
            // Generate a simple instance
            Instance instance = new Instance() { Name = "testinstance" };
            instance.Containers = new List<Container> { new Container() { ID = 0, Mesh = new MeshCube() { Length = 600, Width = 400, Height = 300 } } };
            instance.Pieces = new List<VariablePiece>();
            for (int i = 0; i < 10; i++)
            {
                var piece = new VariablePiece() { ID = i };
                piece.AddComponent(0, 0, 0, rand.Next(50, 400), rand.Next(50, 400), rand.Next(50, 400));
                piece.SetFlags(new (int, int)[] { (0, rand.Next(0, 2)) });
                piece.Seal();
                instance.Pieces.Add(piece);
            }
            instance.Rules.FlagRules.Add(new FlagRule() { FlagId = 0, RuleType = FlagRuleType.Disjoint, Parameter = 0 });
            // Simplify instance
            var jsonInstance = instance.ToJsonInstance();
            // Generate configuration
            Configuration config = new Configuration();
            JsonCalculation calculation = new JsonCalculation()
            {
                Instance = jsonInstance,
                Configuration = config,
                Priority = 3
            };
            // Serialize
            string json = JsonIO.To(calculation);
            // Write JSON to disk
            File.WriteAllText("calculation.json", json);
        }

        public static void ParseJson()
        {
            // Read JSON file from disk
            var calculation = JsonIO.From<JsonCalculation>(File.ReadAllText("calculation.json"));
            Console.WriteLine($"Instance \"{calculation.Instance.Name}\" parsed from JSON successfully!");
        }
    }
}
