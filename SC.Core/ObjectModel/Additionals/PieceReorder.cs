using SC.Core.ObjectModel.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Core.ObjectModel.Additionals
{
    /// <summary>
    /// Defines the different piece-reorder types
    /// </summary>
    public enum PieceReorderType
    {
        /// <summary>
        /// Pieces are re-ordered based on their scores.
        /// </summary>
        Score,
        /// <summary>
        /// Pieces are re-ordered randomly.
        /// </summary>
        Random,
        /// <summary>
        /// Pieces are not re-ordered.
        /// </summary>
        None,
    }
}
