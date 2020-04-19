using SC.ObjectModel.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.ObjectModel.Additionals
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
        /// <summary>
        /// Supplies an instance of the respective order-type
        /// </summary>
        public static PieceVolumeComparer Volume = new PieceVolumeComparer();

        /// <summary>
        /// Supplies an instance of the respective order-type
        /// </summary>
        public static PieceHWLComparer HeightWidthLength = new PieceHWLComparer();

        /// <summary>
        /// Supplies an instance of the respective order-type
        /// </summary>
        public static PieceVolumeComparerWithHeightTieBreaker VolumeWithHeightTieBreaker = new PieceVolumeComparerWithHeightTieBreaker();

        /// <summary>
        /// Supplies an instance of the respective order-type
        /// </summary>
        public static PieceHeightComparerWithVolumeTieBreaker HeightWithVolumeTieBreaker = new PieceHeightComparerWithVolumeTieBreaker();

        /// <summary>
        /// Supplies an instance of the respective order-type
        /// </summary>
        public static PieceAreaComparerWithHeightTieBreaker AreaWithHeightTieBreaker = new PieceAreaComparerWithHeightTieBreaker();

        /// <summary>
        /// Supplies an instance of the respective order-type
        /// </summary>
        public static PieceHeightComparerWithAreaTieBreaker HeightWithAreaTieBreaker = new PieceHeightComparerWithAreaTieBreaker();

        /// <summary>
        /// Returns a comparer object by type
        /// </summary>
        /// <param name="type">The comparer type</param>
        /// <returns>The object belonging to the desired type</returns>
        public static IComparer<Piece> GetComparerByType(PieceOrderType type)
        {
            switch (type)
            {
                case PieceOrderType.V:
                    return Volume;
                case PieceOrderType.HWL:
                    return HeightWidthLength;
                case PieceOrderType.VwH:
                    return VolumeWithHeightTieBreaker;
                case PieceOrderType.HwV:
                    return HeightWithVolumeTieBreaker;
                case PieceOrderType.AwH:
                    return AreaWithHeightTieBreaker;
                case PieceOrderType.HwA:
                    return HeightWithAreaTieBreaker;
                default:
                    return VolumeWithHeightTieBreaker;
            }
        }
    }

    #region Piece comparers

    /// <summary>
    /// Defines the respective comparer
    /// </summary>
    public class PieceVolumeComparer : IComparer<Piece>
    {

        #region IComparer<Piece> Members

        public int Compare(Piece x, Piece y)
        {
            // Check if y has more volume then x
            if (x.Volume < y.Volume)
            {
                return -1;
            }
            else
            {
                // Check if x has more volume then y
                if (x.Volume > y.Volume)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Defines the respective comparer
    /// </summary>
    public class PieceHWLComparer : IComparer<Piece>
    {

        #region IComparer<Piece> Members

        public int Compare(Piece x, Piece y)
        {
            // Check if y is taller then x
            if (x.Original.BoundingBox.Height < y.Original.BoundingBox.Height)
            {
                return -1;
            }
            else
            {
                // Check if x is taller then y
                if (x.Original.BoundingBox.Height > y.Original.BoundingBox.Height)
                {
                    return 1;
                }
                else
                {
                    // Check if y is wider then x
                    if (x.Original.BoundingBox.Width < y.Original.BoundingBox.Width)
                    {
                        return -1;
                    }
                    else
                    {
                        // Check if x is wider then y
                        if (x.Original.BoundingBox.Width > y.Original.BoundingBox.Width)
                        {
                            return 1;
                        }
                        else
                        {
                            // Check if y is longer then x
                            if (x.Original.BoundingBox.Length < y.Original.BoundingBox.Length)
                            {
                                return -1;
                            }
                            else
                            {
                                // Check if x is longer then y
                                if (x.Original.BoundingBox.Length > y.Original.BoundingBox.Length)
                                {
                                    return 1;
                                }
                                else
                                {
                                    return 0;
                                }
                            }
                        }

                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Defines the respective comparer
    /// </summary>
    public class PieceVolumeComparerWithHeightTieBreaker : IComparer<Piece>
    {
        #region IComparer<Piece> Members

        public int Compare(Piece x, Piece y)
        {
            // Check if y has more volume then x
            if (x.Volume < y.Volume)
            {
                return -1;
            }
            else
            {
                // Check if x has more volume then y
                if (x.Volume > y.Volume)
                {
                    return 1;
                }
                else
                {
                    // Check if y is taller then x
                    if (x.Original.BoundingBox.Height < y.Original.BoundingBox.Height)
                    {
                        return -1;
                    }
                    else
                    {
                        // Check if x is taller then y
                        if (x.Original.BoundingBox.Height > y.Original.BoundingBox.Height)
                        {
                            return 1;
                        }
                        else
                        {
                            return 0;
                        }

                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Defines the respective comparer
    /// </summary>
    public class PieceHeightComparerWithVolumeTieBreaker : IComparer<Piece>
    {

        #region IComparer<Piece> Members

        public int Compare(Piece x, Piece y)
        {
            // Check if y is taller then x
            if (x.Original.BoundingBox.Height < y.Original.BoundingBox.Height)
            {
                return -1;
            }
            else
            {
                // Check if x is taller then y
                if (x.Original.BoundingBox.Height > y.Original.BoundingBox.Height)
                {
                    return 1;
                }
                else
                {
                    // Check if y has more volume then x
                    if (x.Volume < y.Volume)
                    {
                        return -1;
                    }
                    else
                    {
                        // Check if x has more volume then y
                        if (x.Volume > y.Volume)
                        {
                            return 1;
                        }
                        else
                        {
                            return 0;
                        }

                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Defines the respective comparer
    /// </summary>
    public class PieceAreaComparerWithHeightTieBreaker : IComparer<Piece>
    {

        #region IComparer<Piece> Members

        public int Compare(Piece x, Piece y)
        {
            // Check if y is taller then x
            if (x.Original.BoundingBox.Length * x.Original.BoundingBox.Width < y.Original.BoundingBox.Length * y.Original.BoundingBox.Width)
            {
                return -1;
            }
            else
            {
                // Check if x is taller then y
                if (x.Original.BoundingBox.Length * x.Original.BoundingBox.Width > y.Original.BoundingBox.Length * y.Original.BoundingBox.Width)
                {
                    return 1;
                }
                else
                {
                    // Check if y has more volume then x
                    if (x.Original.BoundingBox.Height < y.Original.BoundingBox.Height)
                    {
                        return -1;
                    }
                    else
                    {
                        // Check if x has more volume then y
                        if (x.Original.BoundingBox.Height > y.Original.BoundingBox.Height)
                        {
                            return 1;
                        }
                        else
                        {
                            return 0;
                        }

                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Defines the respective comparer
    /// </summary>
    public class PieceHeightComparerWithAreaTieBreaker : IComparer<Piece>
    {

        #region IComparer<Piece> Members

        public int Compare(Piece x, Piece y)
        {
            // Check if y is taller then x
            if (x.Original.BoundingBox.Height < y.Original.BoundingBox.Height)
            {
                return -1;
            }
            else
            {
                // Check if x is taller then y
                if (x.Original.BoundingBox.Height > y.Original.BoundingBox.Height)
                {
                    return 1;
                }
                else
                {
                    // Check if y has more volume then x
                    if (x.Original.BoundingBox.Length * x.Original.BoundingBox.Width < y.Original.BoundingBox.Length * y.Original.BoundingBox.Width)
                    {
                        return -1;
                    }
                    else
                    {
                        // Check if x has more volume then y
                        if (x.Original.BoundingBox.Length * x.Original.BoundingBox.Width > y.Original.BoundingBox.Length * y.Original.BoundingBox.Width)
                        {
                            return 1;
                        }
                        else
                        {
                            return 0;
                        }

                    }
                }
            }
        }

        #endregion
    }

    #endregion
}
