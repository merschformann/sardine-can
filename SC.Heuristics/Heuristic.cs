using SC.Heuristics.PrimalHeuristic;
using SC.ObjectModel;
using SC.ObjectModel.Additionals;
using SC.ObjectModel.Elements;
using SC.ObjectModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SC.ObjectModel.Configuration;

namespace SC.Heuristics
{
    /// <summary>
    /// The basic heuristic class
    /// </summary>
    public abstract class Heuristic : IMethod
    {
        /// <summary>
        /// The instance to solve by the approach
        /// </summary>
        public Instance Instance { get; set; }

        /// <summary>
        /// The solution obtained by this approach
        /// </summary>
        public COSolution Solution { get; set; }

        /// <summary>
        /// The configuration of the method
        /// </summary>
        public Configuration Config { get; set; }

        /// <summary>
        /// The time the solve process started. <code>null</code> if not yet started
        /// </summary>
        protected DateTime TimeStart { get; set; }

        /// <summary>
        /// Indicates whether the timelimit is reached. <code>true</code> if the timelimit is exceeded, <code>false</code> otherwise.
        /// </summary>
        public bool TimeUp { get { return (TimeSpan.MaxValue == Config.TimeLimit) ? false : DateTime.Now > TimeStart + Config.TimeLimit; } }

        /// <summary>
        /// The overall available volume across all containers
        /// </summary>
        protected double VolumeOfContainers { get; set; }

        /// <summary>
        /// Supplies the millis of the last time logging
        /// </summary>
        protected long LogOldMillis { get; set; }

        /// <summary>
        /// Supplies the millis of the last time logging the visuals
        /// </summary>
        protected long LogOldVisualMillis { get; set; }

        /// <summary>
        /// Used when random numbers are necessary
        /// </summary>
        protected Random Randomizer { get; set; }

        /// <summary>
        /// Creates a new mip based approach
        /// </summary>
        /// <param name="instance">The instance to solve</param>
        /// <param name="config">The configuration to use</param>
        public Heuristic(Instance instance, Configuration config)
        {
            Instance = instance;
            Config = config;
            Solution = instance.CreateSolution(config.Tetris, config.MeritType);
            Randomizer = new Random(config.Seed);
        }

        /// <summary>
        /// All solving magic happens here
        /// </summary>
        protected abstract void Solve();

        /// <summary>
        /// The main method
        /// </summary>
        public PerformanceResult Run()
        {
            // Init
            TimeStart = DateTime.Now;

            int itemCount = Instance.Pieces.Count();
            int containerCount = Instance.Containers.Count();

            // Start solving process
            Config.StartTimeStamp = DateTime.Now;

            // Main solve part
            Solve();

            // End solving process
            DateTime afterTimeStamp = DateTime.Now;

            // Return performance result
            PerformanceResult result = new PerformanceResult()
            {
                BestBound = double.NaN, // Unavailable
                Gap = double.NaN, // Unavailable
                Instance = Instance,
                Solution = Solution,
                SolutionTime = afterTimeStamp - Config.StartTimeStamp,
                ObjectiveValue = Solution.VolumeContained
            };
            // Log timestamp
            Config.LogSolutionStatus?.Invoke((DateTime.Now - Config.StartTimeStamp).TotalSeconds, Solution.ExploitedVolume);
            // Log finish
            if (Config.Log != null)
            {
                Config.Log(".Fin.\n");
                Config.Log("Instance contained " + itemCount + " pieces and " + containerCount + " container\n");
                Config.Log("Solution uses " + Solution.NumberOfContainersInUse + " containers and packed " + Solution.NumberOfPiecesPacked + " pieces\n");
                Config.Log("Volume utilization: " +
                    Solution.VolumeContained.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + " / " +
                    Solution.VolumeOfContainers.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                    " (" + ((Solution.VolumeContained / Solution.VolumeOfContainers) * 100).ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + "%)\n");
                Config.Log("Volume utilization (used containers): " +
                    Solution.VolumeContained.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + " / " +
                    Solution.VolumeOfContainersInUse.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                    " (" + ((Solution.VolumeContained / Solution.VolumeOfContainersInUse) * 100).ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + "%)\n");
                Config.Log("Time consumed: " + result.SolutionTime.ToString());
            }

            // Return
            return result;
        }

        /// <summary>
        /// Logs basic information about the progress
        /// </summary>
        /// <param name="piece">The currently added piece</param>
        /// <param name="container">The container to which the piece is added</param>
        protected void LogProgress(COSolution solution, Piece piece, Container container)
        {
            if (Config.Log != null && DateTime.Now.Ticks - LogOldMillis > 10000000)
            {
                LogOldMillis = DateTime.Now.Ticks;
                Config.Log(solution.ExploitedVolume.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + " / " +
                    VolumeOfContainers.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                    " - Piece " + piece.ID + " -> Container " + container.ID + "\n");
            }
        }

        /// <summary>
        /// Logs the visuals of the current solution
        /// </summary>
        /// <param name="solution">The solution to log</param>
        /// <param name="overrideTime">Overrides the timeout and logs anyway</param>
        protected void LogVisuals(COSolution solution, bool overrideTime)
        {
            if (Config.SubmitSolution != null && (overrideTime || DateTime.Now.Ticks - LogOldVisualMillis > 150000000))
            {
                LogOldVisualMillis = DateTime.Now.Ticks;
                Config.SubmitSolution(solution, true, ExportationConstants.ExportDir, Instance.GetIdent());
            }
        }

        /// <summary>
        /// Cancels the solve process
        /// </summary>
        public abstract void Cancel();

        /// <summary>
        /// Reset the Method
        /// </summary>
        public void Reset()
        {
            Cancelled = false;
            Solution = Instance.CreateSolution(Config.Tetris, Config.MeritType);
        }

        /// <summary>
        /// Indicates whether to cancel the whole process
        /// </summary>
        protected bool Cancelled { get; set; }

        /// <summary>
        /// Will not be verified by a heuristic and is therefore always <code>false</code>
        /// </summary>
        public abstract bool IsOptimal { get; }

        /// <summary>
        /// Will always at least return an empty solution
        /// </summary>
        public abstract bool HasSolution { get; }
    }
}
