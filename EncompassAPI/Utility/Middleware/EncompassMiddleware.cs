using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Repos;
using EncompassAPI.Services;
using Microsoft.Extensions.Options;
using Shared.Models;

namespace EncompassAPI.Utility.Middleware
{
    public class EncompassMiddleware
    {
        private readonly RequestDelegate _next;

        public EncompassMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IOptionsSnapshot<EncompassConfig> encompassOptions, ITokenRepo tokenRepo, IEncompassAuthService encompassAuthService)
        {
            string xEToken;

            if (context.Request.Path.Value.ToLower().Contains("api/disclosures/retrieve"))
            {
                if (context.Items["x-e-token"] == null)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync(CommonStrings.EncompassTokenRequired);
                    return;
                }

                xEToken = context.Items["x-e-token"].ToString();
            }
            
            if (context.Request.Path.Value.ToLower().Contains(encompassOptions.Value.BaseUrl))
            {
                xEToken = context.Items["x-e-token"].ToString();

                // Set the token in the header of request
                context.Request.Headers["Authorization"] = $"Bearer {xEToken}";
            }
            
            // Call the next middleware
            await _next(context);
        }
    }
}
