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
    public class PushInsertion : PointInsertionSkeleton
    {
        #region Basics

        public PushInsertion(Instance instance, Configuration config) : base(instance, config) { }

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
            InsertAndPush(Solution, containers, pieces, orientationsPerPiece);

            // Improve solution
            if (Config.Improvement)
            {
                // Log visual
                LogVisuals(Solution, true);

                // Improve
                Solution = GASP(Solution, containers, pieces, orientationsPerPiece, InsertAndPush);
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
