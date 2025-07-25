using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EncompassTokenGenerator
{
    internal class TokenService : ITokenService
    {
        private readonly HttpClient _httpClient;

        public TokenService()
        {
            _httpClient = new HttpClient();
        }

        public async Task GetAccessToken()
        {
            // Implement the logic to get the access token from the Encompass API
            // This typically involves making an HTTP request to the token endpoint
            // and handling the response to extract the token.
            throw new NotImplementedException();
        }

        public async Task CheckAccessTokenStatus()
        {
            // Implement the logic to check the status of the access token.
            // This may involve making an HTTP request to a specific endpoint
            // that returns the token's validity or expiration status.
            throw new NotImplementedException();
        }
    }

    public interface ITokenService
    {
        /// <summary>
        /// The access token is active for 30 minutes, up to a maximum of 24 hours. 
        /// To avoid expiration, the access token must be used (by calling an API) at least once within 15 minutes. 
        /// If you make a call after the 15 minutes but before the 30 minutes, the token will expire after 30 minutes.
        /// todo where do i get info from url? instance id? client secret?
        /// </summary>
        /// <returns></returns>
        Task GetAccessToken();

        Task CheckAccessTokenStatus();
    }
}