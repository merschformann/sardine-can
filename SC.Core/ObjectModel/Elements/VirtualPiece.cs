using SC.Core.ObjectModel.Additionals;
using SC.Core.ObjectModel.Interfaces;
using SC.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SC.Core.ObjectModel.Elements
{
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;

    public class VirtualPiece : Piece, ISealable, IDeepCloneable<VirtualPiece>, IXmlSerializable
    {
        /// <summary>
        /// May contain a previously fixed position of the piece
        /// </summary>
        public MeshPoint FixedPosition;

        /// <summary>
        /// May contain a previously fixed orientation of the piece
        /// </summary>
        public int FixedOrientation;

        /// <summary>
        /// The container this piece belongs to
        /// </summary>
        public Container Container;

        /// <summary>
        /// Volume of the virtual piece that lies inside the container (cutted by slants)
        /// </summary>
        private double _volumeInsideContainer = -1.0;

        /// <summary>
        /// Gets the volume of the virtual piece that lies inside the container (cutted by slants)
        /// </summary>
        public double VolumeInsideContainer
        {
            get
            {
                if (this._volumeInsideContainer >= 0)
                {
                    return this._volumeInsideContainer;
                }
                else
                {
                    return this.CalculateVolumeInsideContainer();
                }
            }
        }


        public override string ToIdentString()
        {
            return ID.ToString() + "VirtualPiece" + Container.ToIdentString();
        }

        #region ISealable Members

        /// <summary>
        /// Calculates the volume of the part of the virtual piece which is inside the container 
        /// </summary>
        /// <returns>Volume inside container</returns>
        private double CalculateVolumeInsideContainer()
        {
            // Determine whether we are running on linux
            bool isLinux = false;
            int platformID = (int)Environment.OSVersion.Platform;
            if ((platformID == 4) || (platformID == 6) || (platformID == 128))
                isLinux = true;

            // Get a "unique" file identifier to prevent collisions on the cluster
            Random random = new Random();
            int randomPlaces = 8;
            string fileIdent = string.Format("{0:" + new string('0', randomPlaces) + "}", (int)Math.Floor(random.NextDouble() * Math.Pow(10, randomPlaces)));
            string halfspaceFile = "halfspaces" + fileIdent + ".txt";
            string polyhedronFile = "polyhedron" + fileIdent + ".txt";
            string errorLogFile = "qhull_errorlog" + fileIdent + ".txt";

            // Begin
            double volumeInsideContainer = 0.0;
            ComponentsSet componentsSet = this[this.FixedOrientation];
            foreach (var component in componentsSet.Components)
            {
                //Get origin of component
                MeshPoint origin = new MeshPoint();
                origin.X = this.FixedPosition.X + component.RelPosition.X;
                origin.Y = this.FixedPosition.Y + component.RelPosition.Y;
                origin.Z = this.FixedPosition.Z + component.RelPosition.Z;

                //Get halfspaces of component
                List<double[]> halfspaces = component.GetHalfspacesForQHull(origin);
                //Get halfspaces of container cube
                halfspaces.AddRange(this.Container.Mesh.GetHalfspacesForQHull());

                MeshPoint componentCenter = new MeshPoint();
                componentCenter.X = component.CenterPoint.X + origin.X;
                componentCenter.Y = component.CenterPoint.Y + origin.Y;
                componentCenter.Z = component.CenterPoint.Z + origin.Z;
                double[] coefficients = null;

                //Get halfspaces for each slant
                foreach (var slant in this.Container.Slants)
                {
                    coefficients = slant.GetCoefficientsForQHull(componentCenter);
                    halfspaces.Add(coefficients);
                }


                //Calculate intersection using QHull
                StringBuilder qhullInputAll = new StringBuilder();

                qhullInputAll.Append("3 1\n");
                qhullInputAll.Append((componentCenter.X + 1.0).ToString(CultureInfo.InvariantCulture) + "\t");
                qhullInputAll.Append((componentCenter.Y + 1.0).ToString(CultureInfo.InvariantCulture) + "\t");
                qhullInputAll.Append((componentCenter.Z + 1.0).ToString(CultureInfo.InvariantCulture) + "\n");
                qhullInputAll.Append("4\n");
                qhullInputAll.Append(halfspaces.Count + "\n");

                foreach (var halfspace in halfspaces)
                {
                    qhullInputAll.Append(halfspace[0].ToString(CultureInfo.InvariantCulture) + "\t" + halfspace[1].ToString(CultureInfo.InvariantCulture) + "\t" + halfspace[2].ToString(CultureInfo.InvariantCulture) + "\t" + halfspace[3].ToString(CultureInfo.InvariantCulture) + "\n");
                }


                using (StreamWriter writer = new StreamWriter(halfspaceFile, false))
                {
                    writer.Write(qhullInputAll);
                }

                Process qhalfAll = new Process();
                qhalfAll.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, (isLinux) ? "qhalf" : "qhalf.exe");
                qhalfAll.StartInfo.Arguments = "s Fp" + " TI " + halfspaceFile + " TO " + polyhedronFile;
                qhalfAll.StartInfo.UseShellExecute = false;
                qhalfAll.StartInfo.RedirectStandardError = true;
                qhalfAll.EnableRaisingEvents = true;
                qhalfAll.Start();
                bool errorAll = false;
                using (StreamReader reader = qhalfAll.StandardError)
                {
                    string result = reader.ReadToEnd();
                    if (result.StartsWith("QH"))
                    {

                        using (StreamWriter writer = new StreamWriter(errorLogFile, true))
                        {
                            writer.Write(result);
                        }

                        errorAll = true;
                    }
                }
                qhalfAll.WaitForExit();
                //Calculate volume of component in container using QHull
                if (!errorAll)
                {

                    Process qconvex = new Process();
                    qconvex.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, (isLinux) ? "qconvex" : "qconvex.exe");
                    qconvex.StartInfo.Arguments = "FA" + " TI " + polyhedronFile;
                    qconvex.StartInfo.UseShellExecute = false;
                    qconvex.StartInfo.RedirectStandardOutput = true;
                    qconvex.EnableRaisingEvents = true;
                    qconvex.Start();
                    using (StreamReader reader = qconvex.StandardOutput)
                    {
                        string result = reader.ReadToEnd();
                        string[] resultVolume = result.Split('\n');

                        volumeInsideContainer += double.Parse(resultVolume[resultVolume.Length - 3].Split(':')[1].Trim(), CultureInfo.InvariantCulture);

                    }
                    qconvex.WaitForExit();
                }

                // Clean up
                if (File.Exists(halfspaceFile))
                    File.Delete(halfspaceFile);
                if (File.Exists(polyhedronFile))
                    File.Delete(polyhedronFile);
            }
            this._volumeInsideContainer = volumeInsideContainer;
            return volumeInsideContainer;
        }

        public void Seal()
        {
            Original.Seal();
            VertexGenerator.GenerateMeshesForAllOrientations(this);

        }

        #endregion

        #region IDeepCloneable<VirtualPiece> Members

        public VirtualPiece Clone()
        {
            // Clone the basic information
            VirtualPiece clone = new VirtualPiece();
            clone.ID = ID;
            clone.Original = Original.Clone();
            clone._subPieceID = _subPieceID;
            clone._meshesPerOrientation = _meshesPerOrientation.Select(e => e.Clone()).ToArray();
            clone.FixedPosition = FixedPosition.Clone();
            clone.FixedOrientation = FixedOrientation;
            // Update parent information for mesh-points
            foreach (var vertexID in MeshConstants.VERTEX_IDS)
            {
                clone.Original.BoundingBox[vertexID].ParentPiece = clone;
                foreach (var component in clone.Original.Components)
                {
                    component[vertexID].ParentPiece = clone;
                }
            }
            foreach (var orientation in MeshConstants.ORIENTATIONS)
            {
                foreach (var vertexID in MeshConstants.VERTEX_IDS)
                {
                    clone[orientation].BoundingBox[vertexID].ParentPiece = clone;
                    foreach (var component in clone[orientation].Components)
                    {
                        component[vertexID].ParentPiece = clone;
                    }
                }
            }
            // Return
            return clone;
        }

        #endregion

        #region IXmlSerializable Members

        public void LoadXML(XmlNode node)
        {
            // ID
            this.ID = int.Parse(node.Attributes[Helper.Check(() => this.ID)].Value);

            // Orientation
            this.FixedOrientation = int.Parse(node.Attributes[Helper.Check(() => this.FixedOrientation)].Value);

            // Position
            string[] positionValues = node.Attributes[Helper.Check(() => this.FixedPosition)].Value.Split(ExportationConstants.XML_ATTRIBUTE_DELIMITER);
            this.FixedPosition = new MeshPoint()
            {
                X = double.Parse(positionValues[0], ExportationConstants.FORMATTER),
                Y = double.Parse(positionValues[1], ExportationConstants.FORMATTER),
                Z = double.Parse(positionValues[2], ExportationConstants.FORMATTER)
            };

            // Components
            this.Original = new ComponentsSet();
            XmlNode componentsNode = node.ChildNodes.OfType<XmlNode>().Where(n => n.Name == ExportationConstants.XML_COMPONENTS_SET_IDENT).First();
            this.Original.LoadXML(componentsNode);

            // Seal
            this.Seal();
        }

        public XmlNode WriteXML(XmlDocument document)
        {
            // Create the element
            XmlNode node = document.CreateElement(ExportationConstants.XML_VIRTUAL_PIECE_IDENT);

            // ID
            XmlAttribute attr = document.CreateAttribute(Helper.Check(() => this.ID));
            attr.Value = this.ID.ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER);
            node.Attributes.Append(attr);

            // Orientation
            attr = document.CreateAttribute(Helper.Check(() => this.FixedOrientation));
            attr.Value = this.FixedOrientation.ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER);
            node.Attributes.Append(attr);

            // Orientation
            attr = document.CreateAttribute(Helper.Check(() => this.FixedPosition));
            attr.Value =
                this.FixedPosition.X.ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER) + ExportationConstants.XML_ATTRIBUTE_DELIMITER +
                this.FixedPosition.Y.ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER) + ExportationConstants.XML_ATTRIBUTE_DELIMITER +
                this.FixedPosition.Z.ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER);
            node.Attributes.Append(attr);

            // Attach components
            node.AppendChild(this.Original.WriteXML(document));

            // Return it
            return node;
        }

        #endregion

        #region ToString Members
        public override string ToString()
        {
            return "VirtualPiece" + ID.ToString() + "-Container" + Container.ID.ToString() + "-#C" + Original.Components.Count() +
                "-Dim-(" + this.Original.BoundingBox.Length.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                "," + this.Original.BoundingBox.Width.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                "," + this.Original.BoundingBox.Height.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                ")";
        }

        #endregion
    }
}
