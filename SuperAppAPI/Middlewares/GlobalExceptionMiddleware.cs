using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Exceptions;
using System.Data.SqlClient;
using System.Text.Json;

namespace SuperAppAPI.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, message, details, errors) = exception switch
            {
                ValidationException validEx => (400, "Validation failed", (object?)null, validEx.Errors),
                AppException appEx => (appEx.StatusCode, appEx.Message, (object?)null, (IDictionary<string, string[]>?)null),
                SqlException sqlEx => (
                    500,
                    "Database error occurred",
                    _env.IsDevelopment() ? sqlEx.Message : (object?)null,
                    null
                ),
                UnauthorizedAccessException _ => (401, "Unauthorized access", null, null),
                _ => (
                    500,
                    "An internal server error occurred",
                    _env.IsDevelopment() ? exception.Message : (object?)null,
                    null
                )
            };

            // Log the exception with appropriate level
            if (statusCode >= 500)
            {
                _logger.LogError(exception, "Error occurred: {Message}. Path: {Path}",
                    exception.Message, context.Request.Path);
            }
            else if (statusCode >= 400)
            {
                _logger.LogWarning(exception, "Client error occurred: {Message}. Path: {Path}",
                    exception.Message, context.Request.Path);
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = message,
                Detail = details?.ToString(),
                Instance = context.Request.Path,
                Type = GetProblemType(statusCode)
            };

            // Add validation errors if present
            if (errors != null && errors.Any())
            {
                problemDetails.Extensions["errors"] = errors;
            }

            // Add stack trace in development
            if (_env.IsDevelopment() && exception.StackTrace != null)
            {
                problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            }

            // Add correlation ID if available
            if (context.TraceIdentifier != null)
            {
                problemDetails.Extensions["traceId"] = context.TraceIdentifier;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _env.IsDevelopment()
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(problemDetails, options));
        }

        private static string GetProblemType(int statusCode) => statusCode switch
        {
            400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
            403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            500 => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            _ => $"https://httpstatuses.com/{statusCode}"
        };
    }

    public static class GlobalExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}
