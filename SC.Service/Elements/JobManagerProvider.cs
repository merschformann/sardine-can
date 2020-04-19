using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SC.Service.Elements
{
    /// <summary>
    /// Singleton providing the <see cref="Instance"/> instance.
    /// </summary>
    public static class JobManagerProvider
    {
        /// <summary>
        /// The configuration of the service.
        /// </summary>
        internal static IConfiguration Config { get; set; }
        /// <summary>
        /// The job manager instance.
        /// </summary>
        private static JobManager _jobManager = null;
        public static JobManager Instance
        {
            get
            {
                if (_jobManager == null) InitJobManager();
                return _jobManager;
            }
        }

        private static void InitJobManager()
        {
            // Check params
            int threads = Config.GetValue(JobManager.CONFIG_MAX_THREADCOUNT, 1);
            _jobManager = new JobManager(threads) { };
        }
    }
}
