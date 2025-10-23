namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// Response after updating a workspace item
    /// </summary>
    public class UpdateWorkspaceItemResponse
    {
        /// <summary>
        /// Workspace item ID
        /// </summary>
        public long ItemId { get; set; }

        /// <summary>
        /// Workspace ID
        /// </summary>
        public int WorkspaceId { get; set; }

        /// <summary>
        /// Parent tag ID (null for root level)
        /// </summary>
        public int? ParentTagId { get; set; }

        /// <summary>
        /// Child entity type (tag, note, file)
        /// </summary>
        public string ChildType { get; set; } = string.Empty;

        /// <summary>
        /// Child entity ID
        /// </summary>
        public int ChildId { get; set; }

        /// <summary>
        /// Custom label
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// Notes about this item
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Custom color (hex format)
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Custom icon
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Sort order
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Success message
        /// </summary>
        public string Message { get; set; } = "Workspace item updated successfully";
    }
}
