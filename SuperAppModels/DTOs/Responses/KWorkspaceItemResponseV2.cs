namespace SuperAppModels.DTOs.Responses;

/// <summary>
/// Workspace item response V2 — flat node structure
/// Each item is a self-contained node (no separate entity tables)
/// </summary>
public class KWorkspaceItemResponseV2
{
    // From kws.workspace_items
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public int? ParentId { get; set; }

    // Node data
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }

    // Path
    public string PathIds { get; set; } = "/";
    public int PathDepth { get; set; }

    // Timestamps
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Computed
    public string AccessType { get; set; } = "owner";
    public bool IsOriginal { get; set; } = true;

    // UI state (frontend only)
    public bool IsExpanded { get; set; } = false;
    public bool IsSelected { get; set; } = false;
}
