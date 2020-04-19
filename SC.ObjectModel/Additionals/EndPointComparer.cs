using SC.ObjectModel.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.ObjectModel.Additionals
{
    /// <summary>
    /// Supplies instances of all necessary endpoint comparers
    /// </summary>
    public class EndPointComparerSupply
    {
        /// <summary>
        /// The comparer for x
        /// </summary>
        public static EndPointComparerX ComparerX = new EndPointComparerX();

        /// <summary>
        /// The comparer for y
        /// </summary>
        public static EndPointComparerY ComparerY = new EndPointComparerY();

        /// <summary>
        /// The comparer for z
        /// </summary>
        public static EndPointComparerZ ComparerZ = new EndPointComparerZ();
    }

    #region Endpoint comparers

    /// <summary>
    /// Defines the order of endpoints regarding x
    /// </summary>
    public class EndPointComparerX : IComparer<MeshPoint>
    {
        #region IComparer<MeshPoint> Members

        /// <summary>
        /// Defines two points' order
        /// </summary>
        /// <param name="a">The first point</param>
        /// <param name="b">The second point</param>
        /// <returns>A value indicating the order of the two points</returns>
        public int Compare(MeshPoint a, MeshPoint b)
        {
            // Simply check if a is smaller than b
            if (a.X < b.X)
            {
                return -1;
            }
            else
            {
                // Simply check if a is greater than b
                if (a.X > b.X)
                {
                    return 1;
                }
                else
                {
                    // Check endpoint type
                    bool aIsLeftEndpoint = MeshConstants.VERTEX_IDS_LEFT_ENDPOINTS_X.Contains(a.VertexID);
                    bool bIsLeftEndpoint = MeshConstants.VERTEX_IDS_LEFT_ENDPOINTS_X.Contains(b.VertexID);

                    // If both values are the same check the point type - remark: it should not be of the same type
                    if (aIsLeftEndpoint == bIsLeftEndpoint)
                    {
                        return 0;
                    }
                    else
                    {
                        // If a is a left endpoint it is smaller
                        if (aIsLeftEndpoint)
                        {
                            return 1;
                        }
                        // If a is a right endpoint it is greater
                        else
                        {
                            return -1;
                        }
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Defines the order of endpoints regarding y
    /// </summary>
    public class EndPointComparerY : IComparer<MeshPoint>
    {
        #region IComparer<MeshPoint> Members

        /// <summary>
        /// Defines two points' order
        /// </summary>
        /// <param name="a">The first point</param>
        /// <param name="b">The second point</param>
        /// <returns>A value indicating the order of the two points</returns>
        public int Compare(MeshPoint a, MeshPoint b)
        {
            // Simply check if a is smaller than b
            if (a.Y < b.Y)
            {
                return -1;
            }
            else
            {
                // Simply check if a is greater than b
                if (a.Y > b.Y)
                {
                    return 1;
                }
                else
                {
                    // Check endpoint type
                    bool aIsLeftEndpoint = MeshConstants.VERTEX_IDS_LEFT_ENDPOINTS_Y.Contains(a.VertexID);
                    bool bIsLeftEndpoint = MeshConstants.VERTEX_IDS_LEFT_ENDPOINTS_Y.Contains(b.VertexID);

                    // If both values are the same check the point type - remark: it should not be of the same type
                    if (aIsLeftEndpoint == bIsLeftEndpoint)
                    {
                        return 0;
                    }
                    else
                    {
                        // If a is a left endpoint it is smaller
                        if (aIsLeftEndpoint)
                        {
                            return 1;
                        }
                        // If a is a right endpoint it is greater
                        else
                        {
                            return -1;
                        }
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Defines the order of endpoints regarding z
    /// </summary>
    public class EndPointComparerZ : IComparer<MeshPoint>
    {
        #region IComparer<MeshPoint> Members

        /// <summary>
        /// Defines two points' order
        /// </summary>
        /// <param name="a">The first point</param>
        /// <param name="b">The second point</param>
        /// <returns>A value indicating the order of the two points</returns>
        public int Compare(MeshPoint a, MeshPoint b)
        {
            // Simply check if a is smaller than b
            if (a.Z < b.Z)
            {
                return -1;
            }
            else
            {
                // Simply check if a is greater than b
                if (a.Z > b.Z)
                {
                    return 1;
                }
                else
                {
                    // Check endpoint type
                    bool aIsLeftEndpoint = MeshConstants.VERTEX_IDS_LEFT_ENDPOINTS_Z.Contains(a.VertexID);
                    bool bIsLeftEndpoint = MeshConstants.VERTEX_IDS_LEFT_ENDPOINTS_Z.Contains(b.VertexID);

                    // If both values are the same check the point type - remark: it should not be of the same type
                    if (aIsLeftEndpoint == bIsLeftEndpoint)
                    {
                        return 0;
                    }
                    else
                    {
                        // If a is a left endpoint it is smaller
                        if (aIsLeftEndpoint)
                        {
                            return 1;
                        }
                        // If a is a right endpoint it is greater
                        else
                        {
                            return -1;
                        }
                    }
                }
            }
        }
        #endregion
    }

    #endregion
}
