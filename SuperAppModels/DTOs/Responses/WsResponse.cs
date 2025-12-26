namespace SuperAppModels.DTOs.Responses;

/// <summary>
/// Response DTO for workspace list item
/// Used for displaying workspace selection dropdown
/// </summary>
public class WsResponse
{
    /// <summary>
    /// Unique identifier for the workspace
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User ID who owns the workspace
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Name of the workspace
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the workspace
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When the workspace was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the workspace was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// When the workspace was deleted (soft delete) - null if not deleted
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}
