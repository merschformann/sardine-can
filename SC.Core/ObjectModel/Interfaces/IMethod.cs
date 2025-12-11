using SC.Core.ObjectModel.Additionals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Core.ObjectModel.Interfaces
{
    /// <summary>
    /// Defines the basic skeleton for every solve-method
    /// </summary>
    public interface IMethod
    {
        /// <summary>
        /// The configuration of the method.
        /// </summary>
        Configuration.Configuration Config { get; }

        /// <summary>
        /// The instance to solve
        /// </summary>
        Instance Instance { get; set; }

        /// <summary>
        /// Contains the obtained solution at least after the method finishes
        /// </summary>
        COSolution Solution { get; set; }

        /// <summary>
        /// The main execution method
        /// </summary>
        /// <returns>A basic performance result for the run</returns>
        PerformanceResult Run();

        /// <summary>
        /// Cancels an ongoing optimization process
        /// </summary>
        void Cancel();

        /// <summary>
        /// Reset Method
        /// </summary>
        void Reset();

        /// <summary>
        /// Indicates whether the solution is optimal
        /// </summary>
        bool IsOptimal { get; }

        /// <summary>
        /// Indicates whether a solution was already obtained
        /// </summary>
        bool HasSolution { get; }
    }
}
