using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace SuperAppAPI.Middlewares
{
    /// <summary>
    /// Middleware for validating Google OAuth tokens
    /// WARNING: This middleware has security vulnerabilities and should be replaced
    /// with proper JWT authentication or removed entirely
    /// </summary>
    [Obsolete("This middleware has security vulnerabilities. Use JwtValidationMiddleware or ASP.NET Core JWT authentication instead.")]
    public class GoogleTokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _config;
        private readonly ILogger<GoogleTokenValidationMiddleware> _logger;

        public GoogleTokenValidationMiddleware(
            RequestDelegate next, 
            IConfiguration config,
            ILogger<GoogleTokenValidationMiddleware> logger)
        {
            _next = next;
            _config = config;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {

            // SECURITY VULNERABILITY: Hardcoded endpoint bypass
            // This allows any URL containing "loginSignup" to bypass authentication
            // Should use proper route-based authentication attributes instead
            string path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
            
            // Define specific anonymous endpoints (more secure than substring matching)
            var anonymousEndpoints = new[]
            {
                "/api/authen/login",
                "/api/authen/signup", 
                "/api/authen/googlelogin",
                "/api/health"
            };
            
            if (anonymousEndpoints.Any(endpoint => path.StartsWith(endpoint)))
            {
                _logger.LogInformation("Skipping Google token validation for anonymous endpoint: {Path}", path);
                await _next(context);
                return;
            }
            // Check for the Authorization header
            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                _logger.LogWarning("Authorization header missing.");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Authorization header is required.");
                return;
            }

            // Extract the token from the Authorization header
            string authHeader = context.Request.Headers["Authorization"].ToString();
            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Invalid Authorization header format. Expected 'Bearer <token>'.");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid Authorization header format.");
                return;
            }

            string token = authHeader.Substring("Bearer ".Length).Trim();

            try
            {
                // Validate the Google ID token
                GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(token);

                // Optionally, verify audience (client ID) to ensure token is intended for your app
                string expectedClientId = _config["OAuth:ClientId"];
                if (!payload.Audience.Equals(expectedClientId))
                {
                    _logger.LogWarning("Invalid audience in token.");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid token audience.");
                    return;
                }

                // Optionally, check token expiration
                if (payload.ExpirationTimeSeconds.HasValue)
                {
                    var expiration = DateTimeOffset.FromUnixTimeSeconds(payload.ExpirationTimeSeconds.Value);
                    if (expiration < DateTimeOffset.UtcNow)
                    {
                        _logger.LogWarning("Token has expired.");
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Token has expired.");
                        return;
                    }
                }

                // Store payload in HttpContext for downstream use (e.g., user info)
                context.Items["GooglePayload"] = payload;

                _logger.LogInformation("Token validated successfully for user: {UserId}", payload.Subject);

                // Call the next middleware in the pipeline
                await _next(context);
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogError(ex, "Failed to validate Google token.");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid token.");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during token validation.");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("Internal server error.");
                return;
            }
        }
    }

    // Extension method to register the middleware
    public static class GoogleTokenValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseGoogleTokenValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GoogleTokenValidationMiddleware>();
        }
    }
}