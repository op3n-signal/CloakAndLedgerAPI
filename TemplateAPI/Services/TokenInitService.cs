
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared;
using Template.Models;
using Template.Models.Configs;

namespace Template.Services
{
    public class TokenInitService : IHostedService
    {
        private readonly TokenProvider _tokenProvider;
        private readonly IServiceProvider _serviceProvider;

        public TokenInitService(TokenProvider tokenProvider, IServiceProvider serviceProvider)
        {
            _tokenProvider = tokenProvider;
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var awsConfig = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<AWSConfig>>().Value;
                await _tokenProvider.InitializeAsync(awsConfig);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public class TokenProvider
    {
        public SecurityKeys SecurityKeys { get; private set; }

        public async Task InitializeAsync(AWSConfig config)
        {
            SecurityKeys = await RetrieveTokenAsync(config.SecurityKeysPath);
        }

        private async Task<SecurityKeys> RetrieveTokenAsync(string secretPath)
        {
            return await SecretService.GetSecret<SecurityKeys>(secretPath);
        }
    }
}
