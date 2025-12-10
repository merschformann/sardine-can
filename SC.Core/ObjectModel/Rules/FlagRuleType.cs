using System;
using System.Collections.Generic;
using System.Text;

namespace SC.Core.ObjectModel.Rules
{
    /// <summary>
    /// Distinguishes the different types of flag rules.
    /// </summary>
    public enum FlagRuleType
    {
        /// <summary>
        /// All pieces in a container must be of the same type.
        /// </summary>
        Disjoint = 0,
        /// <summary>
        /// At most as many different pieces per type (as given) can be present in a container.
        /// </summary>
        LesserEqualsPieces = 1,
        /// <summary>
        /// At most as many different types (as given) can be present in a container.
        /// </summary>
        LesserEqualsTypes = 2,
    }
}
