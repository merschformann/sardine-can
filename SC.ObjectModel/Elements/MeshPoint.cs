using SC.ObjectModel.Additionals;
using SC.ObjectModel.Interfaces;
using SC.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SC.ObjectModel.Elements
{
    /// <summary>
    /// Defines a basic three-dimensional point
    /// </summary>
    public class MeshPoint : IEquatable<MeshPoint>, IDeepCloneable<MeshPoint>, IXmlSerializable
    {
        /// <summary>
        /// The x-value of the point
        /// </summary>
        public double X;

        /// <summary>
        /// The y-value of the point
        /// </summary>
        public double Y;

        /// <summary>
        /// The z-value of the point
        /// </summary>
        public double Z;

        /// <summary>
        /// An optional field which indicates the vertex ID this point corresponds to
        /// </summary>
        public int VertexID;

        /// <summary>
        /// The volatile ID of the point used for fast access
        /// </summary>
        public int VolatileID;

        /// <summary>
        /// An optional field which contains the piece this point belongs to
        /// </summary>
        public Piece ParentPiece;

        /// <summary>
        /// Compares two points for equality
        /// </summary>
        /// <param name="p1">The first point</param>
        /// <param name="p2">The second point</param>
        /// <returns>The result of the comparison. <code>true</code> if equal, <code>false</code> otherwise</returns>
        public static bool operator ==(MeshPoint p1, MeshPoint p2)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(p1, p2))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)p1 == null) || ((object)p2 == null))
            {
                return false;
            }
            // Compare for equality
            if (p1.X == p2.X && p1.Y == p2.Y && p1.Z == p2.Z)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Compares two points for equality
        /// </summary>
        /// <param name="p1">The first point</param>
        /// <param name="p2">The second point</param>
        /// <returns>The result of the comparison. <code>true</code> if equal, <code>false</code> otherwise</returns>
        public static MeshPoint operator +(MeshPoint p1, MeshPoint p2)
        {
            var sum = new MeshPoint{ X = p1.X + p2.X, Y = p1.Y + p2.Y, Z = p1.Z + p2.Z};
            return sum;
        }

        /// <summary>
        /// Compares two points for inequality
        /// </summary>
        /// <param name="p1">The first point</param>
        /// <param name="p2">The second point</param>
        /// <returns>The result of the comparison. <code>true</code> if unequal, <code>false</code> otherwise</returns>
        public static bool operator !=(MeshPoint p1, MeshPoint p2)
        {
            return !(p1 == p2);
        }

        /// <summary>
        /// Compares the given point to this point for equality
        /// </summary>
        /// <param name="obj">The given point</param>
        /// <returns>The result of the comparison. <code>true</code> if equal, <code>false</code> otherwise</returns>
        public override bool Equals(object obj)
        {
            return this == (MeshPoint)obj;
        }

        /// <summary>
        /// Calculates a hashcode for this point
        /// </summary>
        /// <returns>The hashcode</returns>
        public override int GetHashCode()
        {
            return (int)(X + Y + Z);
        }

        /// <summary>
        /// Accesses the points components by ID
        /// </summary>
        /// <param name="dimensionID">The ID of the point component / dimension</param>
        /// <returns>The value of the point in the given dimension</returns>
        public double this[int dimensionID]
        {
            get
            {
                switch (dimensionID)
                {
                    case 1: return X;
                    case 2: return Y;
                    case 3: return Z;
                    default:
                        throw new ArgumentException("Invalid index. Must be 1, 2 or 3");
                }
            }
        }

        public override string ToString()
        {
            return
                "(" + this.X.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                "," + this.Y.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                "," + this.Z.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + ")";
        }

        #region IDeepCloneable<MeshPoint> Members

        public MeshPoint Clone()
        {
            return new MeshPoint() { VertexID = VertexID, X = X, Y = Y, Z = Z };
        }

        #endregion

        #region IEquatable<MeshPoint> Members

        public bool Equals(MeshPoint other)
        {
            return this == other;
        }

        #endregion

        #region IXmlSerializable Members

        public void LoadXML(XmlNode node)
        {
            this.X = double.Parse(node.Attributes[Helper.Check(() => this.X)].Value, ExportationConstants.XML_FORMATTER);
            this.Y = double.Parse(node.Attributes[Helper.Check(() => this.Y)].Value, ExportationConstants.XML_FORMATTER);
            this.Z = double.Parse(node.Attributes[Helper.Check(() => this.Z)].Value, ExportationConstants.XML_FORMATTER);
        }

        public XmlNode WriteXML(XmlDocument document)
        {
            // Create the element
            XmlNode node = document.CreateElement(ExportationConstants.XML_POINT_IDENT);

            // Position
            XmlAttribute attr = document.CreateAttribute(Helper.Check(() => this.X));
            attr.Value = this.X.ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER);
            node.Attributes.Append(attr);

            // Position
            attr = document.CreateAttribute(Helper.Check(() => this.Y));
            attr.Value = this.Y.ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER);
            node.Attributes.Append(attr);

            // Position
            attr = document.CreateAttribute(Helper.Check(() => this.Z));
            attr.Value = this.Z.ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER);
            node.Attributes.Append(attr);

            // Return it
            return node;
        }

        #endregion
    }
}
