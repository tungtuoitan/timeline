namespace SuperAppModels.DTOs.Responses;

/// <summary>
/// Response DTO for workspace with its complete tag tree hierarchy
/// Represents workspace as root node with all tag trees as children
/// </summary>
public class WorkspaceWithTagTreeResponse
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
    /// Maximum allowed depth for tag hierarchy in this workspace
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
    /// Hierarchical tag tree structure for this workspace
    /// Contains root-level tags with their children recursively
    /// </summary>
    public List<TagTreeResponse> Tags { get; set; } = new List<TagTreeResponse>();
}
