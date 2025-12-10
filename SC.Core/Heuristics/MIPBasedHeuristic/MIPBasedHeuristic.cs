using SC.Core.Heuristics.PrimalHeuristic;
using SC.Core.ObjectModel;
using SC.Core.ObjectModel.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Core.Heuristics
{
    public class MIPBasedHeuristic : Heuristic
    {
        #region Basics

        public MIPBasedHeuristic(Instance instance, Configuration config) : base(instance, config) { }

        #endregion

        #region Main

        protected override void Solve()
        {
            throw new NotImplementedException();
        }

        //public 

        #endregion

        public override void Cancel()
        {
            throw new NotImplementedException();
        }

        public override bool IsOptimal
        {
            get { throw new NotImplementedException(); }
        }

        public override bool HasSolution
        {
            get { throw new NotImplementedException(); }
        }
    }
}
