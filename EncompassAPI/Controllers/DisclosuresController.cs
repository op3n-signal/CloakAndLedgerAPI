using EncompassAPI.AppLayer;
using EncompassAPI.Models;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Utility.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shared.Models;
using System.Threading.Tasks;
using TokenGenerator;

namespace EncompassAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Security.Policy)]
    [AuthRequired]
    public class DisclosuresController : ControllerBase
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public DisclosuresController(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        [HttpPost("v2/Retrieve")]
        public IActionResult RetrieveDisclosuresV2(CandidIncomingRequest request)
        {
            var token = HttpContext?.Items[CommonFields.EncompassTokenHeaderName]?.ToString();

            // Start background process
            var task = Task.Run(async () =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    // Get a scoped service in THIS scope
                    await using var disclosuresService = scope.ServiceProvider.GetRequiredService<IDisclosuresServiceV2>();

                    disclosuresService.Init(token);

                    // Run the background process
                    await disclosuresService.Retrieve(request);
                }
            });

            // Return 202 Accepted
            return Accepted();
        }

        [HttpPost("v3/Retrieve")]
        public IActionResult RetrieveDisclosuresV3(CandidIncomingRequest request)
        {
            var token = HttpContext?.Items[CommonFields.EncompassTokenHeaderName]?.ToString();

            // Start background process
            var task = Task.Run(async () =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    // Get a scoped service in THIS scope
                    await using var disclosuresService = scope.ServiceProvider.GetRequiredService<IDisclosuresServiceV3>();

                    disclosuresService.Init(token);

                    // Run the background process
                    await disclosuresService.Retrieve(request);
                }
            });

            // Return 202 Accepted
            return Accepted();
        }

        [HttpPost("Retrieve")]
        public IActionResult RetrieveDisclosures(CandidIncomingRequest request)
        {
            var token = HttpContext?.Items[CommonFields.EncompassTokenHeaderName]?.ToString();

            // Start background process
            var task = Task.Run(async () =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    // Get a scoped service in THIS scope
                    await using var disclosuresService = scope.ServiceProvider.GetRequiredService<IDisclosuresServiceV4>();

                    disclosuresService.Init(token);

                    // Run the background process
                    await disclosuresService.Retrieve(request);
                }
            });

            // Return 202 Accepted
            return Accepted();
        }

        [HttpPost("Retrieve/{logId}")]
        public IActionResult RetrieveDisclosures(string logId, CandidIncomingRequest request)
        {
            var token = HttpContext?.Items[CommonFields.EncompassTokenHeaderName]?.ToString();

            // Start background process
            var task = Task.Run(async () =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    // Get a scoped service in THIS scope
                    await using var disclosuresService = scope.ServiceProvider.GetRequiredService<IDisclosuresServiceV4>();

                    disclosuresService.Init(token, logId);

                    // Run the background process
                    await disclosuresService.Retrieve(request);
                }
            });

            // Return 202 Accepted
            return Accepted();
        }
    }
}
