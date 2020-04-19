using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.GUI
{
    /// <summary>
    /// Defines different coloring methods to use when drawing instances
    /// </summary>
    enum ColoringMode
    {
        /// <summary>
        /// Simple random coloring.
        /// </summary>
        Random,
        /// <summary>
        /// Class dependent coloring to emphasize the different material-classes of the items
        /// </summary>
        ClassDependent,
        /// <summary>
        /// Wireframe coloring
        /// </summary>
        WireFrame,
        /// <summary>
        /// Coloring distinguishes n-th flag value of the pieces.
        /// </summary>
        Flag0,
        /// <summary>
        /// Coloring distinguishes n-th flag value of the pieces.
        /// </summary>
        Flag1,
        /// <summary>
        /// Coloring distinguishes n-th flag value of the pieces.
        /// </summary>
        Flag2,
        /// <summary>
        /// Coloring distinguishes n-th flag value of the pieces.
        /// </summary>
        Flag3,
    }
    /// <summary>
    /// Defines the different color pallets that can be used.
    /// </summary>
    enum ColoringPallet
    {
        /// <summary>
        /// Coloring in beamer-compatible colors
        /// </summary>
        Beamer,
        /// <summary>
        /// Full coloring with many different colors
        /// </summary>
        Full,
    }
}
