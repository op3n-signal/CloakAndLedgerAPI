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

        public async Task InvokeAsync(HttpContext context, IOptionsSnapshot<EncompassConfig> encompassOptions)
        {
            // This gets the token from the initial api call and stores it in the context for later use
            string xEToken;

            if (context.Request.Path.Value.ToLower().Contains("api/disclosures/retrieve") || 
                context.Request.Path.Value.ToLower().Contains("api/disclosureaudit") ||
                context.Request.Path.Value.ToLower().Contains("api/documentmapping/upsert"))
            {
                xEToken = context.Request.Headers[CommonFields.EncompassTokenHeaderName].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(xEToken))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync(CommonFields.EncompassTokenRequired);
                    return;
                }

                context.Items[CommonFields.EncompassTokenHeaderName] = xEToken;
            }
            
            if (context.Request.Path.Value.ToLower().Contains(encompassOptions.Value.BaseUrl))
            {
                xEToken = context.Request.Headers[CommonFields.EncompassTokenHeaderName].FirstOrDefault();

                context.Items[CommonFields.EncompassTokenHeaderName] = xEToken;

                // Set the token in the header of request
                context.Request.Headers["Authorization"] = $"Bearer {xEToken}";
            }

            // Call the next middleware
            await _next(context);
        }
    }
}
