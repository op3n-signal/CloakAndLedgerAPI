using EncompassAPI.AppLayer;
using EncompassAPI.Models;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Utility.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
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
            // Start background process
            var task = Task.Run(() =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    // Get a scoped service in THIS scope
                    var disclosuresService = scope.ServiceProvider.GetRequiredService<IDisclosuresService>();

                    // Run the background process
                    disclosuresService.Retrieve(request).GetAwaiter().GetResult();
                }
            });

            // Return 202 Accepted
            return Accepted();
        }
    }
}
