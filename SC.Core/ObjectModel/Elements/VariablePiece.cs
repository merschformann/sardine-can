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
    public class VariablePiece : Piece, ISealable, IDeepCloneable<VariablePiece>, IXmlSerializable
    {
        /// <summary>
        /// The material of this piece
        /// </summary>
        public Material Material = new Material() { MaterialClass = MaterialClassification.Default };

        /// <summary>
        /// Indicates whether items can be placed on this item or not
        /// </summary>
        public bool Stackable = true;

        /// <summary>
        /// Contains the orientations which are forbidden for this piece
        /// </summary>
        public HashSet<int> ForbiddenOrientations = new HashSet<int>();

        #region Flag handling

        /// <summary>
        /// The values per flag associated with this piece.
        /// </summary>
        private Dictionary<int, int> _flags = new Dictionary<int, int>();

        /// <summary>
        /// Sets the values for all flags for this piece.
        /// </summary>
        /// <param name="flags">The flags to set.</param>
        public void SetFlags(IEnumerable<(int flag, int value)> flags) => _flags = flags.ToDictionary(k => k.flag, v => v.value);
        /// <summary>
        /// Returns all flags set for this piece.
        /// </summary>
        /// <returns>All flags and their values for this piece.</returns>
        internal IEnumerable<(int flag, int value)> GetFlags() => _flags.Select(f => (f.Key, f.Value));
        /// <summary>
        /// Gets the value for the given flag. If no information for the flag is present, <code>null</code> will be returned.
        /// </summary>
        /// <param name="flag">The flag to look up.</param>
        /// <returns>The value for the corresponding flag or <code>null</code> if not value is set for it.</returns>
        public int? GetFlag(int flag)
        {
            if (_flags.ContainsKey(flag)) return _flags[flag];
            else return null;
        }

        #endregion

        #region IStringIdentable Members

        public override string ToIdentString()
        {
            return "VariablePiece" + ID.ToString();
        }

        #endregion

        #region ISealable Members

        public virtual void Seal()
        {
            Original.Seal();
            VertexGenerator.GenerateMeshesForAllOrientations(this);
        }

        #endregion

        #region IDeepCloneable<VariablePiece> Members

        public VariablePiece Clone()
        {
            // Clone the basic information
            VariablePiece clone = new VariablePiece
            {
                ID = ID,
                Original = Original.Clone(),
                _subPieceID = _subPieceID,
                _meshesPerOrientation = _meshesPerOrientation.Select(e => e.Clone()).ToArray(),
                Material = Material.Clone(),
                Stackable = Stackable,
                ForbiddenOrientations = new HashSet<int>(ForbiddenOrientations)
            };
            clone.SetFlags(_flags.Select(f => (f.Key, f.Value)));
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

            // Forbidden orientations
            if (node.Attributes[Helper.Check(() => this.ForbiddenOrientations)].Value != "")
            {
                string[] forbiddenOrientationIDs = node.Attributes[Helper.Check(() => this.ForbiddenOrientations)].Value.Split(ExportationConstants.XML_ATTRIBUTE_DELIMITER);
                foreach (var forbiddenID in forbiddenOrientationIDs)
                {
                    this.ForbiddenOrientations.Add(int.Parse(forbiddenID));
                }
            }

            // Weight (only if available - for backwards compatibility)
            if (node.Attributes[Helper.Check(() => this.Weight)] != null)
            {
                this.Weight = double.Parse(node.Attributes[Helper.Check(() => this.Weight)].Value, ExportationConstants.XML_FORMATTER);
            }

            // Material
            this.Material = new Material()
            {
                MaterialClass = (MaterialClassification)Enum.Parse(typeof(MaterialClassification), node.Attributes[Helper.Check(() => this.Material)].Value)
            };

            // Stackable
            this.Stackable = bool.Parse(node.Attributes[Helper.Check(() => this.Stackable)].Value);

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
            XmlNode node = document.CreateElement(ExportationConstants.XML_PIECE_IDENT);

            // ID
            XmlAttribute attr = document.CreateAttribute(Helper.Check(() => this.ID));
            attr.Value = this.ID.ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER);
            node.Attributes.Append(attr);

            // Forbidden orientations
            attr = document.CreateAttribute(Helper.Check(() => this.ForbiddenOrientations));
            attr.Value = Helper.GetOneString(
                this.ForbiddenOrientations.Select(o => o.ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER)),
                ExportationConstants.XML_ATTRIBUTE_DELIMITER.ToString());
            node.Attributes.Append(attr);

            // Weight
            attr = document.CreateAttribute(Helper.Check(() => this.Weight));
            attr.Value = this.Weight.ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER);
            node.Attributes.Append(attr);

            // Material
            attr = document.CreateAttribute(Helper.Check(() => this.Material));
            attr.Value = this.Material.MaterialClass.ToString();
            node.Attributes.Append(attr);

            // Stackable
            attr = document.CreateAttribute(Helper.Check(() => this.Stackable));
            attr.Value = this.Stackable.ToString(ExportationConstants.FORMATTER);
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
            return "Piece" + ID.ToString() + "-#C" + Original.Components.Count() +
                "-Dim-(" + this.Original.BoundingBox.Length.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                "," + this.Original.BoundingBox.Width.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                "," + this.Original.BoundingBox.Height.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                ")";
        }

        #endregion
    }
}
