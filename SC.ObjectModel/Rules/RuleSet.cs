using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SC.ObjectModel.Rules
{
    /// <summary>
    /// Contains all rule definitions to adhere to when solving the problem instance.
    /// </summary>
    public class RuleSet
    {
        /// <summary>
        /// All flag based rules to adhere to.
        /// </summary>
        public List<FlagRule> FlagRules { get; set; } = new List<FlagRule>();

        /// <summary>
        /// Clones this flag rule-set.
        /// </summary>
        /// <returns>The cloned rule-set.</returns>
        public RuleSet Clone() => new RuleSet() { FlagRules = FlagRules.Select(r => r.Clone()).ToList() };
    }
}
