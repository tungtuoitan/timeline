namespace SuperAppModels.DTOs.Responses;

/// <summary>
/// Unified K Workspace DTO
/// Contains workspace metadata + flat list of self-contained nodes (kws.workspace_items)
/// </summary>
public class KWorkspaceDTO
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Flat list of nodes. Frontend builds hierarchy using parentId.
    /// </summary>
    public List<KWorkspaceItemResponseV2> FlatData { get; set; } = new();
}
