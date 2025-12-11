using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Core.ObjectModel.Configuration
{
    /// <summary>
    /// Indicates the goal of the optimization.
    /// </summary>
    public enum OptimizationGoal
    {
        /// <summary>
        /// Minimizes the count of the used containers
        /// </summary>
        MinContainer,

        /// <summary>
        /// Maximizes the used space
        /// </summary>
        MaxUtilization
    }
}
