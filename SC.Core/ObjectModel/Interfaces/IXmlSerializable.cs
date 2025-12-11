using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SC.Core.ObjectModel.Interfaces
{
    /// <summary>
    /// Defines an object as serializable to XML
    /// </summary>
    public interface IXmlSerializable
    {
        /// <summary>
        /// Loads the object from XML
        /// </summary>
        /// <param name="node">The node containing the objects information</param>
        void LoadXML(XmlNode node);

        /// <summary>
        /// Writes the object to XML
        /// </summary>
        /// <param name="document">The document to append the object to</param>
        /// <returns>The XML-node containing the object</returns>
        XmlNode WriteXML(XmlDocument document);
    }
}
