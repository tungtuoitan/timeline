using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    public class SignupRequest
    {
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone format")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
        public string Password { get; set; } = string.Empty;

        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        public DateOnly? Birthday { get; set; }
    }
}
