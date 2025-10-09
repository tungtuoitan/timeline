using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace SuperAppAPI.Middlewares
{
    /// <summary>
    /// Middleware to add security headers to all responses
    /// Implements OWASP security header recommendations
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHostEnvironment _environment;

        public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
        {
            _next = next;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Remove server information header for security
            context.Response.Headers.Remove("Server");

            // Prevent clickjacking attacks
            context.Response.Headers["X-Frame-Options"] = "DENY";

            // Prevent MIME type sniffing
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";

            // Enable XSS protection
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

            // Referrer policy - control referrer information
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Content Security Policy - prevent XSS and injection attacks
            var csp = _environment.IsDevelopment()
                ? "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; connect-src 'self'; font-src 'self';"
                : "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; font-src 'self'; base-uri 'self'; form-action 'self';";
            
            context.Response.Headers["Content-Security-Policy"] = csp;

            // Permissions Policy - control browser features
            context.Response.Headers["Permissions-Policy"] = 
                "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";

            // Cache control for sensitive endpoints
            if (IsSensitiveEndpoint(context.Request.Path))
            {
                context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";
            }

            await _next(context);
        }

        private static bool IsSensitiveEndpoint(PathString path)
        {
            var pathValue = path.Value?.ToLowerInvariant();
            if (string.IsNullOrEmpty(pathValue))
                return false;

            var sensitiveEndpoints = new[]
            {
                "/api/authen",
                "/api/userprofile", 
                "/api/notes",
                "/api/standardregistry"
            };

            return sensitiveEndpoints.Any(endpoint => pathValue.StartsWith(endpoint));
        }
    }

    /// <summary>
    /// Extension method to register the security headers middleware
    /// </summary>
    public static class SecurityHeadersMiddlewareExtensions
    {
        /// <summary>
        /// Adds security headers middleware to the application pipeline
        /// Should be registered early in the pipeline, before other middleware
        /// </summary>
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}