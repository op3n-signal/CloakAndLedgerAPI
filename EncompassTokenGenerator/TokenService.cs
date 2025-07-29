using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
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
            //https://api.elliemae.com/oauth2/v1/token
            // Implement the logic to get the access token from the Encompass API
            // This typically involves making an HTTP request to the token endpoint
            // and handling the response to extract the token.
            throw new NotImplementedException();

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.elliemae.com/oauth2/v1/token");
            var byteArray = Encoding.ASCII.GetBytes("username:password");
            request.Headers.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            request.Headers.Add("Authorization", "••••••");
            var collection = new List<KeyValuePair<string, string>>();
            collection.Add(new("instance_id", "BE11350238"));
            collection.Add(new("scope", "lp"));
            collection.Add(new("grant_type", "client_credentials"));
            var content = new FormUrlEncodedContent(collection);
            request.Content = content;
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            Console.WriteLine(await response.Content.ReadAsStringAsync());

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
        /// todo where do i get info from url? instance id? client secret?  //from shared secret service class
        /// </summary>
        /// <returns></returns>
        Task GetAccessToken();

        Task CheckAccessTokenStatus();
    }
}