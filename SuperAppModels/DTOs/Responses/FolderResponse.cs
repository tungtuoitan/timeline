namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// Response DTO for folder details
    /// </summary>
    public class FolderResponse
    {
        /// <summary>
        /// Folder ID
        /// </summary>
        public int FolderId { get; set; }

        /// <summary>
        /// User ID (owner)
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Folder name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// URL-friendly slug
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Folder color (hex format)
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Folder icon
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Folder description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Number of workspaces using this folder
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// Created timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
