using System;
using System.Collections.Generic;
using System.Text;

namespace SC.Core.ObjectModel.Rules
{
    /// <summary>
    /// Stores a rule for handling a particular flag.
    /// </summary>
    public class FlagRule
    {
        /// <summary>
        /// The ID of the flag controlled by this rule.
        /// </summary>
        public int FlagId { get; set; }
        /// <summary>
        /// The type of this rule.
        /// </summary>
        public FlagRuleType RuleType { get; set; }
        /// <summary>
        /// The parameter to use with this rule.
        /// E.g., lesser or equal different pieces as this per type can be present in a container.
        /// </summary>
        public int Parameter { get; set; }

        /// <summary>
        /// Clones this flag rule definition.
        /// </summary>
        /// <returns>The cloned rule.</returns>
        public FlagRule Clone() => new FlagRule() { FlagId = FlagId, RuleType = RuleType, Parameter = Parameter };

        /// <summary>
        /// Returns an informative string representation of this object.
        /// </summary>
        /// <returns>An informative string representing this object.</returns>
        public override string ToString() => $"({FlagId},{RuleType},{Parameter}";
    }
}
