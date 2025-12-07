namespace SuperAppModels.DTOs.Responses;

/// <summary>
/// Represents a single item (folder/note/file) in the workspace tree hierarchy
/// Polymorphic response that can represent different item types with type-specific metadata
/// </summary>
public class WorkspaceItemResponse
{
    /// <summary>
    /// Relationship ID (workspace_items.id) - used for workspace-specific operations
    /// </summary>
    public int? RelationshipId { get; set; }

    /// <summary>
    /// Type of the item: 'folder', 'note', or 'file'
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Entity ID (folder/note/file ID from respective tables)
    /// </summary>
    public long Id { get; set; }

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
    /// TRUE if this workspace created/owns the item, FALSE if shared from another workspace
    /// </summary>
    public bool IsOriginal { get; set; } = true;

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
    /// ⚠️ NOTE: Backend returns FLAT data - this array is always empty from API
    /// Frontend builds hierarchy using ParentId relationships
    /// This property exists for frontend use after buildHierarchy() is called
    /// </summary>
    public List<WorkspaceItemResponse> Children { get; set; } = new List<WorkspaceItemResponse>();

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

    /// <summary>
    /// When the item was deleted (soft delete) - null if not deleted
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}
