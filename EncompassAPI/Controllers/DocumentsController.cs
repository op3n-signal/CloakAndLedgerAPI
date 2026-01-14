using EncompassAPI.AppLayer;
using EncompassAPI.Models;
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
    public class DocumentsController : ControllerBase
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public DocumentsController(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        [HttpPost("autoIndex")]
        public IActionResult AutoIndex(AutoIndexRequest request)
        {
            var token = HttpContext?.Items[CommonFields.EncompassTokenHeaderName]?.ToString();

            // Start background process
            var task = Task.Run(async () =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    // Get a scoped service in THIS scope
                    await using var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();

                    // Run the background process
                    await documentService.AutoIndex(request, token);
                }
            });

            // Return 202 Accepted
            return Accepted();
        }
    }
}
