namespace SuperAppModels.Models;

/// <summary>
/// Database model for workspace tag tree (backward compatibility)
/// DEPRECATED: Use WorkspaceTreeItem for new development
/// </summary>
[Obsolete("Use WorkspaceTreeItem instead. This class will be removed in v2.0")]
public class WorkspaceTagTree
{
    /// <summary>
    /// Tag identifier
    /// </summary>
    public int TagId { get; set; }

    /// <summary>
    /// User identifier who owns the tag
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Tag name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parent tag ID (null for root tags)
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// URL-friendly slug
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>
    /// Hex color code
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Icon identifier
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Tree level (0 for root)
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Position in parent
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Access type: 'owner' or 'shared'
    /// </summary>
    public string AccessType { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
