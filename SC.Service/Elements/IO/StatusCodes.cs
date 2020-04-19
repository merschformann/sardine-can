using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SC.Service.Elements.IO
{
    /// <summary>
    /// Distinguishes the different status a calculation can be in.
    /// </summary>
    public enum StatusCodes
    {
        Pending = 0,
        Ongoing = 1,
        Done = 2,
        Error = 3,
    }
}
