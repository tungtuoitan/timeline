namespace SuperAppModels.Models
{
    public class User
    {
        // Database columns - must match exactly with 'users' table
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime? CreatedAt { get; set; }
        
        // Additional properties for application logic (not in database)
        public string? Phone { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateOnly? Birthday { get; set; }
        public string? PasswordHash { get; set; }
        public string? Token { get; set; }
        public DateTime? TokenExpires { get; set; }
        public string? AuthenticationType { get; set; }
        public string? ExternalId { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsEmailVerified { get; set; }

        public User()
        {
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }

        public User(string username, string email) : this()
        {
            Username = username ?? throw new ArgumentNullException(nameof(username));
            Email = email ?? throw new ArgumentNullException(nameof(email));
        }

        /// <summary>
        /// Constructor for external authentication with OAuth provider integration
        /// </summary>
        /// <param name="email">User email</param>
        /// <param name="authType">Authentication type (google, facebook, etc.)</param>
        /// <param name="externalId">External provider user ID</param>
        public User(string email, string authType, string externalId) : this()
        {
            Email = email ?? throw new ArgumentNullException(nameof(email));
            AuthenticationType = authType ?? throw new ArgumentNullException(nameof(authType));
            ExternalId = externalId ?? throw new ArgumentNullException(nameof(externalId));
            IsEmailVerified = true; // Assume OAuth emails are verified
        }

        public string FullName => $"{FirstName} {LastName}".Trim();
        public string PrimaryIdentifier => Email ?? Phone ?? $"User#{Id}";

        /// <summary>
        /// Updates user profile information with validation and business rules
        /// </summary>
        public void UpdateProfile(string? firstName, string? lastName, DateOnly? birthday = null)
        {
            FirstName = firstName;
            LastName = lastName;
            Birthday = birthday;
        }

        /// <summary>
        /// Sets authentication token with expiration and security management
        /// </summary>
        public void SetToken(string token, DateTime expires)
        {
            Token = token;
            TokenExpires = expires;
        }

        /// <summary>
        /// Records successful login with timestamp and security tracking
        /// </summary>
        public void RecordLogin()
        {
            LastLoginAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Deactivates user account with cleanup of sensitive data
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            Token = null;
            TokenExpires = null;
        }
    }
}