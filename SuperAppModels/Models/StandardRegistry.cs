namespace SuperAppModels.Models
{
    /// <summary>
    /// Domain model representing a standard registry configuration entry
    /// </summary>
    public class StandardRegistry
    {
        /// <summary>
        /// Registry entry unique identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Registry type code (unique)
        /// </summary>
        public string TypeCode { get; set; } = string.Empty;

        /// <summary>
        /// Registry description/value
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Whether the registry entry is active (BIT)
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// When the registry entry was created (UTC)
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// When the registry entry was last updated (UTC)
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public StandardRegistry()
        {
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }

        /// <summary>
        /// Constructor with required fields
        /// </summary>
        /// <param name="typeCode">Registry type code</param>
        /// <param name="description">Registry description</param>
        public StandardRegistry(string typeCode, string? description = null) : this()
        {
            TypeCode = typeCode ?? throw new ArgumentNullException(nameof(typeCode));
            Description = description;
        }

        /// <summary>
        /// Updates the registry entry
        /// </summary>
        public void Update(string? description = null, bool? isActive = null)
        {
            if (description != null) Description = description;
            if (isActive.HasValue) IsActive = isActive.Value;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Activates the registry entry
        /// </summary>
        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Deactivates the registry entry
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}