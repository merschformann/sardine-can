using System.Collections.Generic;
using SC.ObjectModel.IO.Json;
using SC.Service.Elements.IO;

namespace SC.Service.Elements
{
    /// <summary>
    /// Defines job-manager functionality.
    /// </summary>
    public interface IJobManager
    {
        /// <summary>
        /// Returns all calculations currently present.
        /// </summary>
        /// <returns>All calculations (pending, ongoing & completed).</returns>
        List<JsonJob> GetCalculations();
        /// <summary>
        /// Gets a calculation.
        /// </summary>
        /// <param name="id">The id of the calculation.</param>
        /// <returns>The calculation, or <code>null</code> if the calculation is unknown.</returns>
        JsonJob GetCalculation(int id);
        /// <summary>
        /// Obtains a new ID for another calculation job.
        /// </summary>
        /// <returns></returns>
        int GetNextId();
        /// <summary>
        /// Enqueues a new calculation job.
        /// </summary>
        /// <param name="calc">The calculation job.</param>
        void Enqueue(Calculation calc);
        /// <summary>
        /// Returns all calculations status' currently present.
        /// </summary>
        /// <returns>All status elements of present calculations.</returns>
        List<JsonStatus> GetStatus();
        /// <summary>
        /// Gets the status of a calculation.
        /// </summary>
        /// <param name="id">The id of the calculation.</param>
        /// <returns>The status associated with the calculation, or <code>null</code> if the calculation is unknown.</returns>
        JsonStatus GetStatus(int id);
        /// <summary>
        /// Gets the solution of a calculation.
        /// </summary>
        /// <param name="id">The id of the calculation.</param>
        /// <returns>The solution associated with the calculation, or <code>null</code> if there is none.</returns>
        JsonSolution GetSolution(int id);
    }
}
