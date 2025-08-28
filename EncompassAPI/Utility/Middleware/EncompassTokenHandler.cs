using EncompassAPI.Services;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Shared.Models;

namespace EncompassAPI.Utility.Middleware
{
    public class EncompassTokenHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EncompassTokenHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string xEToken = _httpContextAccessor.HttpContext.Items[CommonFields.EncompassTokenHeaderName].ToString();

            if (!string.IsNullOrWhiteSpace(xEToken))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", xEToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }

}
