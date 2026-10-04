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

        /// <summary>
        /// Redirect URI used when requesting the code. Optional: defaults to the web callback.
        /// Must be in the server allowlist (OAuth:Google:RedirectUri + OAuth:Google:CliRedirectUris).
        /// Used by the TungRoot CLI (scripts/sa/sa.py), which receives the code on localhost.
        /// </summary>
        [StringLength(500)]
        public string? RedirectUri { get; set; }
    }
}