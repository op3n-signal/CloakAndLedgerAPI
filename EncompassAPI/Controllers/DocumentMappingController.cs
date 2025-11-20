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

        /// <summary>
        /// Since this is a document upsert and we are reaching into a specific loan make sure to get a token for the specific org in encompass
        /// To get specific document data you must change the loan data in encompass 
        /// If there is ever an error where a document is not found in document mappings, you must run this api endpoint against that specific loan
        /// IMPORTANT: To run this endpoint, the loan must not be locked and there should be no disclosure log included in the timeline
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("upsert")]
        public async Task<IActionResult> UpsertDocumentMapping(MapRequest request)
        {
            var token = HttpContext?.Items[CommonFields.EncompassTokenHeaderName]?.ToString();

            ProcessResult pr = await _mappingAppService.RunAsync(request, token);

            if (pr.Success)
            {
                return Ok();
            }

            if (pr.Status == ResultStatus.InternalError)
                return Problem(pr.Message);

            return BadRequest(pr.Message);
        }

        /// <summary>
        /// Gets all document mappings
        /// If there are no document mappings for a document you are looking for then either: 
        /// 1) the document does not have signers
        /// 2) the document is not of signature type 'esignable' and might be a wet sign type
        /// 3) the document has not been put into the system yet. if so, run UpsertDocumentMapping against the loan that contains the document
        /// </summary>
        /// <returns></returns>
        [HttpGet("get/loanType/{loanType}/state/{state}/borrowerCount/{borrowerCount}")]
        public async Task<IActionResult> GetDocumentMappings(string loanType, string state, int borrowerCount)
        {
            ProcessResult<List<DocumentSignerMapping>> pr = await _mappingAppService.GetAllDocMappings(loanType, state, borrowerCount);

            if (pr.Success)
            {
                return Ok(pr.Result);
            }
            else
            {
                if(pr.Status == ResultStatus.InternalError)
                    return BadRequest(pr.Message);

                return Problem(pr.Message);
            }
        }
    }
}
