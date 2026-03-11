namespace SuperAppModels.DTOs.Responses;

/// <summary>
/// Workspace with tree response V2 - with full entity data embedded
/// Contains workspace metadata and flat list of items with clear separation
/// </summary>
public class KWorkspaceWithTreeResponseV2
{
    /// <summary>Workspace ID</summary>
    public int WorkspaceId { get; set; }

    /// <summary>User ID who owns the workspace</summary>
    public int UserId { get; set; }

    /// <summary>Workspace name</summary>
    public string Name { get; set; } = string.Empty; 

    /// <summary>Workspace description</summary>
    public string? Description { get; set; }

    /// <summary>Hex color code</summary>
    public string? Color { get; set; }

    /// <summary>Icon name or class</summary>
    public string? Icon { get; set; }

    /// <summary>Workspace organization type</summary>
    public string? Type { get; set; }

    /// <summary>Maximum depth allowed</summary>
    public int? MaxDepth { get; set; }

    /// <summary>Whether this is the default workspace</summary>
    public bool IsDefault { get; set; }

    /// <summary>Whether publicly accessible</summary>
    public bool IsPublic { get; set; }

    /// <summary>Whether this is a template</summary>
    public bool IsTemplate { get; set; }

    /// <summary>Whether archived</summary>
    public bool IsArchived { get; set; }

    /// <summary>Total number of tags/folders</summary>
    public int TagCount { get; set; }

    /// <summary>Total number of notes</summary>
    public int NoteCount { get; set; }

    /// <summary>Total number of files</summary>
    public int FileCount { get; set; }

    /// <summary>Number of members</summary>
    public int MemberCount { get; set; }

    /// <summary>Additional settings as JSON</summary>
    public string? Settings { get; set; }

    /// <summary>When created</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When last updated</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// FLAT list of workspace items with full entity data
    /// Frontend builds hierarchy using parentId
    /// Each item has:
    /// - Root level: workspace_items table properties
    /// - Data property: Full entity data (FolderData | NoteData | FileData)
    /// </summary>
    public List<WorkspaceItemResponseV2> Items { get; set; } = new();
}
