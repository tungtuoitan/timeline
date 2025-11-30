namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// Response DTO for folder details (matches ws.folders schema)
    /// </summary>
    public class FolderResponse
    {
        /// <summary>
        /// Folder ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// User ID (owner)
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Folder name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Folder description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Folder color (hex format, default: #F59E0B)
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Folder icon (default: 📁)
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Created timestamp
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
