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
                    await using var disclosuresService = scope.ServiceProvider.GetRequiredService<IDisclosuresServiceV2>();

                    disclosuresService.Init(token);

                    // Run the background process
                    await disclosuresService.Retrieve(request);
                }
            });

            // Return 202 Accepted
            return Accepted();
        }
    }
}
