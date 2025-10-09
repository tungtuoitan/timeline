namespace SuperAppModels.Models
{
    /// <summary>
    /// Domain model representing a User entity
    /// </summary>
    public class User
    {
        /// <summary>
        /// User unique identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// User email address (unique)
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// User phone number (unique, optional)
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
        /// User birth date
        /// </summary>
        public DateOnly? Birthday { get; set; }

        /// <summary>
        /// Hashed password (for local authentication)
        /// </summary>
        public string? PasswordHash { get; set; }

        /// <summary>
        /// Current authentication token (temporary)
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// Token expiration date
        /// </summary>
        public DateTime? TokenExpires { get; set; }

        /// <summary>
        /// Authentication type (local, google, facebook, etc.)
        /// </summary>
        public string? AuthenticationType { get; set; }

        /// <summary>
        /// External authentication provider user ID
        /// </summary>
        public string? ExternalId { get; set; }

        /// <summary>
        /// When the user account was created (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the user last logged in (UTC)
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Whether the user account is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether the user's email is verified
        /// </summary>
        public bool IsEmailVerified { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public User()
        {
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }

        /// <summary>
        /// Constructor for local authentication
        /// </summary>
        /// <param name="email">User email</param>
        /// <param name="passwordHash">Hashed password</param>
        public User(string email, string passwordHash) : this()
        {
            Email = email ?? throw new ArgumentNullException(nameof(email));
            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
            AuthenticationType = "local";
        }

        /// <summary>
        /// Constructor for external authentication (OAuth)
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

        /// <summary>
        /// User's full display name
        /// </summary>
        public string FullName => $"{FirstName} {LastName}".Trim();

        /// <summary>
        /// Primary identifier (email or phone)
        /// </summary>
        public string PrimaryIdentifier => Email ?? Phone ?? $"User#{Id}";

        /// <summary>
        /// Updates user profile information
        /// </summary>
        public void UpdateProfile(string? firstName, string? lastName, DateOnly? birthday = null)
        {
            FirstName = firstName;
            LastName = lastName;
            Birthday = birthday;
        }

        /// <summary>
        /// Sets a new authentication token
        /// </summary>
        public void SetToken(string token, DateTime expires)
        {
            Token = token;
            TokenExpires = expires;
        }

        /// <summary>
        /// Records a successful login
        /// </summary>
        public void RecordLogin()
        {
            LastLoginAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Deactivates the user account
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            Token = null;
            TokenExpires = null;
        }
    }
}