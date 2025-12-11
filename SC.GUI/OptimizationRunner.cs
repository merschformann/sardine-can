using SC.Core.Heuristics;
using SC.Core.Heuristics.PrimalHeuristic;
using SC.Core.ObjectModel;
using SC.Core.ObjectModel.Additionals;
using SC.Core.ObjectModel.Configuration;
using SC.Core.ObjectModel.Generator;
using SC.Core.ObjectModel.Interfaces;
using SC.Core.Toolbox;
using SC.Core.Linear;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SC.GUI
{
    /// <summary>
    /// Used to execute demonstrations
    /// </summary>
    class OptimizationRunner
    {
        /// <summary>
        /// The instance to solve
        /// </summary>
        private Instance _instance;

        /// <summary>
        /// The method to execute
        /// </summary>
        private IMethod method;

        /// <summary>
        /// The action used to submit solutions
        /// </summary>
        private Action<COSolution, bool, string, string> _submitAction;

        /// <summary>
        /// The action executed when finished
        /// </summary>
        private Action _finishAction;

        /// <summary>
        /// The action when starting to solve
        /// </summary>
        private Action _startSingleSolveAction;

        /// <summary>
        /// The action when finishing to solve
        /// </summary>
        private Action _finishSingleSolveAction;

        /// <summary>
        /// The export dir
        /// </summary>
        private string _exportDir;

        /// <summary>
        /// Runs the progress
        /// </summary>
        /// <param name="context">Dummy / not used</param>
        public void Run(object context)
        {
            // Write instance-solution as xml
            _instance.WriteXMLWithoutSolutions(Path.Combine(_exportDir, _instance.Name + ".xml"));
            _startSingleSolveAction.Invoke();
            PerformanceResult result = method.Run();
            _finishSingleSolveAction.Invoke();
            string solutionFile = _instance.Name + "_Solution.txt";
            using (StreamWriter sw = new StreamWriter(File.Open(Path.Combine(_exportDir, solutionFile), FileMode.Create)))
            {
                sw.WriteLine("ObjectiveValue: " + result.ObjectiveValue);
                sw.WriteLine("Runtime: " + result.SolutionTime.ToString());
                result.Instance.OutputInfo(sw);
                //result.Solution.out // TODO define output of solution info
            }
            _submitAction.Invoke(method.Solution, method.HasSolution, ExportationConstants.ExportDir, _instance.GetIdent());
            // Write instance-solution as xml
            _instance.WriteXML(Path.Combine(_exportDir, _instance.GetIdent() + "_Solution.xml"));
            _finishAction();
        }

        /// <summary>
        /// Cancels the ongoing progress
        /// </summary>
        public void Cancel() => method.Cancel();

        /// <summary>
        /// Creates a new optimization runner
        /// </summary>
        /// <param name="method">The method</param>
        /// <param name="instance">The instance to solve</param>
        /// <param name="exportDir">The export-dir</param>
        /// <param name="submitAction">The action used to submit solutions</param>
        /// <param name="finishAction">The action to execute when finished</param>
        /// <param name="startSingleSolveAction">The action when starting to solve</param>
        /// <param name="finishSingleSolveAction">The action when finishing to solve</param>
        public OptimizationRunner(
            IMethod method,
            Instance instance,
            string exportDir,
            Action<COSolution, bool, string, string> submitAction,
            Action finishAction,
            Action startSingleSolveAction,
            Action finishSingleSolveAction)
        {
            this.method = method;
            _instance = instance;
            _submitAction = submitAction;
            _finishAction = finishAction;
            _exportDir = exportDir;
            _startSingleSolveAction = startSingleSolveAction;
            _finishSingleSolveAction = finishSingleSolveAction;
        }
    }

    /// <summary>
    /// Used to execute demonstrations
    /// </summary>
    class EvaluationFolderRunner
    {
        /// <summary>
        /// The method to execute
        /// </summary>
        private readonly IMethod _method;

        /// <summary>
        /// The method to execute
        /// </summary>
        private readonly List<Instance> _instances;

        /// <summary>
        /// instance names
        /// </summary>
        private readonly Dictionary<Instance, string> _names;

        /// <summary>
        /// The action executed when finished
        /// </summary>
        private readonly Action<PerformanceResult> _instanceFinishedAction;

        /// <summary>
        /// The action when finishing to solve
        /// </summary>
        private readonly Action _finishAction;

        /// <summary>
        /// The export dir
        /// </summary>
        private readonly string _exportDir;

        /// <summary>
        /// Time limit
        /// </summary>
        protected TimeSpan TimeLimit;

        /// <summary>
        /// Log every Improvement
        /// </summary>
        public bool DetailedLog = false;

        /// <summary>
        /// Creates a new optimization runner
        /// </summary>
        /// <param name="method">The method</param>
        /// <param name="instances">The instances</param>
        /// <param name="exportDir">The export-dir</param>
        /// <param name="instanceFinishedAction">The action to execute when finished one instance</param>
        /// <param name="finishAction">The action to execute when finished</param>
        /// <param name="timeLimit">time limit</param>
        public EvaluationFolderRunner(IMethod method, List<Instance> instances, Dictionary<Instance, string> names, string exportDir, Action<PerformanceResult> instanceFinishedAction, Action finishAction, TimeSpan timeLimit)
        {
            _method = method;
            _names = names;
            _instances = instances;
            _finishAction = finishAction;
            _instanceFinishedAction = instanceFinishedAction;
            _exportDir = exportDir;
            TimeLimit = timeLimit;
        }

        /// <summary>
        /// Runs the progress
        /// </summary>
        /// <param name="context">Dummy / not used</param>
        /// <param name="nameExtension">additional name</param>
        public void Run(object context)
        {


            // Write instance-solution as xml
            //_instance.WriteXMLWithoutSolutions(Path.Combine(_exportDir, _instance.GetIdent() + ".xml"));
            foreach (var instace in _instances)
            {
                var output = new Dictionary<string, List<Tuple<double, double>>>();

                _method.Instance = instace;
                _method.Reset(); //Load the instance correctly etc.

                using (var sw = new StreamWriter(File.Open(Path.Combine(_exportDir, @"evaluationResult_" + _names[_method.Instance].Replace(".xinst", "") + @".csv"), FileMode.Create)))
                {

                    // Start statistics file
                    sw.Write(
                        "Instance" + ExportationConstants.CSV_DELIMITER +
                        "Runtime" + ExportationConstants.CSV_DELIMITER +
                        "Volume of Containters" + ExportationConstants.CSV_DELIMITER
                        );

                    //header
                    foreach (var config in output.Keys)
                        sw.Write(config + ExportationConstants.CSV_DELIMITER);

                    //new line
                    sw.WriteLine();

                    for (double time = 0; time < TimeLimit.TotalSeconds + 3.0; time = time + 0.1)
                    {
                        //default begining
                        sw.Write(
                            _method.Instance.GetIdent() + ExportationConstants.CSV_DELIMITER +
                            time.ToString(CultureInfo.InvariantCulture) + ExportationConstants.CSV_DELIMITER +
                            _method.Solution.VolumeOfContainers.ToString(CultureInfo.InvariantCulture) + ExportationConstants.CSV_DELIMITER
                            );


                        foreach (var config in output.Keys)
                        {
                            //get the solution value
                            int entry;
                            for (entry = 0; entry < output[config].Count && output[config][entry].Item1 <= time; entry++)
                            {
                            }
                            entry--;

                            //output to file
                            sw.Write(output[config][entry].Item2.ToString(CultureInfo.InvariantCulture) + ExportationConstants.CSV_DELIMITER);
                        }

                        //new line
                        sw.WriteLine();
                    }


                    sw.Flush();
                }

                //reset method
                _method.Reset();

            }

            if (_finishAction != null)
                _finishAction.Invoke();
        }

        /// <summary>
        /// Cancels the ongoing progress
        /// </summary>
        public void Cancel()
        {
            _method.Cancel();
        }

    }

    /// <summary>
    /// Used to execute evaluations
    /// </summary>
    class EvaluationRunner
    {
        /// <summary>
        /// The action when starting to solve
        /// </summary>
        private Action _startSingleSolveAction;

        /// <summary>
        /// The action when finishing to solve
        /// </summary>
        private Action _finishSingleSolveAction;

        /// <summary>
        /// The action used to submit solutions
        /// </summary>
        private Action<COSolution, bool, string, string> _submitAction;

        /// <summary>
        /// The action executed when finishing the progress
        /// </summary>
        private Action _finishAction;

        /// <summary>
        /// The action used to log
        /// </summary>
        private Action<string> _logAction;

        /// <summary>
        /// The export-dir
        /// </summary>
        private string _exportDir;

        /// <summary>
        /// Indicates a cancelation of the progress
        /// </summary>
        private bool _cancel;

        /// <summary>
        /// The statistics-file to log to
        /// </summary>
        string _statisticsFile = "Statistics.csv";

        /// <summary>
        /// The instance to solve
        /// </summary>
        private Instance _instance;

        /// <summary>
        /// The list of all obtained evaluation values
        /// </summary>
        private List<double> _evaluationValues;

        /// <summary>
        /// The list of all best-bounds
        /// </summary>
        private List<double> _bestBounds;

        /// <summary>
        /// The list of all gaps
        /// </summary>
        private List<double> _gaps;

        /// <summary>
        /// The list of the time consumed per run
        /// </summary>
        private List<TimeSpan> _timeConsumed;

        #region Parameter fields

        /// <summary>
        /// The method to use
        /// </summary>
        private IMethod _method;

        /// <summary>
        /// The type of the method to use
        /// </summary>
        private MethodType _methodToUse;

        /// <summary>
        /// The config
        /// </summary>
        public Configuration Config { get; set; }

        #endregion

        #region Instance generation fields

        /// <summary>
        /// The config for the instance-generator
        /// </summary>
        public InstanceGeneratorConfiguration GeneratorConfig { get; set; }

        /// <summary>
        /// The number of seed-passes to do
        /// </summary>
        public int SeedPasses { get; set; }

        #endregion

        /// <summary>
        /// Runs the process
        /// </summary>
        /// <param name="context">Dummy / not in use</param>
        public void Run(object context)
        {
            // Prepare statistics file
            using (StreamWriter statisticsWriter = new StreamWriter(File.Open(Path.Combine(_exportDir, _statisticsFile), FileMode.Create)))
            {
                // Start statistics file
                statisticsWriter.WriteLine(
                    "Instance" + ExportationConstants.CSV_DELIMITER +
                    "Runtime" + ExportationConstants.CSV_DELIMITER +
                    "Solution" + ExportationConstants.CSV_DELIMITER +
                    "BestBound" + ExportationConstants.CSV_DELIMITER +
                    "Gap");
            }

            // Init default config
            switch (_methodToUse)
            {
                case MethodType.ExtremePointInsertion:
                case MethodType.SpaceDefragmentation:
                case MethodType.PushInsertion:
                    {
                        if (Config == null)
                        {
                            Config = new Configuration(_methodToUse, true) { Log = _logAction };
                        }
                    }
                    break;
                case MethodType.FrontLeftBottomStyle:
                case MethodType.TetrisStyle:
                    {
                        if (Config == null)
                        {
                            Config = new Configuration(_methodToUse, true) { Log = _logAction };
                        }
                    }
                    break;
                case MethodType.SpaceIndexed:
                    throw new NotImplementedException("Space indexed model not working for now.");
                default:
                    throw new ArgumentException("Unknown method: " + _methodToUse.ToString());
            }


            _timeConsumed = _timeConsumed ?? new List<TimeSpan>();
            _evaluationValues = _evaluationValues ?? new List<double>();
            _bestBounds = _bestBounds ?? new List<double>();
            _gaps = _gaps ?? new List<double>();

            // Solve incrementally for each seed
            for (int k = 0; k < SeedPasses; k++)
            {
                // Solve incrementally for given container count
                for (int j = GeneratorConfig.ContainerMin; j <= GeneratorConfig.ContainerMax; j++)
                {
                    // Solve incrementally for given piece count
                    for (int i = 1; i <= GeneratorConfig.MaxBoxCount && !_cancel; i++)
                    {
                        // Prepare
                        switch (_methodToUse)
                        {
                            case MethodType.ExtremePointInsertion:
                            case MethodType.TetrisStyle:
                                {
                                    _instance = new Instance();
                                    _instance.Containers = InstanceGenerator.GenerateContainer(
                                        j,
                                        GeneratorConfig.ContainerSideLengthMin,
                                        GeneratorConfig.ContainerSideLengthMax,
                                        GeneratorConfig.Rounding,
                                        k);
                                    _instance.Pieces = InstanceGenerator.GeneratePerformanceTestTetrisPieces(
                                        i,
                                        GeneratorConfig.PieceMaxSize,
                                        GeneratorConfig.PieceMinSize,
                                        GeneratorConfig.PieceMinEquals,
                                        GeneratorConfig.PieceMaxEquals,
                                        roundedDecimals: GeneratorConfig.Rounding,
                                        seed: k);
                                }
                                break;
                            case MethodType.FrontLeftBottomStyle:
                            case MethodType.SpaceDefragmentation:
                            case MethodType.PushInsertion:
                                {
                                    _instance = new Instance();
                                    _instance.Containers = InstanceGenerator.GenerateContainer(
                                        j,
                                        GeneratorConfig.ContainerSideLengthMin,
                                        GeneratorConfig.ContainerSideLengthMax,
                                        GeneratorConfig.Rounding,
                                        k);
                                    _instance.Pieces = InstanceGenerator.GeneratePieces(
                                        i,
                                        GeneratorConfig.PieceMaxSize,
                                        GeneratorConfig.PieceMinSize,
                                        GeneratorConfig.PieceMinEquals,
                                        GeneratorConfig.PieceMaxEquals,
                                        roundedDecimals: GeneratorConfig.Rounding,
                                        seed: k);
                                }
                                break;
                            case MethodType.SpaceIndexed:
                                throw new NotImplementedException("Space indexed model not working for now.");
                            default:
                                throw new ArgumentException("Unknown method: " + _methodToUse.ToString());
                        }

                        // Execute
                        Execute(ExportationConstants.ExportDir, _instance.GetIdent(), int.MaxValue);
                    }
                }
            }
            _finishAction();
        }

        /// <summary>
        /// Executes a single method-run
        /// </summary>
        /// <param name="exportationDir">The exportation dir to export the results to</param>
        /// <param name="logFilename">The name of the log-files</param>
        /// <param name="timeOut">A timeout in milliseconds</param>
        private void Execute(string exportationDir, string logFilename, int timeOut)
        {
            // Prepare
            IMethod method = null;
            switch (_methodToUse)
            {
                case MethodType.FrontLeftBottomStyle:
                    {
                        method = new LinearModelFLB(_instance, Config);
                    }
                    break;
                case MethodType.TetrisStyle:
                    {
                        method = new LinearModelTetris(_instance, Config);
                    }
                    break;
                case MethodType.HybridStyle:
                    {
                        method = new LinearModelHybrid(_instance, Config);
                    }
                    break;
                case MethodType.SpaceIndexed:
                    throw new NotImplementedException("Space indexed model not working for now.");
                case MethodType.ExtremePointInsertion:
                    {
                        method = new ExtremePointInsertionHeuristic(_instance, Config);
                    }
                    break;
                case MethodType.SpaceDefragmentation:
                    {
                        method = new SpaceDefragmentationHeuristic(_instance, Config);
                    }
                    break;
                case MethodType.PushInsertion:
                    {
                        method = new PushInsertion(_instance, Config);
                    }
                    break;
                case MethodType.ALNS:
                    {
                        method = new ALNS(_instance, Config);
                    }
                    break;
                default:
                    throw new ArgumentException("Unknown method: " + _methodToUse.ToString());
            }
            // Write instance-solution as xml
            string xmlInstanceFile = logFilename + ".xinst";
            if (!File.Exists(xmlInstanceFile))
            {
                _instance.WriteXML(Path.Combine(exportationDir, xmlInstanceFile));
            }
            _method = method;
            // Execute
            _startSingleSolveAction.Invoke();
            PerformanceResult result = null;
            bool executionSuccess = Helper.TryExecute(method.Run, timeOut, out result);
            _finishSingleSolveAction.Invoke();
            string solutionFile = logFilename + "_Solution.txt";
            using (StreamWriter solutionInfoWriter = new StreamWriter(File.Open(Path.Combine(exportationDir, solutionFile), FileMode.Create)))
            {
                solutionInfoWriter.WriteLine("Runtime: " + ((executionSuccess) ? result.SolutionTime.ToString() : "timeout"));
                solutionInfoWriter.WriteLine("ObjectiveValue: " + ((executionSuccess) ? result.ObjectiveValue.ToString(ExportationConstants.FORMATTER) : "timeout"));
                solutionInfoWriter.WriteLine("BestBound: " + ((executionSuccess) ? result.BestBound.ToString(ExportationConstants.FORMATTER) : "timeout"));
                solutionInfoWriter.WriteLine("RemainingGap: " + ((executionSuccess) ? result.Gap.ToString(ExportationConstants.FORMATTER) : "timeout"));
                if (executionSuccess)
                {
                    result.Instance.OutputInfo(solutionInfoWriter);
                }
                //result.Solution.out // TODO define output of solution info
            }
            // Log information
            if (executionSuccess)
            {
                _timeConsumed.Add(result.SolutionTime);
                _evaluationValues.Add(result.ObjectiveValue);
                _bestBounds.Add(result.BestBound);
                _gaps.Add(result.Gap);
            }
            else
            {
                _timeConsumed.Add(TimeSpan.FromMilliseconds(timeOut));
                _evaluationValues.Add(0);
                _bestBounds.Add(double.NaN);
                _gaps.Add(double.NaN);
            }
            // Write statistics
            using (StreamWriter statisticsWriter = new StreamWriter(File.Open(Path.Combine(exportationDir, _statisticsFile), FileMode.Append)))
            {
                statisticsWriter.WriteLine(
                    _instance.GetIdent() +
                    ExportationConstants.CSV_DELIMITER +
                    ((executionSuccess) ?
                        result.SolutionTime.TotalSeconds.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) :
                        TimeSpan.FromMilliseconds(timeOut).TotalSeconds.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER)) +
                    ExportationConstants.CSV_DELIMITER +
                    ((executionSuccess) ?
                        result.ObjectiveValue.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) :
                        (0.0).ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER)) +
                    ExportationConstants.CSV_DELIMITER +
                    ((executionSuccess) ?
                        result.BestBound.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) :
                        double.NaN.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER)) +
                    ExportationConstants.CSV_DELIMITER +
                    ((executionSuccess) ?
                        result.Gap.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) :
                        double.NaN.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER)));
            }
            // Write instance-solution as xml
            if (_instance.Solutions.Any())
            {
                _instance.WriteXML(Path.Combine(exportationDir, logFilename + "_Solution.xinst"));
            }
            // Draw visuals
            if (_method.HasSolution)
            {
                _submitAction(method.Solution, _method.HasSolution, exportationDir, logFilename);
            }
            // Wait a bit
            Thread.Sleep(5000);
        }

        /// <summary>
        /// Cancels any ongoing progress at the next point it is safe
        /// </summary>
        public void Cancel()
        {
            if (_method != null)
            {
                _method.Cancel();
            }
            _cancel = true;
        }

        /// <summary>
        /// Creates a new evaluation-runner
        /// </summary>
        /// <param name="methodToUse">Defines the method to execute</param>
        /// <param name="exportDir">Defines the export-dir</param>
        /// <param name="submitSolutionAction">The action used to submit solutions</param>
        /// <param name="logAction">The action used to log</param>
        /// <param name="finishAction">The action to execute when finished</param>
        /// <param name="startSingleSolveAction">The action executed when starting a single process</param>
        /// <param name="finishSingleSolveAction">The action executed when finishing a single process</param>
        public EvaluationRunner(
            MethodType methodToUse,
            string exportDir,
            Action<COSolution, bool, string, string> submitSolutionAction,
            Action<string> logAction,
            Action finishAction,
            Action startSingleSolveAction,
            Action finishSingleSolveAction)
        {
            _methodToUse = methodToUse;
            _startSingleSolveAction = startSingleSolveAction;
            _finishSingleSolveAction = finishSingleSolveAction;
            _finishAction = finishAction;
            _submitAction = submitSolutionAction;
            _logAction = logAction;
            _exportDir = exportDir;
        }
    }

    class BatchRunner
    {
        /// <summary>
        /// The method to use.
        /// </summary>
        public IMethod Method;
        /// <summary>
        /// Creates all instances to solve.
        /// </summary>
        public Func<Instance> InstanceCreator;
        /// <summary>
        /// The timeout to adhere to.
        /// </summary>
        public double Timeout;
        /// <summary>
        /// The directory to write all results to.
        /// </summary>
        public string ResultsDir = Path.Combine(Environment.CurrentDirectory, "Results");
        /// <summary>
        /// The name of the result file.
        /// </summary>
        public string StatisticsFile = "results.csv";
        /// <summary>
        /// The action to invoke, if a solution was found.
        /// </summary>
        public Action<COSolution, bool, string, string> SubmitSolutionAction;
        /// <summary>
        /// The action to use for logging a line.
        /// </summary>
        public Action<string> LogAction;
        /// <summary>
        /// The action to invoke after finishing all runs.
        /// </summary>
        public Action FinishAction;
        /// <summary>
        /// The action to invoke before starting a single run.
        /// </summary>
        public Action StartSingleSolveAction;
        /// <summary>
        /// The action to invoke after starting a single run.
        /// </summary>
        public Action FinishSingleSolveAction;
        /// <summary>
        /// Indicates that a quit is pending.
        /// </summary>
        private bool _quit = false;

        /// <summary>
        /// Cancels the ongoing progress
        /// </summary>
        public void Cancel()
        {
            _quit = true;
            Method.Cancel();
        }

        /// <summary>
        /// Runs the process.
        /// </summary>
        /// <param name="context">Dummy / not in use</param>
        public void Run(object context)
        {
            // Prepare directory (clear previous results)
            if (Directory.Exists(ResultsDir))
            { Directory.Delete(ResultsDir, true); Thread.Sleep(50); }
            Directory.CreateDirectory(ResultsDir);
            Thread.Sleep(50);
            // Start statistics file
            using (StreamWriter sw = new StreamWriter(Path.Combine(ResultsDir, StatisticsFile), false))
                sw.WriteLine(
                    "Instance" + ExportationConstants.CSV_DELIMITER +
                    "Runtime" + ExportationConstants.CSV_DELIMITER +
                    "SpaceUtil" + ExportationConstants.CSV_DELIMITER +
                    "PackedPieces");

            // Prepare
            Method.Config.TimeLimit = TimeSpan.FromSeconds(Timeout);
            Method.Config.Log = LogAction;

            // Execute all
            Instance instance = InstanceCreator();
            while (instance != null)
            {
                // Quit, if desired
                if (_quit)
                    break;
                // Prepare individual run
                Method.Instance = instance;
                Method.Reset();
                instance.WriteXMLWithoutSolutions(Path.Combine(ResultsDir, instance.Name + "_raw.xinst"));
                // Execute
                StartSingleSolveAction?.Invoke();
                PerformanceResult result = Method.Run();
                FinishSingleSolveAction?.Invoke();
                // Write result
                instance.WriteXML(Path.Combine(ResultsDir, instance.Name + "_sol.xinst"));
                using (StreamWriter sw = new StreamWriter(Path.Combine(ResultsDir, StatisticsFile), true))
                    sw.WriteLine(
                        instance.Name + ExportationConstants.CSV_DELIMITER +
                        result.SolutionTime.TotalSeconds.ToString(CultureInfo.InvariantCulture) + ExportationConstants.CSV_DELIMITER +
                        instance.Solutions.Single().VolumeContainedRelative.ToString(CultureInfo.InvariantCulture) + ExportationConstants.CSV_DELIMITER +
                        instance.Solutions.Single().NumberOfPiecesPacked.ToString(CultureInfo.InvariantCulture));
                // Update view
                SubmitSolutionAction?.Invoke(instance.Solutions.Single(), Method.HasSolution, ResultsDir, instance.Name);
                // Create next instance
                instance = InstanceCreator();
            }
            // Mark finish
            FinishAction?.Invoke();
        }
    }
}
