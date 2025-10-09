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
        /// Registry key/code
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Registry description/value
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Registry type/category
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Whether the registry entry is active (1 = active, 0 = inactive)
        /// </summary>
        public int Active { get; set; } = 1;

        /// <summary>
        /// When the registry entry was created (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the registry entry was last updated (UTC)
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Who created this registry entry
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public StandardRegistry()
        {
            CreatedAt = DateTime.UtcNow;
            Active = 1;
        }

        /// <summary>
        /// Constructor with required fields
        /// </summary>
        /// <param name="code">Registry code</param>
        /// <param name="description">Registry description</param>
        /// <param name="type">Registry type</param>
        public StandardRegistry(string code, string description, string type) : this()
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        /// <summary>
        /// Indicates if the registry entry is active
        /// </summary>
        public bool IsActive => Active == 1;

        /// <summary>
        /// Updates the registry entry
        /// </summary>
        public void Update(string? description = null, string? type = null, int? active = null)
        {
            if (description != null) Description = description;
            if (type != null) Type = type;
            if (active.HasValue) Active = active.Value;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Activates the registry entry
        /// </summary>
        public void Activate()
        {
            Active = 1;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Deactivates the registry entry
        /// </summary>
        public void Deactivate()
        {
            Active = 0;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}