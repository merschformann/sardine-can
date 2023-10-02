using Microsoft.Extensions.Logging;
using SC.CLI;
using SC.ObjectModel;
using SC.ObjectModel.Additionals;
using SC.ObjectModel.IO.Json;
using SC.Service.Elements.IO;
using SC.Toolbox;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SC.Service.Elements
{
    /// <summary>
    /// Manages the currently ongoing jobs.
    /// </summary>
    public class JobManager : IJobManager
    {
        /// <summary>
        /// The param name for the maximal number of threads to be used overall.
        /// </summary>
        public const string CONFIG_MAX_THREADCOUNT = "MaxThreads";

        /// <summary>
        /// The maximal number of completed jobs to keep track of before throwing them away.
        /// </summary>
        public const int MAX_COMPLETED_JOBS = 1000;

        public JobManager(IConfiguration configuration)
        {
            // READ CONFIG - env variables take precedence over config vars
            // Read max threads configuration
            var threads = configuration.GetValue(CONFIG_MAX_THREADCOUNT, 1) ;
            var envThreadsGiven = int.TryParse(Environment.GetEnvironmentVariable("MAX_THREADS"), out var envThreads);
            if (envThreadsGiven)
                threads = envThreads;
            // Init
            _threadCount = threads <= 0 ? Environment.ProcessorCount : threads;
            _logTimer = new Timer(new TimerCallback(LogCallback), null, 500, 5000);
        }


        #region Simple logging

        /// <summary>
        /// The timer object for periodic logging.
        /// </summary>
        private readonly Timer _logTimer;

        /// <summary>
        /// The logging callback to use (or <code>null</code> to silence it).
        /// </summary>
        internal ILogger Logger { get; set; }

        /// <summary>
        /// The log callback that is invoked periodically.
        /// </summary>
        /// <param name="state">Not used.</param>
        private void LogCallback(object state)
        {
            Logger?.LogInformation($"{DateTime.Now.ToString("s", CultureInfo.InvariantCulture)}: Jobs: {_backlog.Count}, {_pending.Count}, {_ongoing.Count}, {_completed.Count} (backlog, pending, ongoing, completed)");
        }

        #endregion

        /// <summary>
        /// Manages the access to the backlog. Allows multiple parallel reads, but only one write at a time.
        /// </summary>
        private ReaderWriterLockSlim _backlogAccess = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// The maximal number of threads used.
        /// </summary>
        private readonly int _threadCount;

        /// <summary>
        /// Manages the wait position entry. Jobs either need to wait in that position or can immediately start, if sufficient threads are available.
        /// </summary>
        private object _waitPositionLock = new object();

        /// <summary>
        /// Indicates whether a calculation is currently waiting for resources.
        /// </summary>
        private bool _waiting = false;

        /// <summary>
        /// Manages access to the number of threads in use.
        /// </summary>
        private object _threadCountLock = new object();

        /// <summary>
        /// The number of threads currently in use.
        /// </summary>
        private int _threadsInUse = 0;

        /// <summary>
        /// The calculation backlog.
        /// </summary>
        private readonly List<Calculation> _backlog = new List<Calculation>();

        /// <summary>
        /// All currently waiting/imminent jobs.
        /// </summary>
        private readonly HashSet<Calculation> _pending = new HashSet<Calculation>();

        /// <summary>
        /// All currently calculating jobs.
        /// </summary>
        private readonly HashSet<Calculation> _ongoing = new HashSet<Calculation>();

        /// <summary>
        /// The list of completed calculations.
        /// </summary>
        private readonly List<Calculation> _completed = new List<Calculation>();

        /// <summary>
        /// A map translating the job ID to the calculation instance.
        /// </summary>
        private readonly Dictionary<int, Calculation> _idToCalculation = new Dictionary<int, Calculation>();

        /// <summary>
        /// The current problem instance ID.
        /// </summary>
        private int _currentId = 0;

        /// <summary>
        /// Obtains a new ID for another calculation job.
        /// </summary>
        /// <returns></returns>
        public int GetNextId()
        {
            // We need to synchronize all write processes and also the read access
            _backlogAccess.EnterWriteLock();
            // Get next ID
            int newId = _currentId++;
            // Release the lock on the resource
            _backlogAccess.ExitWriteLock();
            // Return it
            return newId;
        }

        /// <summary>
        /// Enqueues a new calculation job.
        /// </summary>
        /// <param name="calc">The calculation job.</param>
        public void Enqueue(Calculation calc)
        {
            // We need to synchronize all write processes and also the read access
            _backlogAccess.EnterWriteLock();
            // Insert into backlog
            _backlog.AddSorted(calc);
            _idToCalculation[calc.Status.Id] = calc;
            // Release the lock on the resource
            _backlogAccess.ExitWriteLock();
            // See whether we can immediately start the job (fire and forget)
            Task.Run(EnterNextJob);
        }

        /// <summary>
        /// Returns the next calculation problem in queue.
        /// </summary>
        /// <returns>The next problem to </returns>
        public Calculation Fetch()
        {
            // We need to synchronize all write processes and also the read access
            _backlogAccess.EnterWriteLock();
            // Get next job
            Calculation calc = _backlog.FirstOrDefault();
            // If there is a next job, remove it from the backlog
            if (calc != null)
            {
                // Move to ongoing
                _backlog.RemoveAt(0);
                _pending.Add(calc);
                // Update status
                calc.Status.Status = StatusCodes.Ongoing;
            }

            // Release the lock on the resource
            _backlogAccess.ExitWriteLock();
            // Return it
            return calc;
        }

        /// <summary>
        /// Marks a calculation done.
        /// </summary>
        /// <param name="calc">The calculation that was completed.</param>
        public void Complete(Calculation calc)
        {
            // We need to synchronize all write processes and also the read access
            _backlogAccess.EnterWriteLock();
            // Mark calculation completed
            _ongoing.Remove(calc);
            _completed.Add(calc);
            // Remove oldest jobs, if too many
            if (_completed.Count > MAX_COMPLETED_JOBS)
            {
                var overflowJob = _completed[0];
                _completed.RemoveAt(0);
                _idToCalculation.Remove(overflowJob.Id);
            }

            // Update status
            calc.Status.Status = StatusCodes.Done;
            // Release the lock on the resource
            _backlogAccess.ExitWriteLock();
            // See whether we can enter another job (fire and forget)
            Task.Run(EnterNextJob);
        }

        /// <summary>
        /// Returns all calculations currently present.
        /// </summary>
        /// <returns>All calculations (pending, ongoing & completed).</returns>
        public List<JsonJob> GetCalculations()
        {
            // Init
            List<JsonJob> calculations = null;
            // We need to synchronize access to the backlog
            _backlogAccess.EnterReadLock();
            try
            {
                calculations = _backlog.Select(c => c.Problem)
                    .Concat(_pending.Select(c => c.Problem))
                    .Concat(_ongoing.Select(c => c.Problem))
                    .Concat(_completed.Select(c => c.Problem))
                    .ToList();
            }
            finally
            {
                // Release the lock on the resource
                _backlogAccess.ExitReadLock();
            }

            // Return result
            return calculations;
        }

        /// <summary>
        /// Gets a calculation.
        /// </summary>
        /// <param name="id">The id of the calculation.</param>
        /// <returns>The calculation, or <code>null</code> if the calculation is unknown.</returns>
        public JsonJob GetCalculation(int id)
        {
            // Init
            JsonJob problem = null;
            // We need to synchronize access to the backlog
            _backlogAccess.EnterReadLock();
            try
            {
                _idToCalculation.TryGetValue(id, out Calculation calc);
                problem = calc?.Problem;
            }
            finally
            {
                // Release the lock on the resource
                _backlogAccess.ExitReadLock();
            }

            // Return result
            return problem;
        }

        /// <summary>
        /// Returns all calculations status' currently present.
        /// </summary>
        /// <returns>All status elements of present calculations.</returns>
        public List<JsonStatus> GetStatus()
        {
            // Init
            List<JsonStatus> status = null;
            // We need to synchronize access to the backlog
            _backlogAccess.EnterReadLock();
            try
            {
                status = _backlog.Select(c => c.Status)
                    .Concat(_pending.Select(c => c.Status))
                    .Concat(_ongoing.Select(c => c.Status))
                    .Concat(_completed.Select(c => c.Status))
                    .ToList();
            }
            finally
            {
                // Release the lock on the resource
                _backlogAccess.ExitReadLock();
            }

            // Return result
            return status;
        }

        /// <summary>
        /// Gets the status of a calculation.
        /// </summary>
        /// <param name="id">The id of the calculation.</param>
        /// <returns>The status associated with the calculation, or <code>null</code> if the calculation is unknown.</returns>
        public JsonStatus GetStatus(int id)
        {
            // Init
            JsonStatus status = null;
            // We need to synchronize access to the backlog
            _backlogAccess.EnterReadLock();
            try
            {
                _idToCalculation.TryGetValue(id, out Calculation calc);
                status = calc?.Status;
            }
            finally
            {
                // Release the lock on the resource
                _backlogAccess.ExitReadLock();
            }

            // Return result
            return status;
        }

        /// <summary>
        /// Gets the solution of a calculation.
        /// </summary>
        /// <param name="id">The id of the calculation.</param>
        /// <returns>The solution associated with the calculation, or <code>null</code> if there is none.</returns>
        public JsonSolution GetSolution(int id)
        {
            // Init
            JsonSolution solution = null;
            // We need to synchronize access to the backlog
            _backlogAccess.EnterReadLock();
            try
            {
                _idToCalculation.TryGetValue(id, out Calculation calc);
                solution = calc?.Solution;
            }
            finally
            {
                // Release the lock on the resource
                _backlogAccess.ExitReadLock();
            }

            // Return result
            return solution;
        }

        private void EnterNextJob()
        {
            // Try to get hold of waiting position
            var mine = false;
            lock (_waitPositionLock)
                if (!_waiting)
                {
                    _waiting = true;
                    mine = true;
                }

            // If we can't access the waiting position, quit
            if (!mine) return;
            
            // Get next job
            var nextJob = Fetch();
            // Put it in waiting position, if there is another job
            if (nextJob != null)
                Calculate(nextJob);
            else
                lock (_waitPositionLock)
                    _waiting = false;
        }

        private void Calculate(Calculation calc)
        {
            // Limit the threads for the job
            int threads = calc.Problem.Configuration.ThreadLimit = calc.Problem.Configuration.ThreadLimit <= 0 || calc.Problem.Configuration.ThreadLimit > _threadCount
                ?
                // On unlimited/out-of-bounds threads use the maximal capacity of the service
                _threadCount
                :
                // Otherwise use the thread-limit of the job
                calc.Problem.Configuration.ThreadLimit;

            try
            {
                // Wait for sufficient thread count
                while (_threadCount - _threadsInUse < threads)
                    Thread.Sleep(500);
                // Grab necessary threads
                lock (_threadCountLock)
                    _threadsInUse += threads;
                // Release waiting position
                lock (_waitPositionLock)
                    _waiting = false;
                // We need to synchronize all write processes and also the read access
                _backlogAccess.EnterWriteLock();
                // Move job to ongoing
                _pending.Remove(calc);
                _ongoing.Add(calc);
                // Release the lock on the resource
                _backlogAccess.ExitWriteLock();
                // Trigger next job to enter waiting position (fire and forget)
                Task.Run(EnterNextJob);
                // Log start
                Logger?.LogInformation($"Starting job {calc.Status.Id}");
                // Execute this job
                var result = Executor.Execute(Instance.FromJsonInstance(calc.Problem.Instance), calc.Problem.Configuration, calc.Logger);
                // Log done
                Logger?.LogInformation($"Finished job {calc.Status.Id} in {result.SolutionTime.TotalSeconds:F0} s");
                // Complete solution
                calc.Solution = result.Solution.ToJsonSolution();
                Complete(calc);
                // Trigger next job to enter waiting position again (fire and forget)
                Task.Run(EnterNextJob);
            }
            catch (Exception ex)
            {
                // We need to synchronize all write processes and also the read access
                _backlogAccess.EnterWriteLock();
                // Remove from sets
                _pending.Remove(calc);
                _ongoing.Remove(calc);
                // Release the lock on the resource
                _backlogAccess.ExitWriteLock();
                // Mark erroneous
                calc.Status.Status = StatusCodes.Error;
                calc.Status.ErrorMessage =
                    ex.Message + Environment.NewLine +
                    ex.StackTrace + Environment.NewLine;
                // Log
                Logger.LogError($"Exception: {ex.Message}");
                Logger.LogError($"StackTrace:\n{ex.StackTrace}");
            }
            finally
            {
                // Release grabbed threads
                lock (_threadCountLock)
                    _threadsInUse -= threads;
            }
        }
    }
}
