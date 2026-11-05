using CloakAndLedger.Domain.Interfaces;
using CloakAndLedger.Domain.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CloakAndLedger.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PolicitianController : ControllerBase
    {
        private readonly IPoliticianService _politicianService;

        public PolicitianController(IPoliticianService politicianService)
        {
            _politicianService = politicianService;
        }

        [HttpPost("Add")]
        public async Task<IActionResult> AddPolitician([FromBody] Politician politician)
        {
            var result = await _politicianService.CreatePoliticianAsync(politician);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPolitician(int id)
        {
            var politician = await _politicianService.GetPoliticianByIdAsync(id);
            if (politician == null)
            {
                return NotFound();
            }
            return Ok(politician);
        }

        [HttpPut("Update")]
        public async Task<IActionResult> UpdatePolitician([FromBody] Politician politician)
        {
            var result = await _politicianService.UpdatePoliticianAsync(politician);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePolitician(int id)
        {
            var result = await _politicianService.DeletePoliticianAsync(id);
            return Ok(result);
        }
    }
}
