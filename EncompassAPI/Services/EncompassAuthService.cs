using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Repos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Shared;
using Shared.Models;
using System;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace EncompassAPI.Services
{
    public class EncompassAuthService : IEncompassAuthService
    {
        private readonly AWSConfig _awsConfig;
        private readonly EncompassConfig _encompassConfig;
        private readonly IHttpService _http;
        private readonly ITokenRepo _repo;
        private readonly ILogger<EncompassAuthService> _logger;

        public EncompassAuthService(IOptionsSnapshot<AWSConfig> awsOptions, IOptionsSnapshot<EncompassConfig> encompassConfig, IHttpService http, ITokenRepo repo, ILogger<EncompassAuthService> logger)
        {
            _awsConfig = awsOptions.Value;
            _encompassConfig = encompassConfig.Value;
            _http = http;
            _repo = repo;
            _logger = logger;
        }

        public async Task<ProcessResult<string>> CheckAndGetAccessToken(string instanceId)
        {
            try
            {
                EncompassCredentials credentials = await SecretService.GetSecret<EncompassCredentials>($"{_awsConfig.SecretPath}/encompass/credentials/{instanceId}");

                // Convert secrets to base64 for Basic Authentication
                byte[] byteArray = Encoding.ASCII.GetBytes($"{credentials.client_id}:{credentials.client_secret}");
                string authorizationBase64 = Convert.ToBase64String(byteArray);
                //_http.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authorizationBase64);

                // Get the token info from the database
                EncompassTokenInfo[] tokenInfo = _repo.Get(credentials.org_id);

                // Only run introspection if we have an access token
                if (tokenInfo[0] != null || !string.IsNullOrEmpty(tokenInfo[0].Access_Token))
                {
                    bool blIsValid = await CheckAccessTokenStatus(credentials.org_id, tokenInfo[0].Access_Token);

                    if (blIsValid)
                        return new ProcessResult<string>(true, tokenInfo[0].Access_Token);
                }

                // Let's get a new access token since access token arguement doesn't exist or is invalid
                EncompassTokenInfo encompassTokenInfo = await GetAccessToken(credentials.org_id, instanceId);

                // Save the new access token to the database
                _repo.Upsert(encompassTokenInfo);

                return new ProcessResult<string>(true, encompassTokenInfo.Access_Token);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{CommonFields.CannotRetrieveAccessToken}{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<string>(false, CommonFields.CannotRetrieveAccessToken);
            }
        }

        private async Task<EncompassTokenInfo> GetAccessToken(string orgId, string instanceId)
        {
            FormUrlEncodedContent tokenRequestBody = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "scope", "lp" },
                { "instance_id", instanceId }
            });

            EncompassAccessToken token = await _http.PostAsync<EncompassAccessToken>(_encompassConfig.TokenEndpoint, tokenRequestBody);

            if(token == null || string.IsNullOrEmpty(token.access_token))
            {
                _logger.LogError(CommonFields.CannotRetrieveAccessToken);
                return null;
            }

            return new EncompassTokenInfo
            {
                Access_Token = token.access_token,
                Org_Id = orgId
            };
        }

        private async Task<bool> CheckAccessTokenStatus(string orgId, string accessToken)
        {
            try
            {
                FormUrlEncodedContent introspectionRequestBody = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "token", accessToken }
                });

                EncompassIntrospection introspection = await _http.PostAsync<EncompassIntrospection>(_encompassConfig.TokenEndpoint, introspectionRequestBody);

                if (introspection == null)
                {
                    _logger.LogError(CommonFields.CannotRetrieveIntrospection);
                    return false;
                }

                // Get the timespan until the token expires
                DateTimeOffset expirationTime = DateTimeOffset.FromUnixTimeSeconds(introspection.exp);
                TimeSpan timeRemaining = expirationTime - DateTimeOffset.UtcNow;

                // If the token is valid for more than 20 seconds, we consider it active
                // This is because we need to use the token at least once within 15 minutes to keep it active
                // 20 seconds will ensure we have enough time to make a call before it expires
                return timeRemaining.Seconds > 20;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{CommonFields.CannotRetrieveIntrospection}{Environment.NewLine}Exception: {ex}");
                return false;
            }
        }
    }

    public interface IEncompassAuthService
    {
        /// <summary>
        /// The access token is active for 30 minutes, up to a maximum of 24 hours. 
        /// To avoid expiration, the access token must be used (by calling an API) at least once within 15 minutes. 
        /// If you make a call after the 15 minutes but before the 30 minutes, the token will expire after 30 minutes.
        /// todo where do i get info from url? instance id? client secret?  //from shared secret service class
        /// </summary>
        /// <param name="orgId"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        Task<ProcessResult<string>> CheckAndGetAccessToken(string instanceId);
    }
}