using EncompassAPI.AppLayer;
using EncompassAPI.Models;
using EncompassAPI.Models.Salesforce;
using EncompassAPI.Services;
using EncompassAPI.Utility.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using TokenGenerator;

namespace EncompassAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Security.Policy)]
    [AuthRequired]
    public class DocumentMappingController : ControllerBase
    {
        private readonly IMappingAppService _mappingAppService;

        public DocumentMappingController(IMappingAppService mappingAppService)
        {
            _mappingAppService = mappingAppService;
        }

        [HttpPost("upsert")]
        public async Task UpsertDocumentMapping(MapRequest request)
        {
            // Since this is a document upsert and we are reaching into a specific loan make sure to get a token for the specific org in encompass
            // To get specific document data you must change the loan data in encompass 
            // If there is ever an error where a document is not found in document mappings, you must run this api endpoint against that specific loan
            var token = HttpContext?.Items[CommonFields.EncompassTokenHeaderName]?.ToString();

            await _mappingAppService.RunAsync(request, token);
        }
    }
}
