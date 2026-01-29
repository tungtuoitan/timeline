using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    public class GoogleCodeRequest
    {
        [Required(ErrorMessage = "Authorization code is required")]
        [StringLength(2000, ErrorMessage = "Authorization code cannot exceed 2000 characters")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// PKCE code verifier - optional for backward compatibility
        /// Per RFC 7636: 43-128 characters, URL-safe
        /// </summary>
        [StringLength(128, MinimumLength = 43, ErrorMessage = "Code verifier must be between 43 and 128 characters")]
        public string? CodeVerifier { get; set; }
    }
}