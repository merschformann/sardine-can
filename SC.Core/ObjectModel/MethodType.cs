using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Core.ObjectModel
{
    /// <summary>
    /// Defines the method used for solving
    /// </summary>
    public enum MethodType
    {
        /// <summary>
        /// The basic extreme point insertion heuristic
        /// </summary>
        ExtremePointInsertion,

        /// <summary>
        /// The space defragmentation heuristic
        /// </summary>
        SpaceDefragmentation,

        /// <summary>
        /// The new method
        /// </summary>
        PushInsertion,

        /// <summary>
        /// Indicates that the ALNS meta-heuristic will be used for solving
        /// </summary>
        ALNS,

        /// <summary>
        /// Indicates that the FLB model shall get solved
        /// </summary>
        FrontLeftBottomStyle,

        /// <summary>
        /// Indicates that the tetris model shall get solved
        /// </summary>
        TetrisStyle,

        /// <summary>
        /// Indicates that the hybrid model shall get solved
        /// </summary>
        HybridStyle,

        /// <summary>
        /// Indicates that the space-indexed model shall get solved
        /// </summary>
        SpaceIndexed
    }
}
