using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SC.ObjectModel.IO.Json;
using SC.Service.Elements;
using SC.Service.Elements.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SC.ObjectModel.IO;
using Swashbuckle.AspNetCore.Filters;

namespace SC.Service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PackingController : ControllerBase
    {
        private readonly ILogger<PackingController> _logger;
        private readonly IJobManager _jobManager;

        public PackingController(ILogger<PackingController> logger, IJobManager jobManager)
        {
            _jobManager = jobManager;
            _logger = logger;
        }

        private const string SUB_CALCULATION_PROBLEMS = "calculations";

        [HttpGet(SUB_CALCULATION_PROBLEMS)]
        public ActionResult<List<JsonJob>> ProblemsGet()
        {
            // Get calculations
            var calcs = _jobManager.GetCalculations();
            // Log
            _logger.LogInformation($"GET calculations (returning {calcs.Count})");
            // Return all known calculations
            return Ok(calcs);
        }

        [HttpGet(SUB_CALCULATION_PROBLEMS + "/{id:int}")]
        public ActionResult<JsonJob> ProblemsGet(int id)
        {
            // Get calculation by ID
            var problem = _jobManager.GetCalculation(id);
            // Log
            _logger.LogInformation($"GET calculation by ID ({id}, present: {problem != null})");
            // Return calculation with given ID or not found (if not present)
            if (problem == null) return NotFound();
            else return Ok(problem);
        }

        [HttpPost(SUB_CALCULATION_PROBLEMS)]
        [SwaggerRequestExample(typeof(JsonJob), typeof(JsonJobExample))]
        public ActionResult<JsonStatus> ProblemsPost([FromBody] JsonJob instance)
        {
            // Sanity check job
            var inputErr = JsonIO.Validate(instance.Instance);
            if (inputErr != null)
            {
                _logger.LogWarning($"POST calculation had error: {inputErr}");
                return BadRequest(inputErr);
            }
            // Create calculation job
            int calcId = _jobManager.GetNextId();
            var calc = new Calculation(calcId, instance, null);
            if (calc.Problem.Configuration == null) // Set a default config, if none is given
                calc.Problem.Configuration = new ObjectModel.Configuration.Configuration(ObjectModel.MethodType.ExtremePointInsertion, false);
            calc.Status.ProblemUrl = $"{SUB_CALCULATION_PROBLEMS}/{calcId}";
            calc.Status.StatusUrl = $"{SUB_CALCULATION_PROBLEMS}/{calcId}/status";
            calc.Status.SolutionUrl = $"{SUB_CALCULATION_PROBLEMS}/{calcId}/solution";
            // Log
            _logger.LogInformation($"POST calculation (got ID {calcId})");
            // Enqueue the problem
            _jobManager.Enqueue(calc);
            return Ok(calc.Status);
        }

        [HttpGet(SUB_CALCULATION_PROBLEMS+"/status")]
        public ActionResult<List<JsonStatus>> StatusGet()
        {
            // Get status of all calculations
            var status = _jobManager.GetStatus();
            // Log
            _logger.LogInformation($"GET status (returning {status.Count})");
            // Return all known calculations
            return Ok(status);
        }

        [HttpGet(SUB_CALCULATION_PROBLEMS + "/{id:int}/status")]
        public ActionResult<JsonStatus> StatusGet(int id)
        {
            // Get status of calculation by ID
            var status = _jobManager.GetStatus(id);
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
            var solution = _jobManager.GetSolution(id);
            // Log
            _logger.LogInformation($"GET solution by ID ({id}, present: {solution != null})");
            // Return status of calculation with given ID or not found (if not present)
            if (solution == null) return NotFound();
            else return Ok(solution);
        }
    }
}
