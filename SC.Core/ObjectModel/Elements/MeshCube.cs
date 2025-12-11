using SC.Core.ObjectModel.Additionals;
using SC.Core.ObjectModel.Interfaces;
using SC.Core.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SC.Core.ObjectModel.Elements
{
    /// <summary>
    /// Defines a simple cube
    /// </summary>
    public class MeshCube : IDeepCloneable<MeshCube>, IXmlSerializable, IStringIdentable
    {
        /// <summary>
        /// The ID of the cube
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The relative position of the cube
        /// </summary>
        public MeshPoint RelPosition = new MeshPoint();

        /// <summary>
        /// The length of the cube (x-dimension)
        /// </summary>
        public double Length;

        /// <summary>
        /// The width of the cube (y-dimension)
        /// </summary>
        public double Width;

        /// <summary>
        /// The height of the cube (z-dimension)
        /// </summary>
        public double Height;

        /// <summary>
        /// A volatile ID for fast access
        /// </summary>
        public int VolatileID;

        #region Volume treatment

        /// <summary>
        /// Used for fast volume access
        /// </summary>
        private double _volume = double.NaN;

        /// <summary>
        /// The volume of this cube (precalculated L*W*H)
        /// </summary>
        public double Volume
        {
            get
            {
                if (double.IsNaN(_volume))
                {
                    _volume = Length * Width * Height;
                }
                return _volume;
            }
        }

        /// <summary>
        /// The centerpoint of this cube
        /// </summary>
        public MeshPoint CenterPoint
        {
            get
            {
                MeshPoint centerpoint = new MeshPoint();
                centerpoint.X = (this.Length / 2.0);
                centerpoint.Y = (this.Width / 2.0);
                centerpoint.Z = (this.Height / 2.0);
                return centerpoint;
            }
        }

        /// <summary>
        /// Gets the halfspaces to form the basic cube
        /// </summary>
        /// <returns></returns>
        public List<double[]> GetHalfspacesForQHull(MeshPoint fixedPosition = null)
        {
            List<double[]> halfspaces = new List<double[]>();
            double xShift = 1.0;
            double yShift = 1.0;
            double zShift = 1.0;
            if (fixedPosition != null)
            {
                xShift += fixedPosition.X;
                yShift += fixedPosition.Y;
                zShift += fixedPosition.Z;
            }
            halfspaces.Add(new double[4] { -1, 0, 0, xShift }); //LB X
            halfspaces.Add(new double[4] { 0, -1, 0, yShift }); //LB Y
            halfspaces.Add(new double[4] { 0, 0, -1, zShift }); //LB Z
            halfspaces.Add(new double[4] { 1, 0, 0, (-1.0 * (this.Length + xShift)) }); //UB X
            halfspaces.Add(new double[4] { 0, 1, 0, (-1.0 * (this.Width + yShift)) }); //UB Y
            halfspaces.Add(new double[4] { 0, 0, 1, (-1.0 * (this.Height + zShift)) }); //UB Z;
            return halfspaces;
        }

        #endregion

        #region Vertex access

        /// <summary>
        /// Contains the vertices for this cube
        /// </summary>
        private MeshPoint[] _vertices = new MeshPoint[9];

        /// <summary>
        /// Accesses a vertex by its ID
        /// </summary>
        /// <param name="vertexID">The ID of the vertex</param>
        /// <returns>The vertex</returns>
        public MeshPoint this[int vertexID]
        {
            get { return _vertices[vertexID]; }
            set { _vertices[vertexID] = value; }
        }

        /// <summary>
        /// All vertices of this cube
        /// </summary>
        public IEnumerable<MeshPoint> Vertices { get { return _vertices.AsEnumerable(); } }

        #endregion

        #region Side access

        /// <summary>
        /// Accesses the sides of the cube by an ID
        /// </summary>
        /// <param name="beta">The ID of the side</param>
        /// <returns>The length of the specified side</returns>
        public double SideLength(int beta)
        {
            switch (beta)
            {
                case 1: return Length;
                case 2: return Width;
                case 3: return Height;
                default:
                    throw new ArgumentException("Unknown betaID: " + beta);
            }
        }

        #endregion

        #region IDeepCloneable<MeshCubeSet> Members

        public MeshCube Clone()
        {
            return new MeshCube()
            {
                ID = ID,
                RelPosition = RelPosition.Clone(),
                Length = Length,
                Width = Width,
                Height = Height,
                _vertices = (_vertices != null) ? _vertices.Select(v => v.Clone()).ToArray() : null,
                _volume = _volume
            };
        }

        #endregion

        #region IStringIdentable Members

        public string ToIdentString()
        {
            return "Cube" + ID.ToString();
        }

        #endregion

        #region IXmlSerializable Members

        public void LoadXML(XmlNode node)
        {
            // Read attributes
            this.Length = double.Parse(node.Attributes[Helper.Check(() => this.Length)].Value, ExportationConstants.XML_FORMATTER);
            this.Width = double.Parse(node.Attributes[Helper.Check(() => this.Width)].Value, ExportationConstants.XML_FORMATTER);
            this.Height = double.Parse(node.Attributes[Helper.Check(() => this.Height)].Value, ExportationConstants.XML_FORMATTER);

            // Read position
            this.RelPosition = new MeshPoint();
            this.RelPosition.LoadXML(node.FirstChild);
        }

        public XmlNode WriteXML(XmlDocument document)
        {
            // Create the element
            XmlNode node = document.CreateElement(ExportationConstants.XML_MESHCUBE_IDENT);

            // Size
            XmlAttribute attr = document.CreateAttribute(Helper.Check(() => this.RelPosition.X));
            attr = document.CreateAttribute(Helper.Check(() => this.Length));
            attr.Value = this.Length.ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER);
            node.Attributes.Append(attr);

            // Size
            attr = document.CreateAttribute(Helper.Check(() => this.Width));
            attr.Value = this.Width.ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER);
            node.Attributes.Append(attr);

            // Size
            attr = document.CreateAttribute(Helper.Check(() => this.Height));
            attr.Value = this.Height.ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER);
            node.Attributes.Append(attr);

            // Attach position
            node.AppendChild(this.RelPosition.WriteXML(document));

            // Return it
            return node;
        }

        #endregion

        public override string ToString()
        {
            return
                "Cube" + ID.ToString() +
                "-Dim-(" + this.Length.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                "," + this.Width.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                "," + this.Height.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                ")";
        }
    }
}
