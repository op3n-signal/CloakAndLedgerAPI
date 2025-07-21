using Microsoft.Extensions.Options;
using EncompassAPI.Models;

namespace EncompassAPI.Services
{
    public class AuthService : IAuthService
    {
        // TODO you have to create the auth Key in the secret manager then put the part of the path here. The beginning of the path will be in app settings.
        private const string SecretsAuthPath = "";
        private AWSConfig _awsConfig;

        public AuthService(IOptionsSnapshot<AWSConfig> awsOptions)
        {
            _awsConfig = awsOptions.Value;
        }

        public async Task<bool> Authenticate(string authKey)
        {
            Auth auth = await AWSService.GetSecret<Auth>(_awsConfig.SecretPath + SecretsAuthPath);

            return authKey == auth.Key;
        }
    }

    public interface IAuthService
    {
        Task<bool> Authenticate(string authKey);
    }
}
