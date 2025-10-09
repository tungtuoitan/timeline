namespace SuperAppModels.Models
{
    /// <summary>
    /// Domain model representing user profile configuration
    /// </summary>
    public class UserProfile
    {
        /// <summary>
        /// User email (identifier)
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Application code
        /// </summary>
        public string? AppC { get; set; }

        /// <summary>
        /// Parents configuration (JSON or comma-separated values)
        /// </summary>
        public string? Parents { get; set; }

        /// <summary>
        /// Priorities configuration (JSON or comma-separated values)
        /// </summary>
        public string? Priorities { get; set; }

        /// <summary>
        /// Statuses configuration (JSON or comma-separated values)
        /// </summary>
        public string? Statuses { get; set; }

        /// <summary>
        /// Types configuration (JSON or comma-separated values)
        /// </summary>
        public string? Types { get; set; }

        /// <summary>
        /// Repeat types configuration (JSON or comma-separated values)
        /// </summary>
        public string? RepeatTypes { get; set; }

        /// <summary>
        /// Update status for today
        /// </summary>
        public string? IsUpdatedTodays { get; set; }

        /// <summary>
        /// When the profile was created (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the profile was last updated (UTC)
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Whether the profile is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Default constructor
        /// </summary>
        public UserProfile()
        {
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Constructor with email
        /// </summary>
        /// <param name="email">User email</param>
        public UserProfile(string email) : this()
        {
            Email = email ?? throw new ArgumentNullException(nameof(email));
        }

        /// <summary>
        /// Updates the profile configuration
        /// </summary>
        public void UpdateConfiguration(
            string? parents = null,
            string? priorities = null,
            string? statuses = null,
            string? types = null,
            string? repeatTypes = null,
            string? isUpdatedTodays = null)
        {
            if (parents != null) Parents = parents;
            if (priorities != null) Priorities = priorities;
            if (statuses != null) Statuses = statuses;
            if (types != null) Types = types;
            if (repeatTypes != null) RepeatTypes = repeatTypes;
            if (isUpdatedTodays != null) IsUpdatedTodays = isUpdatedTodays;
            
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if the profile has any configuration data
        /// </summary>
        public bool HasConfiguration => 
            !string.IsNullOrWhiteSpace(Parents) ||
            !string.IsNullOrWhiteSpace(Priorities) ||
            !string.IsNullOrWhiteSpace(Statuses) ||
            !string.IsNullOrWhiteSpace(Types) ||
            !string.IsNullOrWhiteSpace(RepeatTypes);
    }
}