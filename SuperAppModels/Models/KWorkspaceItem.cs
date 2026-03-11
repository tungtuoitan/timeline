namespace SuperAppModels.Models;

/// <summary>
/// Database model for workspace tree items (internal use)
/// Maps to workspace tree query results
/// </summary>
public class KWorkspaceItem
{
    /// <summary>
    /// Relationship ID (workspace_items.id) - used for workspace operations
    /// </summary>
    public int? RelationshipId { get; set; }

    /// <summary>
    /// Type of the item: 'folder', 'note', or 'file'
    /// </summary>
    public string Type { get; set; } = string.Empty; 

    /// <summary>
    /// Entity ID (folder/note/file ID from respective tables)
    /// </summary>
    public long ItemId { get; set; }

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
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// URL-friendly slug for the item
    /// </summary>
    //public string? Slug { get; set; }

    /// <summary>
    /// Hex color code for display (e.g., #FF5733)
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Icon name or class for display
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Access type: 'owner' or 'shared'
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
    /// Position/order within the same parent
    /// </summary>
    public int Position { get; set; }

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

    /// <summary>
    /// Additional metadata (JSON or serialized object)
    /// </summary>
    public string? MetadataJson { get; set; }
}
