using System.Threading.Tasks;
using SC.ObjectModel;
using SC.ObjectModel.Additionals;
using SC.ObjectModel.Interfaces;
using SC.Preprocessing.PreprocessingMethods;
using SC.Preprocessing.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using SC.ObjectModel.Configuration;
using Atto.LinearWrap;

namespace SC.Linear
{
    /// <summary>
    /// The basic structure of a model builder used to transform the object-model into a solver representation
    /// </summary>
    public abstract class LinearModelBase : IMethod
    {
        /// <summary>
        /// Creates a new model
        /// </summary>
        /// <param name="instance">The instance to solve</param>
        /// <param name="config">The config to use</param>
        public LinearModelBase(Instance instance, Configuration config)
        {
            Instance = instance;
            Config = config;
            PreprocessorSteps = null;
        }

        #region Auxiliary methods

        /// <summary>
        /// The solver chosen for handling optimization.
        /// </summary>
        protected SolverType ChosenSolver
        {
            get
            {
                return Config.SolverToUse switch
                {
                    Solvers.Cplex => SolverType.CPLEX,
                    Solvers.Gurobi => SolverType.Gurobi,
                    _ => throw new ArgumentException($"Unknown solver: {Config.SolverToUse}"),
                };
            }
        }

        #endregion

        #region IMethod Members

        /// <summary>
        /// The instance to solve
        /// </summary>
        public Instance Instance { get; set; }

        /// <summary>
        /// The achieved solution
        /// </summary>
        public COSolution Solution { get; set; }

        /// <summary>
        /// The config to use
        /// </summary>
        public Configuration Config { get; set; }

        /// <summary>
        /// Preprocessor
        /// </summary>
        public List<IPreprocessorStep> PreprocessorSteps { get; set; }

        /// <summary>
        /// preprocessor Methods
        /// </summary>
        private Dictionary<Type, IPreprocessorMethod> _preprocessingMethods;

        /// <summary>
        /// reset method
        /// </summary>
        public void Reset()
        {
            HasSolution = false;
            PreprocessorSteps = null;
            Solution = null;
        }

        /// <summary>
        /// Indicates whether the solution is optimal
        /// </summary>
        public bool IsOptimal { get; private set; } = false;

        /// <summary>
        /// Indicates whether a solution is available
        /// </summary>
        public bool HasSolution { get; private set; } = false;

        /// <summary>
        /// Cancels the process
        /// </summary>
        public void Cancel()
        {

            //cancel preprocessors
            if (PreprocessorSteps != null)
            {
                foreach (var step in PreprocessorSteps)
                    _preprocessingMethods[step.GetMethodType()].Cancel();
            }

            //cancel solver
            if (_solver != null)
            {
                Action cancelAction = CancelSolver;
                Task.Run(cancelAction);
            }
        }

        /// <summary>
        /// cancel the solver
        /// </summary>
        private void CancelSolver()
        {
            if (_solver != null)
                _solver.Abort();

            //wait for the solver to cancel
            Task.Delay(1000);

            var solver = _solver;

            //if the solver is still up -> try again
            if (solver != null && _solver.IsBusy())
            {
                Action cancelAction = CancelSolver;
                Task.Run(cancelAction);
            }
        }

        /// <summary>
        /// Starts the progress
        /// </summary>
        /// <param name="outputAfterwards">Indicates whether to output additional information after the process</param>
        /// <returns>Statistics about the process</returns>
        public PerformanceResult Run()
        {
            IsOptimal = false;
            Config.StartTimeStamp = DateTime.Now;

            // Log first timestamp
            Config.LogSolutionStatus?.Invoke((DateTime.Now - Config.StartTimeStamp).TotalSeconds, 0);

            //Preprocessing Start
            //preprocessing, if a preprocessor is supplied
            //This will normaly change the instance.
            _preprocessingMethods = new Dictionary<Type, IPreprocessorMethod>();
            if (PreprocessorSteps != null && PreprocessorSteps.Count > 0)
            {
                //init
                foreach (var step in PreprocessorSteps)
                {
                    if (!_preprocessingMethods.ContainsKey(step.GetMethodType()))
                    {
                        _preprocessingMethods.Add(step.GetMethodType(), step.GetNewMethodInstance());
                        _preprocessingMethods[step.GetMethodType()].InitPreprocessing(Instance, Config);
                    }
                }

                //call steps
                foreach (var step in PreprocessorSteps)
                    _preprocessingMethods[step.GetMethodType()].Preprocessing(step);

                InstanceModificator.GenerateNewPieceNonvolatileIds(Instance);
                //recreate solution
                //Solution = Instance.CreateSolution(((PointInsertionConfiguration)Config).Tetris, ((PointInsertionConfiguration)Config).MeritType);
            }

            LinearModel model = Transform();
            model.SetTimelimit(Config.TimeLimit);
            model.SetThreadLimit(Config.ThreadLimit);
            _solver = model;
            // Add the callbacks
            if (Config.SubmitSolution != null)
                // Submit solution
                _solver.StatusCallback.NewIncumbent = () => { TransformIntermediateSolution(); Config.SubmitSolution(Solution, true, ExportationConstants.ExportDir, Instance.GetIdent()); };
            if (Config.LogSolutionStatus != null)
                // Log objective value
                _solver.StatusCallback.LogIncumbent = (double incumbent) => { Config.LogSolutionStatus((DateTime.Now - Config.StartTimeStamp).TotalSeconds, Math.Abs(incumbent)); };

            // --> Optimize
            _solver.Optimize();

            // Get the results
            if (_solver.HasSolution())
            {
                Retransform();
                // Log if desired
                Instance.OutputInfo(Console.Out);
                HasSolution = true;
            }
            // Create trivial solution in case solver did not find any
            if (Solution == null)
            {
                Solution = Instance.CreateSolution(true, MeritFunctionType.None);
            }
            // Set additional info
            if (Solution != null)
            {
                HasSolution = true;
            }
            if (_solver.IsOptimal())
            {
                IsOptimal = true;
            }

            // Measure time and get further results directly from the solver
            DateTime after = DateTime.Now;
            double bestBound = double.NaN;
            double gap = double.NaN;
            double objective = double.NaN;
            try { objective = _solver.GetObjectiveValue(); } catch (Exception) { }
            try { bestBound = _solver.GetBestBound(); } catch (Exception) { }
            try { gap = Math.Abs(_solver.GetGap()); } catch (Exception) { }

            //Preprocessing End
            //Decompose
            if (PreprocessorSteps != null)
            {
                InstanceModificator.DecomposePreprocessedPieces(Solution);
                foreach (var method in _preprocessingMethods.Values)
                    method.Dispose();
            }

            // Build the result
            PerformanceResult result = new PerformanceResult()
            {
                Instance = Instance,
                Solution = Solution,
                SolutionTime = after - Config.StartTimeStamp,
                ObjectiveValue = objective,
                BestBound = bestBound,
                Gap = gap
            };

            // Log final timestamp
            Config.LogSolutionStatus?.Invoke((DateTime.Now - Config.StartTimeStamp).TotalSeconds, Solution.ExploitedVolume);

            // Return the result
            return result;
        }

        #endregion

        #region Basic transformer methods declaration

        /// <summary>
        /// Transforms the object-model into a mathematical formulation
        /// </summary>
        /// <returns>The model</returns>
        internal abstract LinearModel Transform();

        /// <summary>
        /// Transforms the solution back into an object-model representation
        /// </summary>
        internal abstract void TransformSolution();

        /// <summary>
        /// Transforms an intermediate solution back into an object-model representation
        /// </summary>
        internal abstract void TransformIntermediateSolution();

        /// <summary>
        /// Writes solution information to a writer
        /// </summary>
        /// <param name="tw">The TextWriter to write to</param>
        /// <param name="solution">The solution to output</param>
        internal abstract void PrintSolution(TextWriter tw);

        /// <summary>
        /// The solver used
        /// </summary>
        private LinearModel _solver;

        /// <summary>
        /// Retransforms a solution at the end of the optimization process
        /// </summary>
        /// <param name="solution">The obtained solution</param>
        internal void Retransform()
        {
            TransformSolution();
        }

        #endregion

        #region Exportation methods

        /// <summary>
        /// Exports the model to a MPS-file
        /// </summary>
        /// <param name="filePath">The path to the MPS-file</param>
        public void ExportMPS(string filePath)
        {
            LinearModel model = Transform();
            model.ExportMPS(filePath);
        }

        #endregion
    }
}
