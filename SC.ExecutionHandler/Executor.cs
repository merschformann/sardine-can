using SC.Heuristics.PrimalHeuristic;
using SC.ObjectModel;
using SC.ObjectModel.Additionals;
using SC.ObjectModel.Configuration;
using SC.ObjectModel.Interfaces;
using SC.Linear;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SC.ExecutionHandler
{
    /// <summary>
    /// A class used to wrap the methods to enable execution by the CLI
    /// </summary>
    public class Executor
    {
        /// <summary>
        /// The footprint file
        /// </summary>
        public const string FILENAME_FOOTPRINT = "footprint.csv";

        /// <summary>
        /// The consolidated footprint file
        /// </summary>
        public const string FILENAME_CONSOLIDATED_FOOTPRINTS = "footsprints.csv";

        /// <summary>
        /// The status over time log file
        /// </summary>
        public const string FILENAME_STATUS_OVER_TIME = "progression.dat";

        /// <summary>
        /// The name of the instance that is solved
        /// </summary>
        public const string FILENAME_INSTANCE_NAME = "instance.txt";

        /// <summary>
        /// The name of the config used for solving
        /// </summary>
        public const string FILENAME_CONFIG_NAME = "config.txt";

        /// <summary>
        /// The name of the gnuplot progression script file
        /// </summary>
        public const string FILENAME_PROGRESSION_SCRIPT = "progression.gp";

        /// <summary>
        /// The name of the batch file to generate in order to execute the progression script
        /// </summary>
        public const string FILENAME_PROGRESSION_SCRIPT_BATCH_FILE = "progression.cmd";

        /// <summary>
        /// Executes a single method-run
        /// </summary>
        /// <param name="instanceFile">The path to the instance</param>
        /// <param name="configFile">The path to the config</param>
        /// <param name="outputDirectory">The directory to write the results to</param>
        /// <param name="seedNumber">The seed to use</param>
        public static void Execute(string instanceFile, string configFile, string outputDirectory, string seedNumber)
        {
            // Save parameters for the case of an exception
            ExInstanceName = instanceFile;
            ExConfigName = configFile;
            ExSeedNumber = seedNumber;

            // Read config
            Configuration config = Configuration.Read(configFile);
            Instance instance = Instance.ReadXML(instanceFile);

            // Create output dir if not already existing
            string exportationDir = Path.Combine(outputDirectory, instance.Name + "-" + config.Name + "-" + seedNumber);
            if (Directory.Exists(exportationDir))
                Directory.Delete(exportationDir, true);
            // Wait for directory to be really deleted - nasty but necessary
            while (Directory.Exists(exportationDir))
                Thread.Sleep(100);
            // Create a fresh directory
            Directory.CreateDirectory(exportationDir);

            // Catch unhandled exceptions
            if (!AppDomain.CurrentDomain.FriendlyName.EndsWith("vshost.exe"))
            {
                Console.WriteLine("Adding handler for unhandled exceptions ...");
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(LogUnhandledException);
            }

            // Determine log file
            string logFile = Path.Combine(exportationDir, "output.txt");

            // Write instance name
            using (StreamWriter sw = new StreamWriter(Path.Combine(exportationDir, FILENAME_INSTANCE_NAME)))
                sw.Write(instance.Name);

            // Write configuration name
            using (StreamWriter sw = new StreamWriter(Path.Combine(exportationDir, FILENAME_CONFIG_NAME)))
                sw.Write(config.Name);

            // Write all output
            using (StreamWriter outputWriter = new StreamWriter(logFile) { AutoFlush = true })
            {
                // Write solution status over time
                using (StreamWriter statusWriter = new StreamWriter(Path.Combine(exportationDir, FILENAME_STATUS_OVER_TIME)) { AutoFlush = true })
                {
                    // Prepare
                    Action<string> logger = (string s) => { outputWriter.Write(s); Console.Write(s); };
                    Action<string> loggerLine = (string s) => { outputWriter.Write(s + Environment.NewLine); Console.WriteLine(s); };
                    statusWriter.WriteLine("% time incumbent");
                    config.LogSolutionStatus = (double timeStamp, double incumbent) => { statusWriter.WriteLine(timeStamp.ToString(ExportationConstants.FORMATTER) + ExportationConstants.GNUPLOT_DELIMITER + incumbent.ToString(ExportationConstants.FORMATTER)); };
                    config.Log = logger;
                    config.Seed = int.Parse(seedNumber);
                    IMethod method = null;
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
                    loggerLine("Welcome to SardineCan CLI");
                    loggerLine("Parameters: " + instanceFile + " " + configFile + " " + outputDirectory + " " + seedNumber);
                    loggerLine("Initializing ...");
                    loggerLine("Instance: " + instance.Name);
                    loggerLine("Config: " + config.Name);
                    loggerLine("Seed: " + seedNumber);
                    loggerLine("Config-details: ");
                    LogConfigDetails(config, logger);
                    loggerLine("");

                    // Execute
                    logger("Executing ... ");
                    PerformanceResult result = method.Run();

                    // Log some information to the console
                    loggerLine("Finished!");
                    loggerLine("");
                    loggerLine("Result outline:");
                    loggerLine("Obj: " + result.ObjectiveValue.ToString(CultureInfo.InvariantCulture));
                    loggerLine("VolumeContained: " + result.Solution.VolumeContained.ToString(CultureInfo.InvariantCulture));
                    loggerLine("VolumeOfContainers: " + result.Solution.VolumeOfContainers.ToString(CultureInfo.InvariantCulture));
                    loggerLine("VolumeContainedRelative: " + (result.Solution.VolumeContainedRelative * 100).ToString(CultureInfo.InvariantCulture) + "%");
                    loggerLine("VolumeOfContainersInUse: " + result.Solution.VolumeOfContainersInUse.ToString(CultureInfo.InvariantCulture));
                    loggerLine("NumberOfContainersInUse: " + result.Solution.NumberOfContainersInUse.ToString(CultureInfo.InvariantCulture));
                    loggerLine("NumberOfPiecesPacked: " + result.Solution.NumberOfPiecesPacked.ToString(CultureInfo.InvariantCulture));
                    loggerLine("SolutionTime: " + result.SolutionTime.TotalSeconds.ToString(CultureInfo.InvariantCulture) + "s");

                    // Log performance information
                    using (StreamWriter solutionInfoWriter = new StreamWriter(File.Open(Path.Combine(exportationDir, "result.txt"), FileMode.Create)))
                    {
                        solutionInfoWriter.WriteLine("Runtime: " + result.SolutionTime.ToString());
                        solutionInfoWriter.WriteLine("ObjectiveValue: " + result.ObjectiveValue.ToString(ExportationConstants.FORMATTER));
                        solutionInfoWriter.WriteLine("BestBound: " + result.BestBound.ToString(ExportationConstants.FORMATTER));
                        solutionInfoWriter.WriteLine("RemainingGap: " + result.Gap.ToString(ExportationConstants.FORMATTER));
                        result.Instance.OutputInfo(solutionInfoWriter);
                        //result.Solution.out // TODO define output of solution info
                    }

                    // Log footprint
                    using (StreamWriter sw = new StreamWriter(Path.Combine(exportationDir, FILENAME_FOOTPRINT)))
                        sw.WriteLine(
                            instance.Name + ExportationConstants.CSV_DELIMITER +
                            config.Name + ExportationConstants.CSV_DELIMITER +
                            seedNumber + ExportationConstants.CSV_DELIMITER +
                            instance.Containers.Count + ExportationConstants.CSV_DELIMITER +
                            instance.Pieces.Count + ExportationConstants.CSV_DELIMITER +
                            instance.Containers.Sum(c => c.VirtualPieces.Count).ToString(CultureInfo.InvariantCulture) + ExportationConstants.CSV_DELIMITER +
                            instance.Containers.Sum(c => c.Slants.Count).ToString(CultureInfo.InvariantCulture) + ExportationConstants.CSV_DELIMITER +
                            result.ObjectiveValue.ToString(CultureInfo.InvariantCulture) + ExportationConstants.CSV_DELIMITER +
                            result.Solution.VolumeContained.ToString(CultureInfo.InvariantCulture) + ExportationConstants.CSV_DELIMITER +
                            result.Solution.VolumeOfContainers.ToString(CultureInfo.InvariantCulture) + ExportationConstants.CSV_DELIMITER +
                            (result.Solution.VolumeContainedRelative * 100).ToString(CultureInfo.InvariantCulture) + ExportationConstants.CSV_DELIMITER +
                            result.Solution.VolumeOfContainersInUse.ToString(CultureInfo.InvariantCulture) + ExportationConstants.CSV_DELIMITER +
                            result.Solution.NumberOfContainersInUse.ToString(CultureInfo.InvariantCulture) + ExportationConstants.CSV_DELIMITER +
                            result.Solution.NumberOfPiecesPacked.ToString(CultureInfo.InvariantCulture) + ExportationConstants.CSV_DELIMITER +
                            result.SolutionTime.TotalSeconds.ToString(CultureInfo.InvariantCulture));

                    // Write instance-solution as xml
                    if (instance.Solutions.Any())
                        instance.WriteXML(Path.Combine(exportationDir, "solution.xinst"));
                }
            }
        }

        public static PerformanceResult Execute(Instance instance, Configuration config, Action<string> logger)
        {
            // Prepare logging
            void loggerLine(string msg) { logger?.Invoke(msg + Environment.NewLine); }
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
            loggerLine($">>> Welcome to SardineCan");
            loggerLine($"Initializing ...");
            loggerLine($"Instance: {instance.Name}");
            loggerLine($"Config: {config.Name}");
            loggerLine($"Seed: {config.Seed}");
            loggerLine($"Config-details: ");
            LogConfigDetails(config, logger);
            logger?.Invoke(Environment.NewLine);

            // Execute
            loggerLine($"Executing ... ");
            PerformanceResult result = method.Run();

            // Log some information to the console
            loggerLine($"Finished!");
            loggerLine($"Result outline:");
            loggerLine($"Obj: {result.ObjectiveValue.ToString(CultureInfo.InvariantCulture)}");
            loggerLine($"VolumeContained: {result.Solution.VolumeContained.ToString(CultureInfo.InvariantCulture)}");
            loggerLine($"VolumeOfContainers: {result.Solution.VolumeOfContainers.ToString(CultureInfo.InvariantCulture)}");
            loggerLine($"VolumeContainedRelative: {(result.Solution.VolumeContainedRelative * 100).ToString(CultureInfo.InvariantCulture)}%");
            loggerLine($"VolumeOfContainersInUse: {result.Solution.VolumeOfContainersInUse.ToString(CultureInfo.InvariantCulture)}");
            loggerLine($"NumberOfContainersInUse: {result.Solution.NumberOfContainersInUse.ToString(CultureInfo.InvariantCulture)}");
            loggerLine($"NumberOfPiecesPacked: {result.Solution.NumberOfPiecesPacked.ToString(CultureInfo.InvariantCulture)}");
            loggerLine($"SolutionTime: {result.SolutionTime.TotalSeconds.ToString(CultureInfo.InvariantCulture)}s");

            // We're done here
            return result;
        }

        /// <summary>
        /// Returns an appropriate header for the consolidated footprint file
        /// </summary>
        /// <returns>The header</returns>
        public static string GetFootPrintHeader()
        {
            return "Instance" + ExportationConstants.CSV_DELIMITER +
                "Config" + ExportationConstants.CSV_DELIMITER +
                "Seed" + ExportationConstants.CSV_DELIMITER +
                "Containers" + ExportationConstants.CSV_DELIMITER +
                "Pieces" + ExportationConstants.CSV_DELIMITER +
                "VirtualPieces" + ExportationConstants.CSV_DELIMITER +
                "Slants" + ExportationConstants.CSV_DELIMITER +
                "ObjectiveValue" + ExportationConstants.CSV_DELIMITER +
                "VolumeContained" + ExportationConstants.CSV_DELIMITER +
                "VolumeOfContainers" + ExportationConstants.CSV_DELIMITER +
                "VolumeContainedRelative" + ExportationConstants.CSV_DELIMITER +
                "VolumeOfContainersInUse" + ExportationConstants.CSV_DELIMITER +
                "NumberOfContainersInUse" + ExportationConstants.CSV_DELIMITER +
                "NumberOfPiecesPacked" + ExportationConstants.CSV_DELIMITER +
                "SolutionTimeInSeconds";
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
                            value = toStringMethod.Invoke(field.GetValue(config), new object[] { CultureInfo.InvariantCulture }).ToString();
                        else
                            value = field.GetValue(config).ToString();
                    }
                }
                // Output it
                logger?.Invoke(field.Name + ": " + value + Environment.NewLine);
            }
        }

        /// <summary>
        /// Logs an unhandled exception.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        private static void LogUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogException((Exception)e.ExceptionObject);
        }

        /// <summary>
        /// The saved instance name that is written to output in case of an exception.
        /// </summary>
        private static string ExInstanceName;
        /// <summary>
        /// The saved config name that is written to output in case of an exception.
        /// </summary>
        private static string ExConfigName;
        /// <summary>
        /// The saved seed number that is written to output in case of an exception.
        /// </summary>
        private static string ExSeedNumber;

        /// <summary>
        /// Logs an exception to the default file.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="additionFileToLogTo">Specifies an additional file to log the exception to.</param>
        private static void LogException(Exception ex, string additionFileToLogTo = null)
        {
            // Init output log
            using (StreamWriter sw = new StreamWriter("exception.txt", true))
            {
                StreamWriter additionalSW = additionFileToLogTo != null ? new StreamWriter(additionFileToLogTo, true) : null;
                Action<string> log = (string msg) => { sw.Write(msg); Console.Write(msg); if (additionalSW != null) { additionalSW.Write(msg); } };
                Action<string> logLine = (string msg) => { sw.WriteLine(msg); Console.WriteLine(msg); if (additionalSW != null) { additionalSW.WriteLine(msg); } };
                logLine("---> Caught an unhandled exception: ");
                logLine("Instance: " + ExInstanceName);
                logLine("Config: " + ExConfigName);
                logLine("Seed: " + ExSeedNumber);
                logLine("Message: " + ex.Message);
                logLine("Time: " + DateTime.Now.ToString(CultureInfo.InvariantCulture));
                logLine("Stacktrace:");
                logLine(ex.StackTrace);
                log("InnerException: ");
                if (ex.InnerException != null)
                {
                    logLine(ex.InnerException.Message);
                    logLine("Stacktrace:");
                    logLine(ex.InnerException.StackTrace);
                }
                else
                {
                    logLine("None");
                }
                additionalSW.Close();
            }
        }
    }
}
