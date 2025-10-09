namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// Response model for user operations with computed display properties
    /// </summary>
    public class UserResponse
    {
        public int Id { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public DateOnly? Birthday { get; set; }

        public string? AuthenticationType { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public bool IsActive { get; set; }

        public bool IsEmailVerified { get; set; }

        /// <summary>
        /// User's full display name computed from first and last name
        /// </summary>
        public string FullName => $"{FirstName} {LastName}".Trim();

        /// <summary>
        /// Primary identifier for the user (email, phone, or fallback to user ID)
        /// </summary>
        public string PrimaryIdentifier => Email ?? PhoneNumber ?? $"User#{Id}";
    }
}