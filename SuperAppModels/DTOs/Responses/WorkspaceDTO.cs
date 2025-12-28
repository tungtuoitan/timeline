namespace SuperAppModels.DTOs.Responses;

/// <summary>
/// Unified Workspace DTO
/// Contains workspace data from 'workspaces' table + flat list of workspace items
/// Replaces: WsResponse, WorkspaceWithTreeResponse, WorkspaceWithTreeResponseV2
/// </summary>
public class WorkspaceDTO
{
    // ============================================
    // WORKSPACE TABLE PROPERTIES
    // ============================================

    /// <summary>Workspace ID (workspaces.id)</summary>
    public int Id { get; set; }

    /// <summary>User ID who owns the workspace (workspaces.user_id)</summary>
    public int UserId { get; set; }

    /// <summary>Workspace name (workspaces.name)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Workspace description (workspaces.description)</summary>
    public string? Description { get; set; }

    /// <summary>Hex color code (workspaces.color)</summary>
    public string? Color { get; set; }

    /// <summary>Icon name or class (workspaces.icon)</summary>
    public string? Icon { get; set; }

    /// <summary>Workspace organization type (workspaces.type)</summary>
    public string? Type { get; set; }

    /// <summary>Maximum depth allowed (workspaces.max_depth)</summary>
    public int? MaxDepth { get; set; }

    /// <summary>Whether this is the default workspace (workspaces.is_default)</summary>
    public bool IsDefault { get; set; }

    /// <summary>Whether publicly accessible (workspaces.is_public)</summary>
    public bool IsPublic { get; set; }

    /// <summary>Whether this is a template (workspaces.is_template)</summary>
    public bool IsTemplate { get; set; }

    /// <summary>Whether archived (workspaces.is_archived)</summary>
    public bool IsArchived { get; set; }

    /// <summary>Total number of folders (computed)</summary>
    public int FolderCount { get; set; }

    /// <summary>Total number of notes (computed)</summary>
    public int NoteCount { get; set; }

    /// <summary>Total number of files (computed)</summary>
    public int FileCount { get; set; }

    /// <summary>Number of members (computed)</summary>
    public int MemberCount { get; set; }

    /// <summary>Additional settings as JSON (workspaces.settings)</summary>
    public string? Settings { get; set; }

    /// <summary>When created (workspaces.created_at)</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When last updated (workspaces.updated_at)</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>When deleted - soft delete (workspaces.deleted_at)</summary>
    public DateTime? DeletedAt { get; set; }

    // ============================================
    // WORKSPACE ITEMS DATA
    // ============================================

    /// <summary>
    /// FLAT list of workspace items with full entity data
    /// Frontend builds hierarchy using parentId
    /// Each item has:
    /// - Root level: workspace_items table properties (id, workspaceId, parentId, entityType, entityId, ...)
    /// - Data property: Full entity data (FolderData | NoteData | FileData)
    /// </summary>
    public List<WorkspaceItemResponseV2> FlatData { get; set; } = new();
}
