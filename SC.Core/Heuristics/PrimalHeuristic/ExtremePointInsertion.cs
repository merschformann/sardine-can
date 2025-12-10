using SC.Core.Heuristics.PrimalHeuristic;
using SC.Core.ObjectModel;
using SC.Core.ObjectModel.Additionals;
using SC.Core.ObjectModel.Configuration;
using SC.Core.ObjectModel.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SC.Core.Heuristics.PrimalHeuristic
{
    /// <summary>
    /// The extreme point (EP) insertion constructive heuristic with optional improvement stage
    /// </summary>
    public class ExtremePointInsertionHeuristic : PointInsertionSkeleton
    {
        #region Basics

        /// <summary>
        /// Creates a new EP heuristic
        /// </summary>
        /// <param name="instance">The instance to solve</param>
        /// <param name="config">The config to use</param>
        public ExtremePointInsertionHeuristic(Instance instance, Configuration config) : base(instance, config) { }

        /// <summary>
        /// Solves the instance
        /// </summary>
        protected override void Solve()
        {
            // Initialize meta-info
            VolumeOfContainers = Instance.Containers.Sum(c => c.Mesh.Volume);

            // Init ordering
            List<Container> containers = null;
            List<VariablePiece> pieces = null;
            int[][] orientationsPerPiece = null;
            Init(out containers, out pieces, out orientationsPerPiece);

            // Generate initial solution
            ExtremePointInsertion(Solution, containers, pieces, orientationsPerPiece);

            // Improve solution
            if (Config.Improvement)
            {
                // Log visual
                LogVisuals(Solution, true);

                // Improve
                Solution = GASP(Solution, containers, pieces, orientationsPerPiece, ExtremePointInsertion);
            }

            // Log
            if (Config.Log != null)
            {
                Config.Log("EPs available: " + Solution.ExtremePoints.Sum(kvp => kvp.Count) + "\n");
                Config.Log(Solution.VolumeContained.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + " / " +
                    VolumeOfContainers.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + "\n");
            }
        }

        #endregion
    }
}
