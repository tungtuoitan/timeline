namespace SuperAppModels.DTOs.Responses
{
    public class NoteDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Type { get; set; }
        public string? StatusCode { get; set; } // Status code from standard_registries
        public string? Icon { get; set; } // Icon type for visual display
        public string? Color { get; set; } // Hex color code for icon
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; } // ✅ Track if note is deleted

        /// <summary>
        /// List of workspaces that link to this note via workspace_items
        /// Used to show which workspaces reference this note
        /// </summary>
        public List<WorkspaceLinkDTO> WorkspaceLinks { get; set; } = new List<WorkspaceLinkDTO>();

        // Note: Tags/Hashtags are handled separately via standard_registries, not included in NoteDTO
        // Note: CreatedBy removed for security - sensitive data should not be exposed in API responses
    }
}
