using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    public class LoginRequest
    {
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone format")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Password cannot be empty")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Validates that either email or phone is provided (complex business logic)
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(Email) || !string.IsNullOrWhiteSpace(Phone);
    }
}
