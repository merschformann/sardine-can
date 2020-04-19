using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.ObjectModel.Interfaces
{
    /// <summary>
    /// Classes implementing this interface can be cloned deeply. This means the clone equals the origin but shares no references.
    /// </summary>
    /// <typeparam name="T">The type of the object to clone</typeparam>
    public interface IDeepCloneable<T>
    {
        /// <summary>
        /// Clones the object deeply
        /// </summary>
        /// <returns>The clone</returns>
        T Clone();
    }
}
