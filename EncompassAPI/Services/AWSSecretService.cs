using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using Microsoft.Extensions.Options;
using Shared;

namespace EncompassAPI.Services
{
    public class AWSSecretService
    {
        private readonly ILogger<AWSSecretService> _logger;
        private readonly AWSConfig _awsConfig;

        public AWSSecretService(ILogger<AWSSecretService> logger, IOptionsSnapshot<AWSConfig> awsOptions)
        {
            _logger = logger;
            _awsConfig = awsOptions.Value;
        }

        public async Task<ProcessResult<T>> GetSecret<T>(string secretPath)
        {
            try
            {
                
                T result = await SecretService.GetSecret<T>(secretPath);

                return new ProcessResult<T>(true)
                {
                    Result = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to retrieve secret. Exception: {ex}");
                return new ProcessResult<T>(false, $"Failed to retrieve secret {secretPath}.");
            }
        }
    }
}
