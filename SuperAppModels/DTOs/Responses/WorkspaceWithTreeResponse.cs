namespace SuperAppModels.DTOs.Responses;

/// <summary>
/// Response DTO for workspace with its complete tree hierarchy
/// Represents workspace as root node with all items (tags/notes/files) as children
/// Replaces WorkspaceWithTagTreeResponse to support mixed item types
/// </summary>
public class WorkspaceWithTreeResponse
{
    /// <summary>
    /// Unique identifier for the workspace
    /// </summary>
    public int WorkspaceId { get; set; }

    /// <summary>
    /// User ID who owns the workspace
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Name of the workspace
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the workspace
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Hex color code for workspace display (e.g., #FF5733)
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Icon name or class for workspace display
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Type of workspace organization (hierarchy/graph/network/timeline/custom)
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Maximum allowed depth for hierarchy in this workspace
    /// </summary>
    public int? MaxDepth { get; set; }

    /// <summary>
    /// Whether this is the default workspace for the user
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Whether the workspace is publicly accessible
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Whether the workspace is a template
    /// </summary>
    public bool IsTemplate { get; set; }

    /// <summary>
    /// Whether the workspace is archived
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Total number of tags in this workspace
    /// </summary>
    public int TagCount { get; set; }

    /// <summary>
    /// Total number of notes in this workspace
    /// </summary>
    public int NoteCount { get; set; }

    /// <summary>
    /// Total number of files in this workspace
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// Number of members with access to this workspace
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// JSON string containing additional workspace settings
    /// </summary>
    public string? Settings { get; set; }

    /// <summary>
    /// When the workspace was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the workspace was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Hierarchical tree structure for this workspace
    /// Contains root-level items (tags/notes/files) with their children recursively
    /// Notes and files are always leaf nodes (no children)
    /// Tags can have children of any type (tags/notes/files)
    /// </summary>
    public List<WorkspaceItemResponse> Items { get; set; } = new List<WorkspaceItemResponse>();
}
