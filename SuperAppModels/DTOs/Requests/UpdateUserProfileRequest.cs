using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request model for updating user profile configuration and settings
    /// </summary>
    public class UpdateUserProfileRequest
    {
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? Email { get; set; }

        [StringLength(50, ErrorMessage = "AppC cannot exceed 50 characters")]
        public string? AppC { get; set; }

        [StringLength(1000, ErrorMessage = "Parents configuration cannot exceed 1000 characters")]
        public string? Parents { get; set; }

        [StringLength(1000, ErrorMessage = "Priorities configuration cannot exceed 1000 characters")]
        public string? Priorities { get; set; }

        [StringLength(1000, ErrorMessage = "Statuses configuration cannot exceed 1000 characters")]
        public string? Statuses { get; set; }

        [StringLength(1000, ErrorMessage = "Types configuration cannot exceed 1000 characters")]
        public string? Types { get; set; }

        [StringLength(1000, ErrorMessage = "RepeatTypes configuration cannot exceed 1000 characters")]
        public string? RepeatTypes { get; set; }

        [StringLength(100, ErrorMessage = "IsUpdatedTodays cannot exceed 100 characters")]
        public string? IsUpdatedTodays { get; set; }

        public string? UserProfileJson { get; set; }
    }
}