using SC.Core.ObjectModel.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Core.ObjectModel.Additionals
{
    /// <summary>
    /// Defines the different piece-order types
    /// </summary>
    public enum PieceOrderType
    {
        /// <summary>
        /// Pieces are ordered by their volume
        /// </summary>
        V,

        /// <summary>
        /// Pieces are ordered by their height then by their width then by their length
        /// </summary>
        HWL,

        /// <summary>
        /// Pieces are ordered by their length with a height tie-breaker
        /// </summary>
        VwH,

        /// <summary>
        /// Pieces are ordered by their height with a volume tie-breaker
        /// </summary>
        HwV,

        /// <summary>
        /// Pieces are ordered by their contact face with a height tie-breaker
        /// </summary>
        AwH,

        /// <summary>
        /// Pieces are ordered by their height with an area tie-breaker
        /// </summary>
        HwA
    }

    /// <summary>
    /// Supplies objects used for piece-ordering
    /// </summary>
    public class PieceOrderSupply
    {
        public static List<VariablePiece> Sort(List<VariablePiece> pieces, PieceOrderType type)
        {
            switch (type)
            {
                case PieceOrderType.V:
                    return pieces.OrderByDescending(p => p.Volume).ToList();
                case PieceOrderType.HWL:
                    return pieces.OrderByDescending(p => p.Original.BoundingBox.Height)
                        .ThenByDescending(p => p.Original.BoundingBox.Width)
                        .ThenByDescending(p => p.Original.BoundingBox.Length).ToList();
                case PieceOrderType.VwH:
                    return pieces.OrderByDescending(p => p.Volume)
                        .ThenByDescending(p => p.Original.BoundingBox.Height).ToList();
                case PieceOrderType.HwV:
                    return pieces.OrderByDescending(p => p.Original.BoundingBox.Height)
                        .ThenByDescending(p => p.Volume).ToList();
                case PieceOrderType.AwH:
                    return pieces.OrderByDescending(p => p.Original.BoundingBox.Length * p.Original.BoundingBox.Width)
                        .ThenByDescending(p => p.Original.BoundingBox.Height).ToList();
                case PieceOrderType.HwA:
                    return pieces.OrderByDescending(p => p.Original.BoundingBox.Height)
                        .ThenByDescending(p => p.Original.BoundingBox.Length * p.Original.BoundingBox.Width).ToList();
                default:
                    return pieces.OrderByDescending(p => p.Volume).ToList();
            }
        }
    }
}
