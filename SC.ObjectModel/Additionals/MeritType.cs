using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.ObjectModel.Additionals
{
    /// <summary>
    /// Defines the different available merit functions
    /// </summary>
    public enum MeritFunctionType
    {
        /// <summary>
        /// No merit function is used
        /// </summary>
        None,

        /// <summary>
        /// Aims to minimize the free volume left after item accomodation
        /// </summary>
        MFV,

        /// <summary>
        /// Aims to minimize the size of the resulting packing regarding x and y
        /// </summary>
        MMPSXY,

        /// <summary>
        /// Exploits the <code>MinimizeMaximumPackingSizeXY</code> and also distinguishes between the positioning inside the packing area
        /// </summary>
        LPXY,

        /// <summary>
        /// Aims to maximize the utilization of the residual space at the insertion point
        /// </summary>
        MRSU,

        /// <summary>
        /// Aims to minimize the euclidean distance to the origin
        /// </summary>
        MEDXYZ,

        /// <summary>
        /// Aims to minimize the euclidean distance to the origin only regarding x and y
        /// </summary>
        MEDXY,

        /// <summary>
        /// Aims to minimize packing height
        /// </summary>
        H,
    }
}
