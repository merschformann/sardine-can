﻿using SC.ObjectModel.Interfaces;
using SC.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.ObjectModel.Additionals
{
    /// <summary>
    /// Defines basic statistics about a single run of a method
    /// </summary>
    public class PerformanceResult
    {
        /// <summary>
        /// The solution generated by the run
        /// </summary>
        public COSolution Solution { get; set; }

        /// <summary>
        /// The instance solved
        /// </summary>
        public Instance Instance { get; set; }

        /// <summary>
        /// The objective value achieved
        /// </summary>
        public double ObjectiveValue { get; set; }

        /// <summary>
        /// The consumed solution time
        /// </summary>
        public TimeSpan SolutionTime { get; set; }

        /// <summary>
        /// The best bound on the objective value
        /// </summary>
        public double BestBound { get; set; }

        /// <summary>
        /// The gap left between incumbent and best bound
        /// </summary>
        public double Gap { get; set; }

        /// <summary>
        /// Transforms this result to a line that can be written to a file
        /// </summary>
        /// <returns>The result in a line</returns>
        public string GetResultLine()
        {
            return
                SolutionTime.TotalSeconds.ToString(ExportationConstants.FORMATTER) + ExportationConstants.CSV_DELIMITER +
                ObjectiveValue.ToString(ExportationConstants.FORMATTER) + ExportationConstants.CSV_DELIMITER +
                BestBound.ToString(ExportationConstants.FORMATTER) + ExportationConstants.CSV_DELIMITER +
                Gap.ToString(ExportationConstants.FORMATTER);
        }

        /// <summary>
        /// Gets the corresponding headline to the result line
        /// </summary>
        /// <returns>The headline</returns>
        public static string GetResultHeadline()
        {
            return
                "SolutionTime" + ExportationConstants.CSV_DELIMITER +
                "ObjectiveValue" + ExportationConstants.CSV_DELIMITER +
                "BestBound" + ExportationConstants.CSV_DELIMITER +
                "Gap";
        }
    }
}
