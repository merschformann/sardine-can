using SC.Core.Heuristics.PrimalHeuristic;
using SC.Core.ObjectModel;
using SC.Core.ObjectModel.Additionals;
using SC.Core.ObjectModel.Configuration;
using SC.Core.ObjectModel.Interfaces;
using SC.Core.Linear;
using System;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;

namespace SC.CLI
{
    /// <summary>
    /// A class used to wrap the methods to enable execution by the CLI
    /// </summary>
    public class Executor
    {
        public static PerformanceResult Execute(Instance instance, Configuration config, Action<string> logger)
        {
            // Prepare logging
            void logLine(string msg) { logger?.Invoke(msg + Environment.NewLine); }
            config.Log = logger;

            // Init method
            IMethod method;
            switch (config.Type)
            {
                case MethodType.FrontLeftBottomStyle: { method = new LinearModelFLB(instance, config); } break;
                case MethodType.TetrisStyle: { method = new LinearModelTetris(instance, config); } break;
                case MethodType.HybridStyle: { method = new LinearModelHybrid(instance, config); } break;
                case MethodType.SpaceIndexed: throw new NotImplementedException("Space indexed model not working for now.");
                case MethodType.ExtremePointInsertion: { method = new ExtremePointInsertionHeuristic(instance, config); } break;
                case MethodType.SpaceDefragmentation: { method = new SpaceDefragmentationHeuristic(instance, config); } break;
                case MethodType.PushInsertion: { method = new PushInsertion(instance, config); } break;
                case MethodType.ALNS: { method = new ALNS(instance, config); } break;
                default: throw new ArgumentException("Unknown method: " + config.Type.ToString());
            }

            // Output some information before starting
            logLine($">>> Welcome to SardineCan");
            logLine($"Initializing ...");
            logLine($"Instance: {instance.Name}");
            logLine($"Config: {config.Name}");
            logLine($"Seed: {config.Seed}");
            logLine($"Config-details: ");
            LogConfigDetails(config, logger);
            logger?.Invoke(Environment.NewLine);

            // Execute
            logLine($"Executing ... ");
            PerformanceResult result = method.Run();

            // Log some information to the console
            logLine($"Finished!");
            logLine($"Result outline:");
            logLine($"Obj: {result.ObjectiveValue.ToString(CultureInfo.InvariantCulture)}");
            logLine($"VolumeContained: {result.Solution.VolumeContained.ToString(CultureInfo.InvariantCulture)}");
            logLine($"VolumeOfContainers: {result.Solution.VolumeOfContainers.ToString(CultureInfo.InvariantCulture)}");
            logLine($"VolumeContainedRelative: {(result.Solution.VolumeContainedRelative * 100).ToString(CultureInfo.InvariantCulture)}%");
            logLine($"VolumeOfContainersInUse: {result.Solution.VolumeOfContainersInUse.ToString(CultureInfo.InvariantCulture)}");
            logLine($"NumberOfContainersInUse: {result.Solution.NumberOfContainersInUse.ToString(CultureInfo.InvariantCulture)}");
            logLine($"NumberOfPiecesPacked: {result.Solution.NumberOfPiecesPacked.ToString(CultureInfo.InvariantCulture)}");
            logLine($"SolutionTime: {result.SolutionTime.TotalSeconds.ToString(CultureInfo.InvariantCulture)}s");

            // We're done here
            return result;
        }

        /// <summary>
        /// Logs detailed information about the used configuration.
        /// </summary>
        /// <param name="config">The config to log details about.</param>
        /// <param name="logger">The logger to use.</param>
        private static void LogConfigDetails(Configuration config, Action<string> logger)
        {
            foreach (var field in config.GetType().GetProperties())
            {
                // Is string like
                string value = "";
                // If the field has a xmlignore attribute: ignore it here too
                if (field.GetCustomAttributes(false).Any(a => a is XmlIgnoreAttribute))
                    continue;
                // See if it already is a string
                if (field.PropertyType == typeof(string))
                {
                    value = (string)field.GetValue(config);
                }
                else
                {
                    // Fetch to-string method - check whether a formatter is necessary
                    var toStringMethod = field.PropertyType.GetMethod("ToString", new[] { typeof(CultureInfo) });
                    if (field.GetValue(config) == null)
                    {
                        value = null;
                    }
                    else
                    {
                        if (toStringMethod != null)
                            value = toStringMethod.Invoke(field.GetValue(config), new object[] { CultureInfo.InvariantCulture })?.ToString();
                        else
                            value = field.GetValue(config)?.ToString();
                    }
                }
                // Output it
                logger?.Invoke(field.Name + ": " + value + Environment.NewLine);
            }
        }
    }
}
