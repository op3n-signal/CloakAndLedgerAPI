using EncompassAPI.AppLayer;
using EncompassAPI.Models;
using EncompassAPI.Models.Salesforce;
using EncompassAPI.Utility.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IDisclosureAuditAppService _disclosureAuditAppService;

        public DisclosureAuditController(IDisclosureAuditAppService disclosureAuditAppService)
        {
            _disclosureAuditAppService = disclosureAuditAppService;
        }

        [HttpPost("Start/PostProcessing")]
        public async Task<IActionResult> UpdateDisclosureIntentToProceed(CandidAuditRequest candidAuditRequest)
        {
            var token = HttpContext?.Items[CommonFields.EncompassTokenHeaderName]?.ToString();

            _disclosureAuditAppService.Init(token);

            ProcessResult processResult = await _disclosureAuditAppService.UpdatePostSignatureFields(candidAuditRequest);

            if (!processResult.Success)
            {
                return Problem(processResult.Message);
            }

            return Ok(processResult.Success);
        }

        [HttpPost("Start/PostProcessing/{logId}")]
        public async Task<IActionResult> UpdateDisclosureIntentToProceed(string logId, CandidAuditRequest candidAuditRequest)
        {
            var token = HttpContext?.Items[CommonFields.EncompassTokenHeaderName]?.ToString();

            _disclosureAuditAppService.Init(token, logId);

            ProcessResult processResult = await _disclosureAuditAppService.UpdatePostSignatureFields(candidAuditRequest);

            if (!processResult.Success)
            {
                return Problem(processResult.Message);
            }

            return Ok(processResult.Success);
        }
    }
}
