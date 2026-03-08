namespace SuperAppModels.DTOs.Responses;

/// <summary>
/// Folder entity data - full data from folders table
/// Used in WorkspaceItemResponseV2 as the Data property for folders
/// </summary>
public class FolderData
{
    /// <summary>Folder ID (folders.id)</summary>
    public int Id { get; set; }

    /// <summary>User ID who owns the folder (folders.user_id)</summary>
    public int UserId { get; set; }

    /// <summary>Folder name (folders.name)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Folder description (folders.description)</summary>
    public string? Description { get; set; }

    /// <summary>Hex color code (folders.color)</summary>
    public string? Color { get; set; }

    /// <summary>Icon emoji or class (folders.icon)</summary>
    public string? Icon { get; set; }

    /// <summary>Created timestamp (folders.created_at)</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Updated timestamp (folders.updated_at)</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Soft delete timestamp (folders.deleted_at)</summary>
    public DateTime? DeletedAt { get; set; }

}
