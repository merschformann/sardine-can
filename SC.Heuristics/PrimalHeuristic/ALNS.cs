using SC.ObjectModel;
using SC.ObjectModel.Additionals;
using SC.ObjectModel.Configuration;
using SC.ObjectModel.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Heuristics.PrimalHeuristic
{
    public class ALNS : PointInsertionSkeleton
    {
        /// <summary>
        /// Creates a new point-insertion based method
        /// </summary>
        /// <param name="instance">The instance to solve</param>
        /// <param name="config">The configuration to use</param>
        public ALNS(Instance instance, Configuration config) : base(instance, config) { }

        #region Backbone

        protected override void Solve()
        {
            // --> Initialization
            // Initialize meta-info
            VolumeOfContainers = Instance.Containers.Sum(c => c.Mesh.Volume);

            // Init ordering
            List<VariablePiece> pieces = null;
            Init(out Solution.ConstructionContainerOrder, out pieces, out Solution.ConstructionOrientationOrder);

            // --> Construction part
            // Log
            Config.Log?.Invoke("Starting construction ... " + Environment.NewLine);
            // Generate initial solution
            ExtremePointInsertion(Solution, Solution.ConstructionContainerOrder, pieces, Solution.ConstructionOrientationOrder);

            // --> Improvement part
            // Log
            Config.Log?.Invoke("Starting improvement ... " + Environment.NewLine);

            // Measure performance compared to given solution
            double initialExploitedVolume = Solution.ExploitedVolume;

            // TODO move the following into the config
            int intervalIterationCountMax = 100;
            double alpha = 0.9;

            // Init counters
            int currentIteration = 0;
            int lastImprovement = 0;
            double currentTemperature = 1000;
            int currentIntervalIteration = 0;

            // Init solutions
            COSolution acceptedSolution = Solution.Clone();

            // Main loop
            do
            {
                // Init solution for this turn
                COSolution currentSolution = acceptedSolution.Clone();

                // Destroy the solution
                ALNSDestroy(currentSolution);

                // Repair the solution
                ALNSRepair(currentSolution);

                // Check whether we want to accept the solution or discard it
                if (ALNSAccept(acceptedSolution.ExploitedVolume, currentSolution.ExploitedVolume, Solution.ExploitedVolume, currentTemperature))
                    acceptedSolution = currentSolution;

                // Lower temperature if iteration limit is reached
                if (currentIntervalIteration >= intervalIterationCountMax)
                {
                    currentIntervalIteration = 0;
                    currentTemperature *= alpha;
                }

                // Store every new best solution
                if (acceptedSolution.ExploitedVolume > Solution.ExploitedVolume)
                    Solution = acceptedSolution;

                // Log visuals
                LogVisuals(Solution, false);

                // Log
                if (DateTime.Now.Ticks - LogOldMillis > 1000000)
                {
                    LogOldMillis = DateTime.Now.Ticks;
                    Config.Log?.Invoke(currentIteration + ". " +
    Solution.ExploitedVolume.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + " / " +
    VolumeOfContainers.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
    " (" + (Solution.ExploitedVolume / VolumeOfContainers * 100).ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + " %) - Current: " +
    acceptedSolution.ExploitedVolume.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + " / " +
    VolumeOfContainers.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
    " (" + ((Solution.ExploitedVolume / VolumeOfContainers - initialExploitedVolume / VolumeOfContainers) * 100).ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + " %)" +
    " Time: " + (DateTime.Now - Config.StartTimeStamp).TotalSeconds +
    " \n");
                    Config.LogSolutionStatus?.Invoke((DateTime.Now - Config.StartTimeStamp).TotalSeconds, Solution.ExploitedVolume);
                }

                // Update counters
                currentIteration++;
                currentIntervalIteration++;

            } while (currentIteration - lastImprovement < Config.StagnationDistance && !Cancelled && !TimeUp);
        }

        #endregion

        #region ALNS methods

        public enum RepairStrategy
        {
            RandomPieceOrder,
            RandomOrientationOrder,
        }

        private void ALNSRepair(COSolution solution)
        {
            // Decide randomly for now
            RepairStrategy repairStrategy = Randomizer.NextDouble() > 0.5 ?
                RepairStrategy.RandomOrientationOrder :
                RepairStrategy.RandomPieceOrder;

            switch (repairStrategy)
            {
                case RepairStrategy.RandomPieceOrder:
                    {
                        // Only order the offload pieces randomly
                        ExtremePointInsertion(solution,
                            solution.ConstructionContainerOrder,
                            solution.OffloadPieces.OrderBy(p => Randomizer.NextDouble()).ToList(),
                            solution.ConstructionOrientationOrder);
                    }
                    break;
                case RepairStrategy.RandomOrientationOrder:
                    {
                        // Order orientations randomly
                        int[][] randomOrientations = solution.ConstructionOrientationOrder.Select(p => p.OrderBy(r => Randomizer.NextDouble()).ToArray()).ToArray();
                        ExtremePointInsertion(solution,
                            solution.ConstructionContainerOrder,
                            solution.OffloadPieces.ToList(),
                            randomOrientations);
                    }
                    break;
                default: throw new ArgumentException("Unknown repair strategy: " + repairStrategy.ToString());
            }
        }

        private void ALNSDestroy(COSolution solution)
        {
            ALNSDestroyRandomContainer(solution);
        }

        private void ALNSDestroyRandomContainer(COSolution solution)
        {
            // Remove all pieces from a random container
            solution.RemoveContainer(Instance.Containers.OrderBy(c => Randomizer.Next(Instance.Containers.Count)).First());
        }

        private void DestroyContainerExploitedVolumeBased(COSolution solution, double relativeContainerIndex)
        {
            // Remove all pieces from the container with the index matching the given relative position when ordered by exploited relative volume
            IEnumerable<Container> usedContainers = Instance.Containers.Where(c => solution.ExploitedVolumeOfContainers[c.VolatileID] > 0);
            int containerIndex = (int)(relativeContainerIndex * usedContainers.Count());
            Container containerToDestroy = usedContainers.OrderByDescending(c => solution.ExploitedVolumeOfContainers[c.VolatileID] / c.Mesh.Volume).ElementAt(containerIndex);
            solution.RemoveContainer(containerToDestroy);
        }

        private bool ALNSAccept(double last, double current, double best, double currentTemperature)
        {
            // Accept all solutions better than the current best
            if (current > best) return true;
            // Determine probability for solution acceptance
            double probability = Math.Pow(Math.E, -((current - last) / currentTemperature));
            // Randomly accept
            if (Randomizer.NextDouble() < probability)
                return true;
            else
                return false;
        }

        #endregion
    }
}
