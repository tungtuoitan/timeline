namespace SuperAppModels.DTOs.Responses;

/// <summary>
/// Tag-specific metadata for WorkspaceTreeItemResponse
/// Contains additional information specific to tag items
/// </summary>
public class TagMetadata
{
    /// <summary>
    /// Hierarchical path showing full tag ancestry (e.g., "/1/5/12/")
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Number of times this tag has been used across the workspace
    /// Includes usage in notes, files, and relationships
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Total number of direct children (tags + notes + files)
    /// </summary>
    public int ChildrenCount { get; set; }

    /// <summary>
    /// Number of child tags only
    /// </summary>
    public int TagChildrenCount { get; set; }

    /// <summary>
    /// Number of child notes only
    /// </summary>
    public int NoteChildrenCount { get; set; }

    /// <summary>
    /// Number of child files only
    /// </summary>
    public int FileChildrenCount { get; set; }

    /// <summary>
    /// Optional description of the tag
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the tag is publicly visible
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Public URL slug if the tag is shared publicly
    /// </summary>
    public string? PublicSlug { get; set; }

    /// <summary>
    /// Tag color (hex format)
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Tag icon
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// When the tag was created
    /// </summary>
    public DateTime? CreatedAt { get; set; }
}
