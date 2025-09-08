using EncompassAPI.Services;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Shared.Models;

namespace EncompassAPI.Utility.Middleware
{
    public class EncompassTokenHandler : DelegatingHandler
    {

        public EncompassTokenHandler()
        {

        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {


            return await base.SendAsync(request, cancellationToken);
        }
    }

}
