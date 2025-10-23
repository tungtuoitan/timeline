namespace SuperAppModels.DTOs.Responses;

/// <summary>
/// Note-specific metadata for WorkspaceTreeItemResponse
/// Contains additional information specific to note items
/// Notes are always leaf nodes and cannot have children
/// </summary>
public class NoteMetadata
{
    /// <summary>
    /// Optional description/summary of the note
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Preview of the note content (first 200 characters)
    /// </summary>
    public string? ContentPreview { get; set; }

    /// <summary>
    /// Content type of the note (markdown/plain/rich-text)
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Whether the note is archived
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Whether the note is pinned for quick access
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// Whether the note is marked as favorite
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// Number of versions/revisions of this note
    /// </summary>
    public int VersionCount { get; set; }

    /// <summary>
    /// Number of members who have access to this note
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// Whether the note is publicly accessible
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Public URL slug if the note is shared publicly
    /// </summary>
    public string? PublicSlug { get; set; }

    /// <summary>
    /// When the note was created
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// When the note was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
