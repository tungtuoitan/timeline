using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    public class GoogleLoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? FirstName { get; set; }

        public string? LastName { get; set; }
    }
}
