using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request model for user authentication (supports email or phone login)
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// User email address (required if phone is not provided)
        /// </summary>
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? Email { get; set; }

        /// <summary>
        /// User phone number (required if email is not provided)
        /// </summary>
        [Phone(ErrorMessage = "Invalid phone format")]
        public string? Phone { get; set; }

        /// <summary>
        /// User password
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Password cannot be empty")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Validates that either email or phone is provided
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(Email) || !string.IsNullOrWhiteSpace(Phone);
    }
}
