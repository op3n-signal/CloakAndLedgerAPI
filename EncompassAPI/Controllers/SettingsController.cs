using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Services;
using EncompassAPI.Utility.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Models;
using System.Text.Json;
using TokenGenerator;

namespace EncompassAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Security.Policy)]
    [AuthRequired]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(string))]
    public class SettingsController : ControllerBase
    {
        private readonly AWSConfig _awsConfig;
        private readonly LoggerService<SettingsController> _logger;

        public SettingsController(IOptionsSnapshot<AWSConfig> awsOptions, LoggerService<SettingsController> logger)
        {
            _awsConfig = awsOptions.Value;
            _logger = logger;
        }

        [HttpPost("Add/{instanceId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        public async Task<IActionResult> AddSecret(string instanceId, EncompassCredentials credentials)
        {
            try
            {
                // Insert secret into AWS Secrets Manager by environment and candid org id
                string arn = await SecretService.CreateSecret($"{_awsConfig.SecretPath}/encompass/credentials/{instanceId}", JsonSerializer.Serialize(credentials));

                if(string.IsNullOrEmpty(arn))
                {
                    return BadRequest(CommonStrings.ErrorCreatingSecret);
                }

                return Ok(arn);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"{CommonStrings.ErrorCreatingSecret}{Environment.NewLine}Exception: {ex}");
                return Problem(CommonStrings.ErrorCreatingSecret);
            }
        }

        [HttpGet("Get/{instanceId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EncompassCredentials))]
        public async Task<IActionResult> GetSecret(string instanceId)
        {
            try
            {
                // Insert secret into AWS Secrets Manager by environment and candid org id
                EncompassCredentials encompassCredentials = await SecretService.GetSecret<EncompassCredentials>($"{_awsConfig.SecretPath}/encompass/credentials/{instanceId}");

                if (encompassCredentials == null)
                {
                    return BadRequest(CommonStrings.ErrorRetrievingSecret);
                }

                return Ok(encompassCredentials);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"{CommonStrings.ErrorRetrievingSecret}{Environment.NewLine}Exception: {ex}");
                return Problem(CommonStrings.ErrorRetrievingSecret);
            }
        }
    }
}
