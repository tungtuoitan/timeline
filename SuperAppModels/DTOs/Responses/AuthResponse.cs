namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// Response model for authentication operations
    /// </summary>
    public class AuthResponse
    {
        /// <summary>
        /// Indicates if the authentication was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Authentication result message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// User data if authentication was successful
        /// </summary>
        public UserData? User { get; set; }

        /// <summary>
        /// Error details if authentication failed
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Token expiration time (UTC)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// User data included in authentication response
    /// </summary>
    public class UserData
    {
        /// <summary>
        /// User unique identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// User email address
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// User phone number
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// User first name
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// User last name
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Profile picture URL (from OAuth provider)
        /// </summary>
        public string? Picture { get; set; }

        /// <summary>
        /// Authentication type: 'local', 'google', 'facebook'
        /// </summary>
        public string AuthType { get; set; } = "local";

        /// <summary>
        /// JWT authentication token
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Token type (usually "Bearer")
        /// </summary>
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// User's full display name
        /// </summary>
        public string? FullName => $"{FirstName} {LastName}".Trim();
    }
}
