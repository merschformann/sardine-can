using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.ObjectModel.Additionals
{
    /// <summary>
    /// Defines different handling instructions
    /// </summary>
    public enum HandlingInstructions
    {
        /// <summary>
        /// Default handling, no further remarks
        /// </summary>
        Default,

        /// <summary>
        /// Indicates that the correpsonding piece shall be kept up-oriented at all times
        /// </summary>
        ThisSideUp,

        /// <summary>
        /// Indicates that the corresponding piece is not stackable and must not be encumbered
        /// </summary>
        NotStackable,

        /// <summary>
        /// Indicates that the corresponding piece is not stackable and shall always be kept up-oriented
        /// </summary>
        Fragile
    }
}
