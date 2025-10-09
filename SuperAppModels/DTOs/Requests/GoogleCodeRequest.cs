using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request model for Google OAuth authorization code exchange
    /// </summary>
    public class GoogleCodeRequest
    {
        /// <summary>
        /// The authorization code received from Google OAuth flow
        /// </summary>
        [Required(ErrorMessage = "Authorization code is required")]
        [StringLength(2000, ErrorMessage = "Authorization code cannot exceed 2000 characters")]
        public string Code { get; set; } = string.Empty;
    }
}