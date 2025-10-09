namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// Response model for user profile operations
    /// </summary>
    public class UserProfileResponse
    {
        /// <summary>
        /// User email
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Application code
        /// </summary>
        public string? AppC { get; set; }

        /// <summary>
        /// Parents configuration
        /// </summary>
        public string? Parents { get; set; }

        /// <summary>
        /// Priorities configuration
        /// </summary>
        public string? Priorities { get; set; }

        /// <summary>
        /// Statuses configuration
        /// </summary>
        public string? Statuses { get; set; }

        /// <summary>
        /// Types configuration
        /// </summary>
        public string? Types { get; set; }

        /// <summary>
        /// Repeat types configuration
        /// </summary>
        public string? RepeatTypes { get; set; }

        /// <summary>
        /// Update status for today
        /// </summary>
        public string? IsUpdatedTodays { get; set; }

        /// <summary>
        /// When the profile was last updated
        /// </summary>
        public DateTime? LastUpdated { get; set; }
    }
}