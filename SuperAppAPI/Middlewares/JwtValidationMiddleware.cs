using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SuperAppAPI.Middlewares
{
    /// <summary>
    /// Middleware for validating JWT tokens and extracting user claims
    /// This middleware complements ASP.NET Core's built-in JWT authentication
    /// </summary>
    public class JwtValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtValidationMiddleware> _logger;
        private readonly JwtSecurityTokenHandler _tokenHandler;

        public JwtValidationMiddleware(
            RequestDelegate next,
            IConfiguration configuration,
            ILogger<JwtValidationMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
            _tokenHandler = new JwtSecurityTokenHandler();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip validation for anonymous endpoints
            if (IsAnonymousEndpoint(context.Request.Path))
            {
                _logger.LogDebug("Skipping JWT validation for anonymous endpoint: {Path}", 
                    context.Request.Path);
                await _next(context);
                return;
            }

            // Extract token from Authorization header
            var token = ExtractTokenFromHeader(context.Request);
            
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Missing or invalid Authorization header for: {Path}", 
                    context.Request.Path);
                await WriteUnauthorizedResponse(context, "Missing or invalid authorization header");
                return;
            }

            try
            {
                // Validate the JWT token
                var principal = ValidateJwtToken(token);
                
                if (principal == null)
                {
                    _logger.LogWarning("Invalid JWT token provided for: {Path}", 
                        context.Request.Path);
                    await WriteUnauthorizedResponse(context, "Invalid token");
                    return;
                }

                // Add validated claims to context
                context.User = principal;
                
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogDebug("JWT validation successful for user: {UserId}", userId);

                await _next(context);
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("Expired JWT token provided for: {Path}", context.Request.Path);
                await WriteUnauthorizedResponse(context, "Token has expired");
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "Invalid JWT token provided for: {Path}", context.Request.Path);
                await WriteUnauthorizedResponse(context, "Invalid token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during JWT validation for: {Path}", 
                    context.Request.Path);
                await WriteInternalServerErrorResponse(context, "Authentication error");
            }
        }

        private bool IsAnonymousEndpoint(PathString path)
        {
            var pathValue = path.Value?.ToLowerInvariant();
            
            if (string.IsNullOrEmpty(pathValue))
                return false;

            // Define endpoints that don't require authentication
            var anonymousEndpoints = new[]
            {
                "/api/authen/login",
                "/api/authen/signup",
                "/api/authen/googlelogin",
                "/api/auth/login",
                "/api/auth/google/login",
                "/api/auth/refresh",
                "/api/auth/logout",
                "/api/health",
                "/swagger",
                "/favicon.ico"
            };

            return anonymousEndpoints.Any(endpoint => pathValue.StartsWith(endpoint));
        }

        private string? ExtractTokenFromHeader(HttpRequest request)
        {
            if (!request.Headers.ContainsKey("Authorization"))
                return null;

            var authHeader = request.Headers["Authorization"].ToString();
            
            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return null;

            return authHeader.Substring("Bearer ".Length).Trim();
        }

        private ClaimsPrincipal? ValidateJwtToken(string token)
        {
            try
            {
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
                
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero, // No tolerance for expired tokens
                    RequireExpirationTime = true
                };

                var principal = _tokenHandler.ValidateToken(token, validationParameters, 
                    out SecurityToken validatedToken);

                return principal;
            }
            catch
            {
                return null;
            }
        }

        private async Task WriteUnauthorizedResponse(HttpContext context, string message)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            
            var response = new
            {
                error = "Unauthorized",
                message = message,
                statusCode = 401,
                timestamp = DateTime.UtcNow,
                path = context.Request.Path.Value
            };

            await context.Response.WriteAsJsonAsync(response);
        }

        private async Task WriteInternalServerErrorResponse(HttpContext context, string message)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            
            var response = new
            {
                error = "Internal Server Error",
                message = message,
                statusCode = 500,
                timestamp = DateTime.UtcNow,
                path = context.Request.Path.Value
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }

    /// <summary>
    /// Extension methods for registering JWT validation middleware
    /// </summary>
    public static class JwtValidationMiddlewareExtensions
    {
        /// <summary>
        /// Adds JWT validation middleware to the application pipeline
        /// Should be registered after UseAuthentication() but before UseAuthorization()
        /// </summary>
        public static IApplicationBuilder UseJwtValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtValidationMiddleware>();
        }
    }
}