using EncompassAPI.AppLayer;
using TokenGenerator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EncompassAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Security.Policy)]
    public class DisclosuresController : ControllerBase
    {
        private readonly IDisclosuresService _disclosuresService;

        public DisclosuresController(IDisclosuresService disclosuresService)
        {
            _disclosuresService = disclosuresService;
        }

        [HttpGet("Test")]
        public async Task<ActionResult> Test()
        {
            await _disclosuresService.Run();

            return Ok();
        }
    }
}
