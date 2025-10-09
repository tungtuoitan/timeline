namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// Response model for standard registry operations
    /// </summary>
    public class StandardRegistryResponse
    {
        /// <summary>
        /// Registry key identifier
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Registry value
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Registry description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// When the registry entry was created
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// When the registry entry was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Whether the registry entry is active
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}