using SC.ObjectModel.Additionals;
using SC.ObjectModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SC.ObjectModel.Elements
{
    /// <summary>
    /// Defines a set of components which describe the shape of a piece.
    /// </summary>
    public class ComponentsSet : ISealable, IDeepCloneable<ComponentsSet>, IXmlSerializable
    {
        /// <summary>
        /// The bounding box wrapping the possibly more complex content of this piece
        /// </summary>
        public MeshCube BoundingBox;

        /// <summary>
        /// All components of which this piece consists. Only one component if its a simple parallelepiped.
        /// </summary>
        public List<MeshCube> Components;

        /// <summary>
        /// Contains all components by their ID.
        /// </summary>
        private MeshCube[] ComponentsByID;

        /// <summary>
        /// Adds a new component to the set.
        /// </summary>
        /// <param name="component">The component to add</param>
        internal void AddComponent(MeshCube component)
        {
            if (Components == null)
            {
                Components = new List<MeshCube>();
            }
            Components.Add(component);
        }

        /// <summary>
        /// Returns the component which relates to the supplied ID
        /// </summary>
        /// <param name="componentID">The ID of the component</param>
        /// <returns>The desired component</returns>
        public MeshCube this[int componentID]
        {
            get
            {
                if (ComponentsByID == null)
                {
                    ComponentsByID = Components.ToArray();
                }
                return ComponentsByID[componentID];
            }
        }

        public override string ToString()
        {
            return
                "Set-#C" + Components.Count.ToString() +
                "-Dim-(" + BoundingBox.Length.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                "," + BoundingBox.Width.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                "," + BoundingBox.Height.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                ")";
        }

        #region ISealable Members

        public void Seal()
        {
            BoundingBox = new MeshCube
            {
                Length = Components.Max(c => c.RelPosition.X + c.Length),
                Width = Components.Max(c => c.RelPosition.Y + c.Width),
                Height = Components.Max(c => c.RelPosition.Z + c.Height)
            };
            if (ComponentsByID == null)
            {
                ComponentsByID = Components.ToArray();
            }
        }

        #endregion

        #region IDeepCloneable<MeshCubeSet> Members

        public ComponentsSet Clone()
        {
            ComponentsSet clone = new ComponentsSet
            {
                BoundingBox = BoundingBox.Clone(),
                ComponentsByID = ComponentsByID.Select(e => e.Clone()).ToArray()
            };
            clone.Components = clone.ComponentsByID.ToList();
            return clone;
        }

        #endregion

        #region IXmlSerializable Members

        public void LoadXML(XmlNode node)
        {
            // Read components
            int componentID = 0;
            foreach (var childNode in node.FirstChild.ChildNodes.OfType<XmlNode>())
            {
                MeshCube cube = new MeshCube();
                cube.ID = componentID++;
                cube.LoadXML(childNode);
                AddComponent(cube);
            }

            // Seal
            Seal();
        }

        public XmlNode WriteXML(XmlDocument document)
        {
            // Create the element
            XmlNode node = document.CreateElement(ExportationConstants.XML_COMPONENTS_SET_IDENT);

            // Create piece root
            XmlNode cubeComponentRoot = document.CreateElement(ExportationConstants.XML_MESHCUBE_COLLECTION_IDENT);

            // Append pieces
            foreach (var component in Components)
            {
                cubeComponentRoot.AppendChild(component.WriteXML(document));
            }

            // Attach pieces
            node.AppendChild(cubeComponentRoot);

            // Return it
            return node;
        }

        #endregion
    }
}
