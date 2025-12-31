namespace SuperAppModels.DTOs.Responses;

/// <summary>
/// Note entity data - full data from notes table
/// Used in WorkspaceItemResponseV2 as the Data property for notes
/// </summary>
public class NoteData
{
    /// <summary>Note ID (notes.id)</summary>
    public int Id { get; set; }

    /// <summary>User ID who owns the note (notes.user_id)</summary>
    public int UserId { get; set; }

    /// <summary>Note name/title (notes.name)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Note description/content (notes.description)</summary>
    public string? Description { get; set; }

    /// <summary>Status code (notes.status_code)</summary>
    public string? StatusCode { get; set; }

    /// <summary>Created timestamp (notes.created_at)</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Updated timestamp (notes.updated_at)</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Soft delete timestamp (notes.deleted_at)</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>Copy metadata JSON (notes.copy_info)</summary>
    public string? CopyInfo { get; set; }

    /// <summary>List of workspaces that link to this note (populated from workspace_items)</summary>
    public List<WorkspaceLinkDTO>? WorkspaceLinks { get; set; }
}
