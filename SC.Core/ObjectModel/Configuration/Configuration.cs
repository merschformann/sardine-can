using SC.Core.ObjectModel.Additionals;
using SC.Core.ObjectModel.Configuration;
using SC.Core.ObjectModel.Interfaces;
using SC.Core.Toolbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SC.Core.ObjectModel.Configuration
{
    /// <summary>
    /// The basic configuration class
    /// </summary>
    public class Configuration
    {
        #region Default values constructor

        /// <summary>
        /// Creates a new configuration
        /// </summary>
        public Configuration() { }

        /// <summary>
        /// Creates a new configuration
        /// </summary>
        /// <param name="method">The method to use</param>
        /// <param name="handleTetris">Indicates whether to respect tetris pieces (applies for some heuristics)</param>
        public Configuration(MethodType method, bool handleTetris)
        {
            // Set some default time-limit to not accidentally cause infinite runs
            TimeLimit = TimeSpan.FromSeconds(10);

            // Base settings
            Type = method;
            HandleGravity = true;
            HandleCompatibility = true;
            HandleStackability = true;
            HandleRotatability = true;
            HandleForbiddenOrientations = true;
            Name = "Default";

            if (method == MethodType.ALNS || method == MethodType.ExtremePointInsertion || method == MethodType.PushInsertion || method == MethodType.SpaceDefragmentation)
            {
                Tetris = true;
                PieceOrder = PieceOrderType.VwH;
                BestFit = true;
                MeritType = MeritFunctionType.MEDXYZ;
                Improvement = true;
                PieceReorder = PieceReorderType.Score;
                RandomSalt = 0.1;
                InflateAndReplaceInsertion = true;
                NormalizationOrder = new DimensionMarker[3] { DimensionMarker.X, DimensionMarker.Y, DimensionMarker.Z };
                ExhaustiveEPProne = false;
                StagnationDistance = 3000;
                MaximumPercentageOfStoreModification = 1.0;
                InitialMaximumPercentageOfStoreModification = 0.1;
                PossibleSwaps = 1;
                MaxSwaps = 4;
                LongTermScoreReInitDistance = 1000;
                PushInsertionVIDs = new int[] { 8, 7, 6, 4, 5, 3, 2, 1 };

                switch (method)
                {
                    case MethodType.ExtremePointInsertion:
                        {
                            if (handleTetris)
                            {
                                Tetris = true;
                                PieceOrder = PieceOrderType.VwH;
                                BestFit = false;
                                MeritType = MeritFunctionType.MEDXYZ;
                                Improvement = true;
                                PieceReorder = PieceReorderType.Score;
                                RandomSalt = 0.08368586690516754;
                                InflateAndReplaceInsertion = false;
                                NormalizationOrder = new DimensionMarker[3] { DimensionMarker.X, DimensionMarker.Y, DimensionMarker.Z };
                                ExhaustiveEPProne = false;
                                StagnationDistance = 3000;
                                MaximumPercentageOfStoreModification = 4.276000422314785;
                                InitialMaximumPercentageOfStoreModification = 0.12554562961303808;
                                PossibleSwaps = 1;
                                MaxSwaps = 8;
                                LongTermScoreReInitDistance = 623;
                                PushInsertionVIDs = new int[] { 8, 7, 6, 4, 5, 3, 2, 1 };
                            }
                            else
                            {
                                Tetris = false;
                                PieceOrder = PieceOrderType.VwH;
                                BestFit = true;
                                MeritType = MeritFunctionType.MFV;
                                Improvement = true;
                                PieceReorder = PieceReorderType.Score;
                                RandomSalt = 0.030644342734285967;
                                InflateAndReplaceInsertion = false;
                                NormalizationOrder = new DimensionMarker[3] { DimensionMarker.X, DimensionMarker.Y, DimensionMarker.Z };
                                ExhaustiveEPProne = false;
                                StagnationDistance = 3000;
                                MaximumPercentageOfStoreModification = 2.3622753818267688;
                                InitialMaximumPercentageOfStoreModification = 0.9173096014024229;
                                PossibleSwaps = 4;
                                MaxSwaps = 5;
                                LongTermScoreReInitDistance = 2001;
                                PushInsertionVIDs = new int[] { 8, 7, 6, 4, 5, 3, 2, 1 };
                            }
                        }
                        break;
                    case MethodType.SpaceDefragmentation:
                        {
                            Tetris = false;
                            PieceOrder = PieceOrderType.HWL;
                            BestFit = false;
                            MeritType = MeritFunctionType.MEDXY;
                            Improvement = true;
                            PieceReorder = PieceReorderType.Score;
                            RandomSalt = 0.8866072829180582;
                            InflateAndReplaceInsertion = true;
                            NormalizationOrder = new DimensionMarker[3] { DimensionMarker.X, DimensionMarker.Y, DimensionMarker.Z };
                            ExhaustiveEPProne = false;
                            StagnationDistance = 3000;
                            MaximumPercentageOfStoreModification = 2.3622753818267688;
                            InitialMaximumPercentageOfStoreModification = 1.0117912386757508;
                            PossibleSwaps = 1;
                            MaxSwaps = 7;
                            LongTermScoreReInitDistance = 691;
                            PushInsertionVIDs = new int[] { 8, 7, 6, 4, 5, 3, 2, 1 };
                        }
                        break;
                    case MethodType.PushInsertion:
                        {
                            if (handleTetris)
                            {
                                Tetris = true;
                                PieceOrder = PieceOrderType.V;
                                BestFit = false;
                                MeritType = MeritFunctionType.MEDXY;
                                Improvement = true;
                                PieceReorder = PieceReorderType.Score;
                                RandomSalt = 0.008158387839849155;
                                InflateAndReplaceInsertion = false;
                                NormalizationOrder = new DimensionMarker[3] { DimensionMarker.Z, DimensionMarker.X, DimensionMarker.Y };
                                ExhaustiveEPProne = false;
                                StagnationDistance = 3000;
                                MaximumPercentageOfStoreModification = 3.708327203520489;
                                InitialMaximumPercentageOfStoreModification = 0.01965795921601555;
                                PossibleSwaps = 3;
                                MaxSwaps = 4;
                                LongTermScoreReInitDistance = 75;
                                PushInsertionVIDs = new int[] { 8, 7, 6, 4 };
                            }
                            else
                            {
                                Tetris = false;
                                PieceOrder = PieceOrderType.HWL;
                                BestFit = false;
                                MeritType = MeritFunctionType.MEDXY;
                                Improvement = true;
                                PieceReorder = PieceReorderType.Score;
                                RandomSalt = 0.12794130221974132;
                                InflateAndReplaceInsertion = false;
                                NormalizationOrder = new DimensionMarker[3] { DimensionMarker.Z, DimensionMarker.Y, DimensionMarker.X };
                                ExhaustiveEPProne = false;
                                StagnationDistance = 3000;
                                MaximumPercentageOfStoreModification = 4.934867626876022;
                                InitialMaximumPercentageOfStoreModification = 0.06707718133633729;
                                PossibleSwaps = 3;
                                MaxSwaps = 5;
                                LongTermScoreReInitDistance = 861;
                                PushInsertionVIDs = new int[] { 8, 7, 6, 4, 5, 3, 2, 1 };
                            }
                        }
                        break;
                    case MethodType.ALNS:
                        {
                            if (handleTetris)
                            {
                                Tetris = true;
                                PieceOrder = PieceOrderType.VwH;
                                BestFit = false;
                                MeritType = MeritFunctionType.MEDXYZ;
                                Improvement = true;
                                PieceReorder = PieceReorderType.Score;
                                RandomSalt = 0.08368586690516754;
                                InflateAndReplaceInsertion = false;
                                NormalizationOrder = new DimensionMarker[3] { DimensionMarker.X, DimensionMarker.Y, DimensionMarker.Z };
                                ExhaustiveEPProne = false;
                                StagnationDistance = 3000;
                                MaximumPercentageOfStoreModification = 4.276000422314785;
                                InitialMaximumPercentageOfStoreModification = 0.12554562961303808;
                                PossibleSwaps = 1;
                                MaxSwaps = 8;
                                LongTermScoreReInitDistance = 623;
                                PushInsertionVIDs = new int[] { 8, 7, 6, 4, 5, 3, 2, 1 };
                            }
                            else
                            {
                                Tetris = false;
                                PieceOrder = PieceOrderType.VwH;
                                BestFit = true;
                                MeritType = MeritFunctionType.MFV;
                                Improvement = true;
                                PieceReorder = PieceReorderType.Score;
                                RandomSalt = 0.030644342734285967;
                                InflateAndReplaceInsertion = false;
                                NormalizationOrder = new DimensionMarker[3] { DimensionMarker.X, DimensionMarker.Y, DimensionMarker.Z };
                                ExhaustiveEPProne = false;
                                StagnationDistance = 3000;
                                MaximumPercentageOfStoreModification = 2.3622753818267688;
                                InitialMaximumPercentageOfStoreModification = 0.9173096014024229;
                                PossibleSwaps = 4;
                                MaxSwaps = 5;
                                LongTermScoreReInitDistance = 2001;
                                PushInsertionVIDs = new int[] { 8, 7, 6, 4, 5, 3, 2, 1 };
                            }
                        }
                        break;
                    case MethodType.FrontLeftBottomStyle:
                    case MethodType.TetrisStyle:
                    case MethodType.SpaceIndexed:
                    default: throw new ArgumentException("Unknown method type: " + method.ToString());
                }
            }
            else
            {
                Goal = OptimizationGoal.MaxUtilization;
                SolverToUse = Solvers.Gurobi;
            }
        }

        #endregion

        #region Global stuff

        /// <summary>
        /// Name of the configuration
        /// </summary>
        public string Name { get; set; } = "default";

        /// <summary>
        /// The type of the method to use
        /// </summary>
        public MethodType Type { get; set; } = MethodType.ExtremePointInsertion;

        /// <summary>
        /// Gets or sets the timelimit for the solution process
        /// </summary>
        [XmlIgnore, JsonIgnore]
        public TimeSpan TimeLimit { get; set; } = TimeSpan.FromMinutes(10);
        /// <summary>
        /// The timelimit for the solution process in seconds
        /// </summary>
        public double TimeLimitInSeconds { get { return TimeLimit.TotalSeconds; } set { TimeLimit = TimeSpan.FromSeconds(value); } }

        /// <summary>
        /// The maximum number of iterations for the solution process. A negative value indicates no limit.
        /// </summary>
        public int IterationsLimit { get; set; } = -1;

        /// <summary>
        /// Indicates whether to respect the gravity contraint or not
        /// </summary>
        public bool HandleGravity { get; set; } = false;

        /// <summary>
        /// Indicates whether to respect the compatibility of pieces when loading them into one container or not
        /// </summary>
        public bool HandleCompatibility { get; set; } = true;

        /// <summary>
        /// Indicates whether to respect the stackability of pieces or not
        /// </summary>
        public bool HandleStackability { get; set; } = false;

        /// <summary>
        /// Indicates whether to enable rotations or use only the default orientation
        /// </summary>
        public bool HandleRotatability { get; set; } = true;

        /// <summary>
        /// Indicates wheter to restrict the orientations of a piece to the feasible ones or not
        /// </summary>
        public bool HandleForbiddenOrientations { get; set; } = true;

        /// <summary>
        /// The seed for any kind of randomization
        /// </summary>
        public int Seed { get; set; } = 0;

        /// <summary>
        /// Defines a limit for the number of parallel threads allowed.
        /// </summary>
        public int ThreadLimit { get; set; } = 0;

        #endregion

        #region Intermediate fields

        /// <summary>
        /// Gets or sets basic log function to which all log messages get submitted
        /// </summary>
        [XmlIgnore]
        public Action<string> Log;

        /// <summary>
        /// Gets or sets the function which submits the current best solution
        /// </summary>
        [XmlIgnore]
        public Action<COSolution, bool, string, string> SubmitSolution;

        /// <summary>
        /// Used to log the current status of the solution. The first element is the time-stamp and the second one the incumbent value.
        /// </summary>
        [XmlIgnore]
        public Action<double, double> LogSolutionStatus;

        /// <summary>
        /// The time-stamp of the start of the solve process
        /// </summary>
        [XmlIgnore]
        public DateTime StartTimeStamp;

        #endregion

        #region Transformer related properties

        /// <summary>
        /// Defines the optimization goal
        /// </summary>
        public OptimizationGoal Goal { get; set; } = OptimizationGoal.MaxUtilization;

        /// <summary>
        /// Defines the solver to use
        /// </summary>
        public Solvers SolverToUse { get; set; } = Solvers.Gurobi;

        #endregion

        #region Heuristic related properties

        /// <summary>
        /// The interval at which the primal heuristic logs its progress.
        /// Since the log interval of the wrapping improvement phase is normally more interesting, this is turned off by default (0.0).
        /// </summary>
        public double PrimalHeuristicLogInterval { get; set; } = 0.0;

        /// <summary>
        /// Defines the objective.
        /// </summary>
        public ObjectiveType Objective { get; set; } = ObjectiveType.MaxVolume;

        /// <summary>
        /// Defines the type of the container sorting to apply at the beginning.
        /// </summary>
        public ContainerInitOrderType ContainerOrderInit { get; set; } = ContainerInitOrderType.Capacity;

        /// <summary>
        /// Defines the type of the container sorting to apply while improving.
        /// </summary>
        public ContainerReorderType ContainerOrderReorder { get; set; } = ContainerReorderType.None;

        /// <summary>
        /// Defines the fraction of containers to be used automatically (opened) instead of aiming to fill as few containers as possible.
        // The fraction is based on the volume of the pieces and the estimated volume of the containers.
        /// </summary>
        public double ContainerOpen { get; set; }

        /// <summary>
        /// Defines the initial order of the pieces
        /// </summary>
        public PieceOrderType PieceOrder { get; set; } = PieceOrderType.VwH;

        /// <summary>
        /// Defines whether to improve the initial solution
        /// </summary>
        public bool Improvement { get; set; } = true;

        /// <summary>
        /// Enables or disables Tetris-level insertion
        /// </summary>
        public bool Tetris { get; set; } = true;

        /// <summary>
        /// Defines whether to use the best available insertion point or the first valid one
        /// </summary>
        public bool BestFit { get; set; } = true;

        /// <summary>
        /// Deletes EPs more extensively
        /// </summary>
        public bool ExhaustiveEPProne { get; set; } = false;

        /// <summary>
        /// Defines how pieces are being reordered between iterations.
        /// </summary>
        public PieceReorderType PieceReorder { get; set; } = PieceReorderType.Score;

        /// <summary>
        /// Defines the merit function when best-fit is active
        /// </summary>
        public MeritFunctionType MeritType { get; set; } = MeritFunctionType.MEDXYZ;

        /// <summary>
        /// Defines a random salt applied to the score-based ordering
        /// </summary>
        public double RandomSalt { get; set; } = 0.1;

        /// <summary>
        /// Defines the normalization order (SD and PI only)
        /// </summary>
        public DimensionMarker[] NormalizationOrder { get; set; } = new DimensionMarker[3] { DimensionMarker.X, DimensionMarker.Y, DimensionMarker.Z };

        /// <summary>
        /// Defines the iteration count after which the improvement-phase terminates if not better solution is found
        /// </summary>
        public int StagnationDistance { get; set; } = 3000;

        /// <summary>
        /// p of GASP
        /// </summary>
        public double MaximumPercentageOfStoreModification { get; set; } = 1.0;

        /// <summary>
        /// s (overlined) of GASP
        /// </summary>
        public double InitialMaximumPercentageOfStoreModification { get; set; } = 0.1;

        /// <summary>
        /// k of GASP
        /// </summary>
        public int PossibleSwaps { get; set; } = 1;

        /// <summary>
        /// k_max of GASP
        /// </summary>
        public int MaxSwaps { get; set; } = 4;

        /// <summary>
        /// The iteration count after which a long term re-initialization is done if no improvement was found
        /// </summary>
        public int LongTermScoreReInitDistance { get; set; } = 600;

        /// <summary>
        /// Activates the insertion by replacing other pieces (SD only)
        /// </summary>
        public bool InflateAndReplaceInsertion { get; set; } = false;

        /// <summary>
        /// The vertex IDs of the container at which PI tries to insert pieces. The order is mandatory.
        /// </summary>
        public int[] PushInsertionVIDs { get; set; } = new int[] { 8, 7, 6, 4, 5, 3, 2, 1 };

        #endregion

        #region I/O

        /// <summary>
        /// Default extension for configurations
        /// </summary>
        public const string DEFAULT_EXTENSION = ".xconf";

        /// <summary>
        /// Writes the config to the specified file.
        /// </summary>
        /// <param name="path">The path to the file. (default extension will be added if missing)</param>
        public void Write(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
            if (!path.EndsWith(DEFAULT_EXTENSION)) { path = path + DEFAULT_EXTENSION; }
            using (TextWriter sw = new StreamWriter(path))
                serializer.Serialize(sw, this);
        }

        /// <summary>
        /// Reads the config from the file.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <returns>The config</returns>
        public static Configuration Read(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
            Configuration config;
            using (TextReader sr = new StreamReader(path))
                config = (Configuration)serializer.Deserialize(sr);
            return config;
        }

        #endregion
    }
}
