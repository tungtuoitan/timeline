using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request model for updating user profile
    /// </summary>
    public class UpdateUserProfileRequest
    {
        /// <summary>
        /// User email (optional - defaults to authenticated user)
        /// </summary>
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? Email { get; set; }

        /// <summary>
        /// Application code
        /// </summary>
        [StringLength(50, ErrorMessage = "AppC cannot exceed 50 characters")]
        public string? AppC { get; set; }

        /// <summary>
        /// Parents configuration
        /// </summary>
        [StringLength(1000, ErrorMessage = "Parents configuration cannot exceed 1000 characters")]
        public string? Parents { get; set; }

        /// <summary>
        /// Priorities configuration
        /// </summary>
        [StringLength(1000, ErrorMessage = "Priorities configuration cannot exceed 1000 characters")]
        public string? Priorities { get; set; }

        /// <summary>
        /// Statuses configuration
        /// </summary>
        [StringLength(1000, ErrorMessage = "Statuses configuration cannot exceed 1000 characters")]
        public string? Statuses { get; set; }

        /// <summary>
        /// Types configuration
        /// </summary>
        [StringLength(1000, ErrorMessage = "Types configuration cannot exceed 1000 characters")]
        public string? Types { get; set; }

        /// <summary>
        /// Repeat types configuration
        /// </summary>
        [StringLength(1000, ErrorMessage = "RepeatTypes configuration cannot exceed 1000 characters")]
        public string? RepeatTypes { get; set; }

        /// <summary>
        /// Update status for today
        /// </summary>
        [StringLength(100, ErrorMessage = "IsUpdatedTodays cannot exceed 100 characters")]
        public string? IsUpdatedTodays { get; set; }
    }
}