using SC.Heuristics.PrimalHeuristic;
using SC.ObjectModel;
using SC.ObjectModel.Additionals;
using SC.ObjectModel.Configuration;
using SC.ObjectModel.Interfaces;
using SC.Linear;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SC.Playground.Lib
{
    internal class PlaygroundFunctions
    {
        public static void ExportConfigs()
        {
            foreach (var method in Enum.GetValues(typeof(MethodType)).Cast<MethodType>())
            {
                Configuration config = new Configuration(method, true) { Name = method + "Default" };
                config.TimeLimit = TimeSpan.FromMinutes(5);
                config.Write(config.Name);
            }
        }

        /// <summary>
        /// Exports all models in all formulations from the given directories
        /// </summary>
        public static void ExportMPSModels(string modelDirPath)
        {
            int counter = 0;
            string fileEnding = ".mps";
            string exportDir = "ExportedMPS";
            if (!Directory.Exists(exportDir))
            {
                Directory.CreateDirectory(exportDir);
            }
            foreach (var file in Directory.EnumerateFiles(Path.Combine(Directory.GetCurrentDirectory(), modelDirPath), "*.xinst"))
            {
                string instanceName = Path.GetFileNameWithoutExtension(file);
                Instance instance = Instance.ReadXML(file);
                Configuration configFLB = new Configuration(MethodType.FrontLeftBottomStyle, true) { /* No gravity here */ HandleGravity = false, /* No stackability here */ HandleStackability = false, HandleCompatibility = true, HandleForbiddenOrientations = true, HandleRotatability = true };
                Configuration configTetris = new Configuration(MethodType.TetrisStyle, true) { /* No gravity here */ HandleGravity = false, /* No stackability here */ HandleStackability = false, HandleCompatibility = true, HandleForbiddenOrientations = true, HandleRotatability = true };
                Configuration configHybrid = new Configuration(MethodType.HybridStyle, true) { /* No gravity here */ HandleGravity = false, /* No stackability here */ HandleStackability = false, HandleCompatibility = true, HandleForbiddenOrientations = true, HandleRotatability = true };
                Console.WriteLine("Exporting (" + (++counter) + "/flb) " + Path.GetFileNameWithoutExtension(file));
                LinearModelFLB transFLB = new LinearModelFLB(instance, configFLB);
                transFLB.ExportMPS(Path.Combine(exportDir, instanceName + "-flb" + fileEnding));
                Console.WriteLine("Exporting (" + (++counter) + "/tetris) " + Path.GetFileNameWithoutExtension(file));
                LinearModelTetris transTetris = new LinearModelTetris(instance, configTetris);
                transTetris.ExportMPS(Path.Combine(exportDir, instanceName + "-tetris" + fileEnding));
                Console.WriteLine("Exporting (" + (++counter) + "/hybrid) " + Path.GetFileNameWithoutExtension(file));
                LinearModelHybrid transHybrid = new LinearModelHybrid(instance, configHybrid);
                transHybrid.ExportMPS(Path.Combine(exportDir, instanceName + "-hybrid" + fileEnding));
            }
        }

        /// <summary>
        /// Call handling wrapper for parameter tuning.
        /// </summary>
        /// <param name="args">All parameters for algorithm execution.</param>
        public static void HandleCall(string[] args)
        {
            // Get basic params
            MethodType methodType = (MethodType)Enum.Parse(typeof(MethodType), args[0]);
            string instancePath = args[1];
            int seed = int.Parse(args[5]);
            TimeSpan timeLimit = TimeSpan.FromSeconds(double.Parse(args[3], ExportationConstants.FORMATTER));

            // Transform parameters
            List<string> parameters = new List<string>();
            string name = "";
            for (int i = 0; i < args.Length; i++)
            {
                if (i % 2 == 0)
                {
                    name = args[i].Substring(1);
                }
                else
                {
                    parameters.Add(name + "=" + args[i].Replace('\'', ' ').Trim());
                }
            }

            // Init config
            Configuration config = new Configuration(methodType, true);
            config.Log = (string message) => { Console.Write(message); };
            config.Seed = seed;
            config.TimeLimit = timeLimit;

            // Parse parameter values
            foreach (var parameter in parameters)
            {
                string paramName = parameter.Split('=')[0];
                string paramValue = parameter.Split('=')[1];

                switch (paramName)
                {
                    case "Tetris": config.Tetris = bool.Parse(paramValue); break;
                    case "PieceOrder": config.PieceOrder = (PieceOrderType)Enum.Parse(typeof(PieceOrderType), paramValue); break;
                    case "BestFit": config.BestFit = bool.Parse(paramValue); break;
                    case "MeritType": config.MeritType = (MeritFunctionType)Enum.Parse(typeof(MeritFunctionType), paramValue); break;
                    case "Improvement": config.Improvement = bool.Parse(paramValue); break;
                    case "ScoreBasedOrder": config.ScoreBasedOrder = bool.Parse(paramValue); break;
                    case "RandomSalt": config.RandomSalt = double.Parse(paramValue, ExportationConstants.FORMATTER); break;
                    case "InflateAndReplaceInsertion": config.InflateAndReplaceInsertion = bool.Parse(paramValue); break;
                    case "NormalizationOrder":
                        {
                            config.NormalizationOrder = new DimensionMarker[paramValue.Length];
                            for (int i = 0; i < paramValue.Length; i++)
                            {
                                switch (paramValue[i])
                                {
                                    case 'X':
                                        config.NormalizationOrder[i] = DimensionMarker.X;
                                        break;
                                    case 'Y':
                                        config.NormalizationOrder[i] = DimensionMarker.Y;
                                        break;
                                    case 'Z':
                                        config.NormalizationOrder[i] = DimensionMarker.Z;
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                        break;
                    case "ExhaustiveEPProne": config.ExhaustiveEPProne = bool.Parse(paramValue); break;
                    case "StagnationDistance": config.StagnationDistance = int.Parse(paramValue); break;
                    case "MaximumPercentageOfStoreModification": config.MaximumPercentageOfStoreModification = double.Parse(paramValue, ExportationConstants.FORMATTER); break;
                    case "InitialMaximumPercentageOfStoreModification": config.InitialMaximumPercentageOfStoreModification = double.Parse(paramValue, ExportationConstants.FORMATTER); break;
                    case "PossibleSwaps": config.PossibleSwaps = int.Parse(paramValue); break;
                    case "MaxSwaps": config.MaxSwaps = int.Parse(paramValue); break;
                    case "LongTermScoreReInitDistance": config.LongTermScoreReInitDistance = int.Parse(paramValue); break;
                    case "WorkerThreads": config.WorkerThreads = int.Parse(paramValue); break;
                    case "Instance": instancePath = paramValue; break;
                    case "TimeLimit": config.TimeLimit = TimeSpan.FromSeconds(double.Parse(paramValue, ExportationConstants.FORMATTER)); break;
                    case "PushInsertionVIDs":
                        {
                            config.PushInsertionVIDs = new int[paramValue.Length];
                            for (int i = 0; i < paramValue.Length; i++)
                            {
                                switch (paramValue[i])
                                {
                                    case '1': config.PushInsertionVIDs[i] = 1; break;
                                    case '2': config.PushInsertionVIDs[i] = 2; break;
                                    case '3': config.PushInsertionVIDs[i] = 3; break;
                                    case '4': config.PushInsertionVIDs[i] = 4; break;
                                    case '5': config.PushInsertionVIDs[i] = 5; break;
                                    case '6': config.PushInsertionVIDs[i] = 6; break;
                                    case '7': config.PushInsertionVIDs[i] = 7; break;
                                    case '8': config.PushInsertionVIDs[i] = 8; break;
                                    default: break;
                                }
                            }
                        }
                        break;
                    default:
                        // Skip on not understanding the input
                        break;
                }
            }

            // Prepare execution
            IMethod method = null;
            Instance instance = Instance.ReadXML(instancePath);
            switch (methodType)
            {
                case MethodType.ExtremePointInsertion: method = new ExtremePointInsertionHeuristic(instance, config); break;
                case MethodType.SpaceDefragmentation: method = new SpaceDefragmentationHeuristic(instance, config); break;
                case MethodType.PushInsertion: method = new PushInsertion(instance, config); break;
                case MethodType.ALNS: method = new ALNS(instance, config); break;
                case MethodType.FrontLeftBottomStyle:
                case MethodType.TetrisStyle:
                case MethodType.SpaceIndexed:
                default:
                    throw new ArgumentException("Not supported method type: " + methodType.ToString());
            }

            // Execute the algorithm
            PerformanceResult result = method.Run();

            // Write result for SMAC
            Console.WriteLine();
            Console.WriteLine(
                    "Result for ParamILS: " +
                    "SAT, " +
                    Math.Min(config.TimeLimit.TotalSeconds, result.SolutionTime.TotalSeconds).ToString(ExportationConstants.FORMATTER) + ", " +
                    "0, " +
                    (result.ObjectiveValue * -1).ToString(ExportationConstants.FORMATTER) + ", " +
                    config.Seed);
        }
    }
}
