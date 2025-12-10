using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Core.ObjectModel.Additionals
{
    /// <summary>
    /// Supplies types of basic shapes to differentiate between them
    /// </summary>
    public enum ShapeType
    {
        /// <summary>
        /// A "L"-shaped piece
        /// </summary>
        L,

        /// <summary>
        /// A "T"-shaped piece
        /// </summary>
        T,

        /// <summary>
        /// A "U"-shaped piece
        /// </summary>
        U,

        /// <summary>
        /// A simple cuboid
        /// </summary>
        Box
    }
}
