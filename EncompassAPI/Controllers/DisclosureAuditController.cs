using Amazon.Runtime.Internal;
using EncompassAPI.AppLayer;
using EncompassAPI.Models;
using EncompassAPI.Models.Salesforce;
using EncompassAPI.Utility.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shared.Models;
using TokenGenerator;

namespace EncompassAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Security.Policy)]
    [AuthRequired]
    public class DisclosureAuditController : ControllerBase
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public DisclosureAuditController(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        [HttpPost("Start/PostProcessing")]
        public async Task<IActionResult> UpdateDisclosureIntentToProceed(CandidAuditRequest candidAuditRequest)
        {
            var token = HttpContext?.Items[CommonFields.EncompassTokenHeaderName]?.ToString();


            // Start background process
            var task = Task.Run(async () =>
            {
                try
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        // Get a scoped service in THIS scope
                        await using var disclosureAuditAppService = scope.ServiceProvider.GetRequiredService<IDisclosureAuditAppService>();

                        disclosureAuditAppService.Init(token);

                        // Run the background process
                        ProcessResult processResult = await disclosureAuditAppService.UpdatePostSignatureFields(candidAuditRequest);
                    }
                }
                catch (Exception ex)
                {
                    // Put a breakpoint here or log to file
                    Console.WriteLine("Retrieve background exception: " + ex);
                }
            });

            // Return 202 Accepted
            return Accepted();
        }

        [HttpPost("Start/PostProcessing/{logId}")]
        public async Task<IActionResult> UpdateDisclosureIntentToProceed(string logId, CandidAuditRequest candidAuditRequest)
        {
            var token = HttpContext?.Items[CommonFields.EncompassTokenHeaderName]?.ToString();

            // Start background process
            var task = Task.Run(async () =>
            {
                try
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        // Get a scoped service in THIS scope
                        await using var disclosureAuditAppService = scope.ServiceProvider.GetRequiredService<IDisclosureAuditAppService>();

                        disclosureAuditAppService.Init(token, logId);

                        // Run the background process
                        ProcessResult processResult = await disclosureAuditAppService.UpdatePostSignatureFields(candidAuditRequest);
                    }
                }
                catch (Exception ex)
                {
                    // Put a breakpoint here or log to file
                    Console.WriteLine("Retrieve background exception: " + ex);
                }
            });

            // Return 202 Accepted
            return Accepted();
        }
    }
}
