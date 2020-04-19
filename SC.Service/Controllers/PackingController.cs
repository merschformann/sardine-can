using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SC.ObjectModel.IO.Json;
using SC.Service.Elements;
using SC.Service.Elements.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SC.Service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PackingController : ControllerBase
    {
        private readonly ILogger<PackingController> _logger;

        public PackingController(ILogger<PackingController> logger)
        {
            _logger = logger;
        }

        private const string SUB_CALCULATION_PROBLEMS = "calculations";

        [HttpGet(SUB_CALCULATION_PROBLEMS)]
        public ActionResult<List<JsonCalculation>> ProblemsGet()
        {
            // Get calculations
            var calcs = JobManagerProvider.Instance.GetCalculations();
            // Log
            _logger.LogInformation($"GET calculations (returning {calcs.Count})");
            // Return all known calculations
            return Ok(calcs);
        }

        [HttpGet(SUB_CALCULATION_PROBLEMS + "/{id:int}")]
        public ActionResult<JsonCalculation> ProblemsGet(int id)
        {
            // Get calculation by ID
            var problem = JobManagerProvider.Instance.GetCalculation(id);
            // Log
            _logger.LogInformation($"GET calculation by ID ({id}, present: {problem != null})");
            // Return calculation with given ID or not found (if not present)
            if (problem == null) return NotFound();
            else return Ok(problem);
        }

        [HttpPost(SUB_CALCULATION_PROBLEMS)]
        public ActionResult<JsonStatus> ProblemsPost([FromBody] JsonCalculation instance)
        {
            // Create calculation job
            int calcId = JobManagerProvider.Instance.GetNextId();
            var calc = new Calculation(calcId, instance, (string msg) => _logger.LogInformation(msg));
            if (calc.Problem.Configuration == null) // Set a default config, if none is given
                calc.Problem.Configuration = new ObjectModel.Configuration.Configuration(ObjectModel.MethodType.ExtremePointInsertion, false);
            calc.Status.ProblemUrl = $"{SUB_CALCULATION_PROBLEMS}/{calcId}";
            calc.Status.StatusUrl = $"{SUB_CALCULATION_PROBLEMS}/{calcId}/status";
            calc.Status.SolutionUrl = $"{SUB_CALCULATION_PROBLEMS}/{calcId}/solution";
            // Log
            _logger.LogInformation($"POST calculation (got ID {calcId})");
            // Enqueue the problem
            JobManagerProvider.Instance.Enqueue(calc);
            return Ok(calc.Status);
        }

        [HttpGet(SUB_CALCULATION_PROBLEMS + "/{id:int}/status")]
        public ActionResult<JsonStatus> StatusGet(int id)
        {
            // Get status of calculation by ID
            var status = JobManagerProvider.Instance.GetStatus(id);
            // Log
            _logger.LogInformation($"GET status by ID ({id}, present: {status != null})");
            // Return status of calculation with given ID or not found (if not present)
            if (status == null) return NotFound();
            else return Ok(status);
        }

        [HttpGet(SUB_CALCULATION_PROBLEMS + "/{id:int}/solution")]
        public ActionResult<JsonSolution> ResultGet(int id)
        {
            // Get solution of calculation by ID
            var solution = JobManagerProvider.Instance.GetSolution(id);
            // Log
            _logger.LogInformation($"GET solution by ID ({id}, present: {solution != null})");
            // Return status of calculation with given ID or not found (if not present)
            if (solution == null) return NotFound();
            else return Ok(solution);
        }
    }
}
