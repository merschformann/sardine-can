using SC.ObjectModel;
using SC.ObjectModel.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Heuristics.PrimalHeuristic
{
    /// <summary>
    /// Defines the basic skeletion of a point-insertion based primal heuristic
    /// </summary>
    public abstract partial class PointInsertionSkeleton : Heuristic
    {
        /// <summary>
        /// Creates a new point-insertion based method
        /// </summary>
        /// <param name="instance">The instance to solve</param>
        /// <param name="config">The configuration to use</param>
        public PointInsertionSkeleton(Instance instance, Configuration config) : base(instance, config) { Config = config; }

        /// <summary>
        /// Cancels the method
        /// </summary>
        public override void Cancel() => Cancelled = true;

        /// <summary>
        /// Checks whether the solution is optimal. (Since this is a heuristic method the answer will always be <code>false</code>)
        /// </summary>
        public override bool IsOptimal { get { return false; } }

        /// <summary>
        /// Checks whether a solution is available. (Since an empty solution is always valid, a valid solution is always available)
        /// </summary>
        public override bool HasSolution { get { return true; } }
    }
}
