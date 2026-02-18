using EncompassAPI.Models;
using EncompassAPI.Models.Salesforce;
using EncompassAPI.Services;
using EncompassAPI.Utility.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using System.Text.Json;
using TokenGenerator;

namespace EncompassAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Security.Policy)]
    public class DisclosuresQueueController : ControllerBase
    {
        private readonly IDisclosuresQueueService _queueService;

        public DisclosuresQueueController(IDisclosuresQueueService queueService)
        {
            _queueService = queueService;
        }

        [HttpPost("update/status/{status}")]
        public async Task<IActionResult> UpsertDisclosuresQueue(string status, object request)
        {
            // get ids
            var token = HttpContext?.Items[CommonFields.EncompassTokenHeaderName]?.ToString();
            var disclosureLogId = Request.Headers[CommonFields.DisclosureLogIdHeaderName].ToString();

            bool parsed = Enum.TryParse(status, true, out DisclosureStatus disclosureStatus);

            if (!parsed)
                return BadRequest("Incorrect status type");

            var processResult = await _queueService.UpsertDisclosureQueue(disclosureLogId, token, JsonSerializer.Serialize(request), disclosureStatus);

            if (!processResult.Success)
            {
                return Problem(processResult.Message);
            }

            return Ok();
        }

        [HttpGet("get")]
        public async Task<IActionResult> GetDisclosuresQueue()
        {
            var processResult = await _queueService.GetDisclosureQueueItems();

            if (!processResult.Success)
            {
                //if(processResult.Status == Models.ResultStatus.InternalError) //use later maybe
                    return Problem(processResult.Message);
            }

            return Ok(processResult.Result);
        }
    }
}
