using SC.ObjectModel.Additionals;
using SC.ObjectModel.Interfaces;
using SC.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace SC.ObjectModel.Elements
{
    /// <summary>
    /// Defines a slant which is used to mark unusable areas of a container
    /// </summary>
    public class Slant : IXmlSerializable, IStringIdentable, ISealable
    {
        /// <summary>
        /// The ID of the slant
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The position of the plane
        /// </summary>
        public MeshPoint Position { get; set; }

        /// <summary>
        /// The normal vector of the plane
        /// </summary>
        public MeshPoint NormalVector { get; set; }

        /// <summary>
        /// The container this slant belongs to
        /// </summary>
        public Container Container { get; set; }

        /// <summary>
        /// Defines the three intercept values of the plane associated with this slant
        /// </summary>
        public MeshPoint InterceptValues = null;

        /// <summary>
        /// Contains all intersections of the plane with the container
        /// </summary>
        private List<MeshPoint> _containerIntersections = null;

        /// <summary>
        /// Contains all intersections of the plane with the container
        /// </summary>
        public List<MeshPoint> ContainerIntersections
        {
            get
            {
                if (_containerIntersections == null)
                {
                    CalculateContainerIntersections();
                }
                return _containerIntersections;
            }
        }

        /// <summary>
        /// Calculates all intersections of the plane with the container
        /// </summary>
        private void CalculateContainerIntersections()
        {
            // Calculate intersect points of plane with container
            List<MeshPoint> intersectionPoints = new List<MeshPoint>()
            {
                // Lower frame
                new MeshPoint() { X = GetIntersectionPositionXProjection(0, 0), Y = 0, Z = 0 },
                new MeshPoint() { X = 0, Y = GetIntersectionPositionYProjection(0, 0), Z = 0 },
                new MeshPoint() { X = GetIntersectionPositionXProjection(Container.Mesh.Width, 0), Y = Container.Mesh.Width, Z = 0 },
                new MeshPoint() { X = Container.Mesh.Length, Y = GetIntersectionPositionYProjection(Container.Mesh.Length, 0), Z = 0 },
                // Outer edges
                new MeshPoint() { X = 0, Y = 0, Z = GetIntersectionPositionZProjection(0, 0) },
                new MeshPoint() { X = Container.Mesh.Length, Y = 0, Z = GetIntersectionPositionZProjection(Container.Mesh.Length, 0) },
                new MeshPoint() { X = 0, Y = Container.Mesh.Width, Z = GetIntersectionPositionZProjection(0, Container.Mesh.Width) },
                new MeshPoint() { X = Container.Mesh.Length, Y = Container.Mesh.Width, Z = GetIntersectionPositionZProjection(Container.Mesh.Length, Container.Mesh.Width) },
                // Upper frame
                new MeshPoint() { X = GetIntersectionPositionXProjection(0, Container.Mesh.Height), Y = 0, Z = Container.Mesh.Height },
                new MeshPoint() { X = 0, Y = GetIntersectionPositionYProjection(0, Container.Mesh.Height), Z = Container.Mesh.Height },
                new MeshPoint() { X = GetIntersectionPositionXProjection(Container.Mesh.Width, Container.Mesh.Height), Y = Container.Mesh.Width, Z = Container.Mesh.Height },
                new MeshPoint() { X = Container.Mesh.Length, Y = GetIntersectionPositionYProjection(Container.Mesh.Length, Container.Mesh.Height), Z = Container.Mesh.Height },
            };

            // Filter the relevant points
            intersectionPoints = intersectionPoints.Where(p =>
                0 <= p.X && p.X <= Container.Mesh.Length &&
                0 <= p.Y && p.Y <= Container.Mesh.Width &&
                0 <= p.Z && p.Z <= Container.Mesh.Height).ToList();

            // Submit intersections
            _containerIntersections = intersectionPoints;
        }

        /// <summary>
        /// Defines a small number which is added to every calculated projection value
        /// </summary>
        private double _projectionEpsilon = 0.000001;

        /// <summary>
        /// Gets the intercept value of a line parallel to the x-axis with the plane
        /// </summary>
        /// <param name="y">The y-value of the line</param>
        /// <param name="z">The z-value of the line</param>
        /// <returns>The intersection of the line and the plane</returns>
        public double GetIntersectionPositionXProjection(double y, double z)
        {
            if (NormalVector.X == 0)
            {
                return double.NaN;
            }
            return (1 - 1.0 / InterceptValues.Y * y - 1.0 / InterceptValues.Z * z) * InterceptValues.X + _projectionEpsilon;
        }

        /// <summary>
        /// Gets the intercept value of a line parallel to the y-axis with the plane
        /// </summary>
        /// <param name="x">The x-value of the line</param>
        /// <param name="z">The z-value of the line</param>
        /// <returns>The intersection of the line and the plane</returns>
        public double GetIntersectionPositionYProjection(double x, double z)
        {
            if (NormalVector.Y == 0)
            {
                return double.NaN;
            }
            return (1 - 1.0 / InterceptValues.X * x - 1.0 / InterceptValues.Z * z) * InterceptValues.Y + _projectionEpsilon;
        }

        /// <summary>
        /// Gets the intercept value of a line parallel to the z-axis with the plane
        /// </summary>
        /// <param name="x">The x-value of the line</param>
        /// <param name="y">The y-value of the line</param>
        /// <returns>The intersection of the line and the plane</returns>
        public double GetIntersectionPositionZProjection(double x, double y)
        {
            if (NormalVector.Z == 0)
            {
                return double.NaN;
            }
            return (1 - 1.0 / InterceptValues.X * x - 1.0 / InterceptValues.Y * y) * InterceptValues.Z + _projectionEpsilon;
        }

        /// <summary>
        /// Gets the coefficients of the slant for input to QHull 
        /// </summary>
        /// <returns>Double array of length 4 with coefficients. First three containing the coordinates and last index containing the offset value.</returns>
        public double[] GetCoefficientsForQHull(MeshPoint containerCenterPoint)
        {
            
            double[] coefficients = new double[4];
            
            coefficients[0] = (this.NormalVector.X );
            coefficients[1] = (this.NormalVector.Y );
            coefficients[2] = (this.NormalVector.Z );
            coefficients[3] = (-1) * ((this.NormalVector.X ) * (this.Position.X + 1.0) + (this.NormalVector.Y ) * (this.Position.Y + 1.0) + (this.NormalVector.Z ) * (this.Position.Z + 1.0));

            return coefficients;
        }


        public override string ToString()
        {
            return "Slant" + ID.ToString() + "-" + Position.ToString() + "-" + NormalVector.ToString();
        }

        #region IXmlSerializable Members

        public void LoadXML(XmlNode node)
        {
            // ID
            this.ID = int.Parse(node.Attributes[Helper.Check(() => this.ID)].Value);

            // Position
            Position = new MeshPoint();
            Position.LoadXML(node.SelectSingleNode(ExportationConstants.XML_SLANT_POSITION_IDENT).ChildNodes.OfType<XmlNode>().Single());

            // Normal
            NormalVector = new MeshPoint();
            NormalVector.LoadXML(node.SelectSingleNode(ExportationConstants.XML_SLANT_NORMAL_VECTOR_IDENT).ChildNodes.OfType<XmlNode>().Single());

            // Seal it
            this.Seal();
        }

        public XmlNode WriteXML(XmlDocument document)
        {
            // Create the element
            XmlNode node = document.CreateElement(ExportationConstants.XML_SLANT_IDENT);

            // ID
            XmlAttribute attr = document.CreateAttribute(Helper.Check(() => this.ID));
            attr.Value = this.ID.ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER);
            node.Attributes.Append(attr);

            // Create position root
            XmlNode positionRoot = document.CreateElement(ExportationConstants.XML_SLANT_POSITION_IDENT);
            // Append position
            positionRoot.AppendChild(Position.WriteXML(document));
            // Attach slants
            node.AppendChild(positionRoot);

            // Create position root
            XmlNode normalRoot = document.CreateElement(ExportationConstants.XML_SLANT_NORMAL_VECTOR_IDENT);
            // Append position
            normalRoot.AppendChild(NormalVector.WriteXML(document));
            // Attach slants
            node.AppendChild(normalRoot);

            // Return it
            return node;
        }

        #endregion

        #region IStringIdentable Members

        public string ToIdentString()
        {
            return "Slant" + ID.ToString();
        }

        #endregion

        #region ISealable Members

        public void Seal()
        {
            // Norm the vector
            double divisor = Math.Sqrt(Math.Pow(NormalVector.X, 2) + Math.Pow(NormalVector.Y, 2) + Math.Pow(NormalVector.Z, 2));
            NormalVector.X /= divisor;
            NormalVector.Y /= divisor;
            NormalVector.Z /= divisor;

            // Calculate coordinate form
            double a = this.NormalVector.X;
            double b = this.NormalVector.Y;
            double c = this.NormalVector.Z;
            double d = this.Position.X * this.NormalVector.X + this.Position.Y * this.NormalVector.Y + this.Position.Z * this.NormalVector.Z;
            InterceptValues = new MeshPoint() { X = d / a, Y = d / b, Z = d / c };
        }

        #endregion
    }
}
