using EncompassAPI.Models;
using EncompassAPI.Services;

namespace EncompassAPI.Middleware
{
    public class AuthKeyMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuthService authService, ILogger<AuthKeyMiddleware> logger)
        {
            // Extract the token from header
            string? authKey = context.Request.Headers[CommonStrings.IncomingAuthKeyName].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(authKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "text/plain";
                logger.LogError(CommonStrings.AuthKeyRequired);
                await context.Response.WriteAsync(CommonStrings.AuthKeyRequired);
                return;
            }

            bool blAuthenticed = await authService.Authenticate(authKey);

            if (!blAuthenticed)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "text/plain";
                logger.LogError(CommonStrings.AuthKeyInvalid);
                await context.Response.WriteAsync(CommonStrings.AuthKeyInvalid);
                return;
            }

            // Call the next middleware
            await _next(context);
        }
    }
}
