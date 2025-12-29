using System.Collections.Generic;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Filter options for workspace tree
    /// Similar to NoteFilterOptions pattern
    /// </summary>
    public class WorkspaceFilterOptions
    {
        /// <summary>
        /// User ID for filtering
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Status codes to filter by (comma-separated: "active,inactive,archived")
        /// Applies to Notes and Files only (Folders don't have status_code)
        /// </summary>
        public List<string>? StatusCodes { get; set; }

        /// <summary>
        /// Deleted status filter: "null" = existing only, "notNull" = deleted only, null = all
        /// Applies to all types (folders, notes, files)
        /// </summary>
        public string? DeletedAt { get; set; }
    }
}
