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
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;

    /// <summary>
    /// Defines a container which can be filled with pieces
    /// </summary>
    public class Container : ISealable, IStringIdentable, IXmlSerializable
    {
        /// <summary>
        /// The ID of the container
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// A volatile ID to reference the container in arrays
        /// </summary>
        public int VolatileID { get; set; }

        /// <summary>
        /// The basic shape of the container
        /// </summary>
        public MeshCube Mesh { get; set; }

        /// <summary>
        /// The virtual pieces defining unusable areas of the container
        /// </summary>
        public List<VirtualPiece> VirtualPieces = new List<VirtualPiece>();

        /// <summary>
        /// A list of all slants defining unusable areas of the container
        /// </summary>
        public List<Slant> Slants = new List<Slant>();

        /// <summary>
        /// The current ID of the virtual piece
        /// </summary>
        private int _virtualPieceID = 0;

        /// <summary>
        /// The current ID of the slant
        /// </summary>
        private int _slantID = 0;

        /// <summary>
        /// The volume of the container (excluding unusable areas cut off by slants)
        /// </summary>
        public double Volume = 0.0;

        /// <summary>
        /// The volume of the container after being cut by all given slants.
        /// </summary>
        public double VolumeAfterCutOffBySlants = 0.0;

        /// <summary>
        /// The volume inside the container that is occupied by virtual pieces.
        /// </summary>
        public double VolumeOccupiedByVirtualPieces = 0.0;

        /// <summary>
        /// The maximal weight a container can take. Unlimited, if not explicitly set.
        /// </summary>
        public double MaxWeight { get; set; } = double.PositiveInfinity;

        /// <summary>
        /// The maximal capacity a container offers for multiple dimensions. Every dimension is identified by a string and needs a matching "Quantities" entry in the pieces.
        /// </summary>
        public Dictionary<string, double> Capacity { get; set; } = [];

        /// <summary>
        /// Adds the virtual piece to the container
        /// </summary>
        /// <param name="piece">A virtual piece defining an unusable area of the container</param>
        public void AddVirtualPiece(VirtualPiece piece)
        {
            piece.ID = _virtualPieceID++;
            VirtualPieces.Add(piece);
        }

        /// <summary>
        /// Adds the slant to the container
        /// </summary>
        /// <param name="slant">A slant defining an unusable area of the container</param>
        public void AddSlant(Slant slant)
        {
            slant.ID = _slantID++;
            Slants.Add(slant);
        }

        /// <summary>
        /// Calculates the volume of the container excluding the unusable areas cut off by slants and virtual pieces
        /// </summary>
        public void CalculateVolume()
        {
            if (this.Slants.Any())
            {
                this.CalculatePureContainerVolume();
            }
            else
            {
                this.Volume = this.Mesh.Height * this.Mesh.Length * this.Mesh.Width;
            }
            if (this.VirtualPieces.Any())
            {
                this.SubstractVirtualPiecesFromTotalVolume();
            }
        }

        /// <summary>
        /// Calculates the volume of the container excluding the unusable areas cut off by slants using QHull (<see cref="http://www.qhull.org/"/>)
        /// </summary>
        public void CalculatePureContainerVolume()
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

            // Generate half spaces
            List<double[]> halfspacesCube = this.Mesh.GetHalfspacesForQHull();
            StringBuilder qhullInput = new StringBuilder();
            qhullInput.Append("3 1\n");
            qhullInput.Append((this.Mesh.CenterPoint.X + 1.0).ToString(CultureInfo.InvariantCulture) + "\t");
            qhullInput.Append((this.Mesh.CenterPoint.Y + 1.0).ToString(CultureInfo.InvariantCulture) + "\t");
            qhullInput.Append((this.Mesh.CenterPoint.Z + 1.0).ToString(CultureInfo.InvariantCulture) + "\n");
            qhullInput.Append("4\n");
            qhullInput.Append(this.Slants.Count + halfspacesCube.Count + "\n");

            foreach (var halfspace in halfspacesCube)
            {
                qhullInput.Append(halfspace[0].ToString(CultureInfo.InvariantCulture) + "\t" + halfspace[1].ToString(CultureInfo.InvariantCulture) + "\t" + halfspace[2].ToString(CultureInfo.InvariantCulture) + "\t" + halfspace[3].ToString(CultureInfo.InvariantCulture) + "\n");
            }
            foreach (var slant in this.Slants)
            {
                double[] coefficients = slant.GetCoefficientsForQHull(this.Mesh.CenterPoint);
                qhullInput.Append(coefficients[0].ToString(CultureInfo.InvariantCulture) + "\t" + coefficients[1].ToString(CultureInfo.InvariantCulture) + "\t" + coefficients[2].ToString(CultureInfo.InvariantCulture) + "\t" + coefficients[3].ToString(CultureInfo.InvariantCulture) + "\n");
            }

            using (StreamWriter writer = new StreamWriter(halfspaceFile))
            {
                writer.Write(qhullInput);
            }

            Process qhalf = new Process();
            qhalf.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, (isLinux) ? "qhalf" : "qhalf.exe");
            qhalf.StartInfo.Arguments = "s Fp" + " TI " + halfspaceFile + " TO " + polyhedronFile;
            qhalf.StartInfo.UseShellExecute = false;
            qhalf.StartInfo.RedirectStandardError = true;
            qhalf.EnableRaisingEvents = true;
            qhalf.Start();
            bool error = false;
            using (StreamReader reader = qhalf.StandardError)
            {
                string result = reader.ReadToEnd();
                if (result.StartsWith("QH"))
                {

                    using (StreamWriter writer = new StreamWriter(errorLogFile, true))
                    {
                        writer.Write(result);
                    }

                    error = true;
                }
            }

            qhalf.WaitForExit();
            if (!error)
            {
                // Execute the external code
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

                    this.Volume = double.Parse(resultVolume[resultVolume.Length - 3].Split(':')[1].Trim(), CultureInfo.InvariantCulture);
                    this.VolumeAfterCutOffBySlants = this.Volume;
                }
                qconvex.WaitForExit();
            }
            else
            {
                this.Volume = -1;
            }

            // Clean up
            if (File.Exists(halfspaceFile))
                File.Delete(halfspaceFile);
            if (File.Exists(polyhedronFile))
                File.Delete(polyhedronFile);
        }

        /// <summary>
        /// Reduce the container volume by occupied space due to virtual pieces
        /// </summary>
        public void SubstractVirtualPiecesFromTotalVolume()
        {
            foreach (var virtualPiece in this.VirtualPieces)
            {
                this.Volume -= virtualPiece.VolumeInsideContainer;
                this.VolumeOccupiedByVirtualPieces += virtualPiece.VolumeInsideContainer;
            }
        }

        public override string ToString()
        {
            return "Container" + ID + "VCount" + VirtualPieces.Count +
                "-Dim-(" + this.Mesh.Length.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                "," + this.Mesh.Width.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                "," + this.Mesh.Height.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                ") Vol: " + this.Volume.ToString(CultureInfo.InvariantCulture) + " \t (" + this.Mesh.Volume.ToString(CultureInfo.InvariantCulture) + " - " + (this.Mesh.Volume - this.VolumeAfterCutOffBySlants).ToString(CultureInfo.InvariantCulture) + " - " + this.VolumeOccupiedByVirtualPieces.ToString(CultureInfo.InvariantCulture) + ")";
        }

        #region ISealable Members

        public void Seal()
        {
            VertexGenerator.GenerateVertexInformation(this);
            foreach (var virtualPiece in VirtualPieces)
            {
                virtualPiece.Seal();
            }
            this.CalculateVolume();
        }

        #endregion

        #region IStringIdentable Members

        public string ToIdentString()
        {
            return "Container" + ID.ToString();
        }

        #endregion

        #region IXmlSerializable Members

        public void LoadXML(XmlNode node)
        {
            // ID
            this.ID = int.Parse(node.Attributes[Helper.Check(() => this.ID)].Value, ExportationConstants.XML_FORMATTER);

            // MaxWeight (only if available - for backwards compatibility)
            if (node.Attributes[Helper.Check(() => this.MaxWeight)] != null)
                this.MaxWeight = double.Parse(node.Attributes[Helper.Check(() => this.MaxWeight)].Value, ExportationConstants.XML_FORMATTER);
            else
                this.MaxWeight = double.PositiveInfinity;

            // Mesh
            MeshCube mesh = new MeshCube();
            mesh.LoadXML(node.SelectSingleNode(ExportationConstants.XML_MESHCUBE_IDENT));
            Mesh = mesh;

            // Virtual pieces
            VirtualPieces = new List<VirtualPiece>();
            if (node.SelectSingleNode(ExportationConstants.XML_VIRTUAL_PIECE_COLLECTION_IDENT) != null)
            {
                foreach (var virtualPieceNode in node.SelectSingleNode(ExportationConstants.XML_VIRTUAL_PIECE_COLLECTION_IDENT).ChildNodes.OfType<XmlNode>())
                {
                    VirtualPiece virtualPiece = new VirtualPiece();
                    virtualPiece.LoadXML(virtualPieceNode);
                    virtualPiece.Container = this;
                    VirtualPieces.Add(virtualPiece);
                }
            }

            // Slants
            Slants = new List<Slant>();
            if (node.SelectSingleNode(ExportationConstants.XML_SLANT_COLLECTION_IDENT) != null)
            {
                foreach (var slantNode in node.SelectSingleNode(ExportationConstants.XML_SLANT_COLLECTION_IDENT).ChildNodes.OfType<XmlNode>())
                {
                    Slant slant = new Slant();
                    slant.LoadXML(slantNode);
                    slant.Container = this;
                    Slants.Add(slant);
                }
            }

            // Seal
            this.Seal();
        }

        public XmlNode WriteXML(XmlDocument document)
        {
            // Create the element
            XmlNode node = document.CreateElement(ExportationConstants.XML_CONTAINER_IDENT);

            // ID
            XmlAttribute attr = document.CreateAttribute(Helper.Check(() => this.ID));
            attr.Value = this.ID.ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER);
            node.Attributes.Append(attr);

            // MaxWeight
            attr = document.CreateAttribute(Helper.Check(() => this.MaxWeight));
            attr.Value = this.MaxWeight.ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER);
            node.Attributes.Append(attr);

            // Create mesh info
            node.AppendChild(Mesh.WriteXML(document));

            // Create virtual piece info (if available)
            if (VirtualPieces != null && VirtualPieces.Count > 0)
            {
                // Create virtual piece root
                XmlNode virtualPieceRoot = document.CreateElement(ExportationConstants.XML_VIRTUAL_PIECE_COLLECTION_IDENT);

                // Append virtual pieces
                foreach (var virtualPiece in VirtualPieces)
                {
                    virtualPieceRoot.AppendChild(virtualPiece.WriteXML(document));
                }

                // Attach pieces
                node.AppendChild(virtualPieceRoot);
            }

            // Create slants info (if available)
            if (Slants != null && Slants.Count > 0)
            {
                // Create slant root
                XmlNode slantRoot = document.CreateElement(ExportationConstants.XML_SLANT_COLLECTION_IDENT);

                // Append slants
                foreach (var slant in Slants)
                {
                    slantRoot.AppendChild(slant.WriteXML(document));
                }

                // Attach slants
                node.AppendChild(slantRoot);
            }

            // Return it
            return node;
        }

        #endregion
    }
}
