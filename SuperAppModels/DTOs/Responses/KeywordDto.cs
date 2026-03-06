namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// DTO for keyword response (includes all keywords from dbo.Keywords table)
    /// </summary>
    public class KeywordDto
    {
        /// <summary>
        /// Database ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Display name of the keyword
        /// Example: "Introduction", "Tổng quan/Học hành/Toán"
        /// </summary>
        public string Name { get; set; } = string.Empty;

        // /// <summary>
        // /// Name index for handling duplicate names
        // /// Default: 1, increments for each duplicate name
        // /// </summary>
        // public int NameIndex { get; set; } // REMOVED: no longer used

        /// <summary>
        /// Unique identifier link
        /// Format:
        /// - Workspace: "w-[workspaceId]"
        /// - Folder: "w-[workspaceId]/f-[workspaceItemId]"
        /// - Note: "w-[workspaceId]/f-[folderId]/n-[noteWorkspaceItemId]"
        ///         or "w-[workspaceId]/n-[noteWorkspaceItemId]"
        /// - File: "w-[workspaceId]/f-[folderId]/file-[fileWorkspaceItemId]"
        /// - Heading: "w-[workspaceId]/n-[noteId]/h1-Title1/h2-Title2"
        /// - External: "https://example.com/..."
        /// </summary>
        public string Link { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable long link with nameIndex
        /// Format:
        /// - Workspace: "WorkspaceName[1]"
        /// - Folder: "WorkspaceName[1]/FolderName[2]"
        /// - Note: "WorkspaceName[1]/FolderName[2]/NoteName[3]"
        /// - Heading: "WorkspaceName[1]/NoteName[2]/HeadingPath[3]"
        /// - External: "ExternalName[1]"
        /// </summary>
        public string LongLink { get; set; } = string.Empty;

        /// <summary>
        /// Type of keyword
        /// Values: external, workspace, folder, note, file, h1, h2, h3, h4, h5, h6
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Optional description
        /// </summary>
        public string? Description { get; set; }
        public DateTime? HardDeletedAt { get; set; }

        // ===== New fields for folder/note/file keywords =====

        /// <summary>
        /// Workspace item ID (workspace_items.id)
        /// Only populated for folder/note/file keywords
        /// </summary>
        public int? WorkspaceItemId { get; set; }

        /// <summary>
        /// Entity ID (folders.id / notes.id / files.id)
        /// Only populated for folder/note/file keywords
        /// </summary>
        public int? EntityId { get; set; }

        /// <summary>
        /// Color for folder/note/file
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Icon for folder/note/file
        /// </summary>
        public string? Icon { get; set; }
    }
}
