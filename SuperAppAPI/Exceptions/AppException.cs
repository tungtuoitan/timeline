namespace SuperAppAPI.Exceptions
{
    public abstract class AppException : Exception
    {
        public int StatusCode { get; }

        protected AppException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }

        protected AppException(string message, int statusCode, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }

    public class NotFoundException : AppException
    {
        public NotFoundException(string message) : base(message, 404) { }
    }

    public class BadRequestException : AppException
    {
        public BadRequestException(string message) : base(message, 400) { }
    }

    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message = "Unauthorized") : base(message, 401) { }
    }

    public class ForbiddenException : AppException
    {
        public ForbiddenException(string message = "Forbidden") : base(message, 403) { }
    }

    public class ValidationException : AppException
    {
        public IDictionary<string, string[]> Errors { get; }

        public ValidationException(IDictionary<string, string[]> errors)
            : base("One or more validation errors occurred.", 400)
        {
            Errors = errors;
        }

        public ValidationException(string field, string error)
            : base("Validation error occurred.", 400)
        {
            Errors = new Dictionary<string, string[]>
            {
                { field, new[] { error } }
            };
        }
    }
}
