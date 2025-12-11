using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Core.ObjectModel.Interfaces
{
    /// <summary>
    /// Defines an object as sealable. This means that the object has to be sealed first before it is ready
    /// </summary>
    public interface ISealable
    {
        /// <summary>
        /// Seal the object (rendering it ready)
        /// </summary>
        void Seal();
    }
}
