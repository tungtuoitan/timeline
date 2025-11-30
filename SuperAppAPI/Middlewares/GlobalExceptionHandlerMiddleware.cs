using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SuperAppAPI.Exceptions;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace SuperAppAPI.Middlewares
{
    /// <summary>
    /// Global exception handler middleware for centralized error handling
    /// </summary>
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            var response = context.Response;
            response.ContentType = "application/json";

            var problemDetails = new ProblemDetailsResponse();

            switch (exception)
            {
                case NotFoundException notFoundEx:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    problemDetails.Status = response.StatusCode;
                    problemDetails.Title = "Not Found";
                    problemDetails.Detail = notFoundEx.Message;
                    _logger.LogWarning(notFoundEx, "Resource not found: {Message}", notFoundEx.Message);
                    break;

                case BadRequestException badRequestEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    problemDetails.Status = response.StatusCode;
                    problemDetails.Title = "Bad Request";
                    problemDetails.Detail = badRequestEx.Message;
                    _logger.LogWarning(badRequestEx, "Bad request: {Message}", badRequestEx.Message);
                    break;

                case ValidationException validationEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    problemDetails.Status = response.StatusCode;
                    problemDetails.Title = "Validation Error";
                    problemDetails.Detail = validationEx.Message;
                    problemDetails.Errors = validationEx.Errors;
                    _logger.LogWarning(validationEx, "Validation error: {Message}", validationEx.Message);
                    break;

                case UnauthorizedException unauthorizedEx:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    problemDetails.Status = response.StatusCode;
                    problemDetails.Title = "Unauthorized";
                    problemDetails.Detail = unauthorizedEx.Message;
                    _logger.LogWarning(unauthorizedEx, "Unauthorized access: {Message}", unauthorizedEx.Message);
                    break;

                case ForbiddenException forbiddenEx:
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                    problemDetails.Status = response.StatusCode;
                    problemDetails.Title = "Forbidden";
                    problemDetails.Detail = forbiddenEx.Message;
                    _logger.LogWarning(forbiddenEx, "Forbidden access: {Message}", forbiddenEx.Message);
                    break;

                case ConflictException conflictEx:
                    response.StatusCode = (int)HttpStatusCode.Conflict;
                    problemDetails.Status = response.StatusCode;
                    problemDetails.Title = "Conflict";
                    problemDetails.Detail = conflictEx.Message;
                    _logger.LogWarning(conflictEx, "Conflict: {Message}", conflictEx.Message);
                    break;

                case BusinessRuleException businessRuleEx:
                    response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                    problemDetails.Status = response.StatusCode;
                    problemDetails.Title = "Business Rule Violation";
                    problemDetails.Detail = businessRuleEx.Message;
                    _logger.LogWarning(businessRuleEx, "Business rule violation: {Message}", businessRuleEx.Message);
                    break;

                case DataAccessException dataAccessEx:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    problemDetails.Status = response.StatusCode;
                    problemDetails.Title = "Data Access Error";
                    problemDetails.Detail = "An error occurred while accessing the database";
                    _logger.LogError(dataAccessEx, "Data access error: {Message}", dataAccessEx.Message);
                    break;

                case KeyNotFoundException keyNotFoundEx:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    problemDetails.Status = response.StatusCode;
                    problemDetails.Title = "Not Found";
                    problemDetails.Detail = keyNotFoundEx.Message;
                    _logger.LogWarning(keyNotFoundEx, "Resource not found: {Message}", keyNotFoundEx.Message);
                    break;

                case ArgumentException argumentEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    problemDetails.Status = response.StatusCode;
                    problemDetails.Title = "Bad Request";
                    problemDetails.Detail = argumentEx.Message;
                    _logger.LogWarning(argumentEx, "Invalid argument: {Message}", argumentEx.Message);
                    break;

                case UnauthorizedAccessException unauthorizedAccessEx:
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                    problemDetails.Status = response.StatusCode;
                    problemDetails.Title = "Forbidden";
                    problemDetails.Detail = unauthorizedAccessEx.Message;
                    _logger.LogWarning(unauthorizedAccessEx, "Unauthorized access: {Message}", unauthorizedAccessEx.Message);
                    break;

                case InvalidOperationException invalidOpEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    problemDetails.Status = response.StatusCode;
                    problemDetails.Title = "Invalid Operation";
                    problemDetails.Detail = invalidOpEx.Message;
                    _logger.LogWarning(invalidOpEx, "Invalid operation: {Message}", invalidOpEx.Message);
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    problemDetails.Status = response.StatusCode;
                    problemDetails.Title = "Internal Server Error";
                    problemDetails.Detail = "An unexpected error occurred";
                    _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
                    break;
            }

            problemDetails.Type = $"https://httpstatuses.com/{response.StatusCode}";
            problemDetails.Instance = context.Request.Path;

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var result = JsonSerializer.Serialize(problemDetails, jsonOptions);
            await response.WriteAsync(result);
        }
    }

    /// <summary>
    /// ProblemDetails response model based on RFC 7807
    /// </summary>
    public class ProblemDetailsResponse
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Status { get; set; }
        public string Detail { get; set; } = string.Empty;
        public string Instance { get; set; } = string.Empty;
        public IDictionary<string, string[]>? Errors { get; set; }
    }

    /// <summary>
    /// Extension methods for GlobalExceptionHandlerMiddleware
    /// </summary>
    public static class GlobalExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        }
    }
}
