using EncompassAPI.Services;
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
    public class DisclosuresQueueController : ControllerBase
    {
        private readonly IDisclosuresQueueService _queueService;

        public DisclosuresQueueController(IDisclosuresQueueService queueService)
        {
            _queueService = queueService;
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
