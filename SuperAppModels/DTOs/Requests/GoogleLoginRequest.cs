using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request model for Google OAuth login
    /// </summary>
    public class GoogleLoginRequest
    {
        /// <summary>
        /// User's Google email address
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's first name from Google profile
        /// </summary>
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string? FirstName { get; set; }

        /// <summary>
        /// User's last name from Google profile
        /// </summary>
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string? LastName { get; set; }

        /// <summary>
        /// Google profile picture URL
        /// </summary>
        [StringLength(500, ErrorMessage = "Picture URL cannot exceed 500 characters")]
        [Url(ErrorMessage = "Invalid picture URL format")]
        public string? Picture { get; set; }
    }
}
