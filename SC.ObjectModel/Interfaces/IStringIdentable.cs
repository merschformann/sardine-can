using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.ObjectModel.Interfaces
{
    /// <summary>
    /// Defines an object as string-identable. This means that a simple identification of the object can be written to a string.
    /// </summary>
    public interface IStringIdentable
    {
        /// <summary>
        /// The ID of the object
        /// </summary>
        int ID { get; set; }

        /// <summary>
        /// Generates the identification string
        /// </summary>
        /// <returns>The identification string</returns>
        string ToIdentString();
    }
}
