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
            if (context.Request.Path.Value.ToLower().Contains(encompassOptions.Value.BaseUrl))
            {
                string instanceId = context.Items["instanceid"].ToString();

                // Get the encompass access token
                ProcessResult<string> prAccessToken = await encompassAuthService.CheckAndGetAccessToken(instanceId);

                if(!prAccessToken.Success)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync(prAccessToken.Message);
                    return;
                }

                // Set the token in the header of request
                context.Request.Headers["Authorization"] = $"Bearer {prAccessToken.Result}";
            }
            
            // Call the next middleware
            await _next(context);
        }
    }
}
