namespace SuperAppModels.DTOs.Responses;

/// <summary>
/// Represents a single item (tag/note/file) in the workspace tree hierarchy
/// Polymorphic response that can represent different item types with type-specific metadata
/// </summary>
public class WorkspaceTreeItemResponse
{
    /// <summary>
    /// Type of the item: 'tag', 'note', or 'file'
    /// </summary>
    public string ItemType { get; set; } = string.Empty;

    /// <summary>
    /// Workspace item ID (workspace_items.item_id) - used for deletion
    /// </summary>
    public long ItemId { get; set; }

    /// <summary>
    /// Workspace item ID (alias for ItemId, for backward compatibility)
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Child entity ID (TagId/NoteId/FileId depending on ItemType)
    /// </summary>
    public int ChildId { get; set; }

    /// <summary>
    /// ID of the user who owns/created this item
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Display name of the item
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// ID of the parent tag (null for root-level items)
    /// Note: Only tags can be parents; notes and files are always leaf nodes
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// URL-friendly slug for the item
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>
    /// Hex color code for display (e.g., #FF5733)
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Icon name or class for display
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Access type: 'owner' (created by user) or 'shared' (shared with user)
    /// </summary>
    public string AccessType { get; set; } = string.Empty;

    /// <summary>
    /// Depth level in the tree hierarchy (0 = root level)
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Position/order within the same parent (for custom sorting)
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Sort order (alias for Position, for backward compatibility)
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Depth in tree hierarchy (alias for Level, for backward compatibility)
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Type-specific metadata (TagMetadata/NoteMetadata/FileMetadata)
    /// Cast to appropriate type based on ItemType
    /// </summary>
    public object? Metadata { get; set; }

    /// <summary>
    /// Child items in the tree hierarchy
    /// Empty for notes and files (leaf nodes only)
    /// Can contain tags/notes/files for tag items
    /// </summary>
    public List<WorkspaceTreeItemResponse> Children { get; set; } = new List<WorkspaceTreeItemResponse>();

    /// <summary>
    /// UI state: Whether the item is currently expanded in the tree view
    /// </summary>
    public bool IsExpanded { get; set; } = false;

    /// <summary>
    /// UI state: Whether the item is currently selected
    /// </summary>
    public bool IsSelected { get; set; } = false;

    /// <summary>
    /// When the item was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the item was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
