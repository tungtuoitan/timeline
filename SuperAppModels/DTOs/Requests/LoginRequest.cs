using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    public class LoginRequest
    {
        public string? Email { get; set; }

        public string? Phone { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }
}
