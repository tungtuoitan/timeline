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
        /// Registry code
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Registry description/value
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Registry type (category)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Whether the registry entry is active (BIT)
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// JSON detail data (flexible storage for additional properties)
        /// </summary>
        public string? Json_detail { get; set; }

        /// <summary>
        /// User who created this entry
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// When the registry entry was created
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        /// User who last modified this entry
        /// </summary>
        public string? LastModifiedBy { get; set; }

        /// <summary>
        /// When the registry entry was last modified
        /// </summary>
        public DateTime? LastModifiedDate { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public StandardRegistry()
        {
            IsActive = true;
        }

        /// <summary>
        /// Constructor with required fields
        /// </summary>
        /// <param name="code">Registry code</param>
        /// <param name="type">Registry type</param>
        /// <param name="description">Registry description</param>
        public StandardRegistry(string code, string type, string? description = null) : this()
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Description = description;
        }
    }
}