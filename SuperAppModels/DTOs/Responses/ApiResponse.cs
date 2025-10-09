namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// Generic API response wrapper for consistent response format
    /// </summary>
    /// <typeparam name="T">The type of data being returned</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Indicates if the request was successful
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Response message
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// The actual response data
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Array of error messages if request failed
        /// </summary>
        public string[]? Errors { get; set; }

        /// <summary>
        /// Response timestamp (UTC)
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Request correlation ID for tracing
        /// </summary>
        public string? TraceId { get; set; }

        /// <summary>
        /// Creates a successful response
        /// </summary>
        public static ApiResponse<T> Ok(T data, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message ?? "Request successful"
            };
        }

        /// <summary>
        /// Creates an error response
        /// </summary>
        public static ApiResponse<T> Error(string message, string[]? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors
            };
        }

        /// <summary>
        /// Creates an error response with a single error
        /// </summary>
        public static ApiResponse<T> Error(string message, string error)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = new[] { error }
            };
        }
    }

    /// <summary>
    /// Non-generic API response for operations that don't return data
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
        /// <summary>
        /// Creates a successful response without data
        /// </summary>
        public static ApiResponse Ok(string? message = null)
        {
            return new ApiResponse
            {
                Success = true,
                Message = message ?? "Request successful"
            };
        }

        /// <summary>
        /// Creates an error response without data
        /// </summary>
        public new static ApiResponse Error(string message, string[]? errors = null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = errors
            };
        }

        /// <summary>
        /// Creates an error response with a single error
        /// </summary>
        public new static ApiResponse Error(string message, string error)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = new[] { error }
            };
        }
    }
}