namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// Response for workspace item operations (add/update/delete operations)
    /// </summary>
    public class KWorkspaceItemOperationResponse
    {
        public long ItemId { get; set; }
        public int WorkspaceId { get; set; }
        public int ParentTagId { get; set; } 
        public string ChildType { get; set; } = string.Empty;
        public bool IsOriginal { get; set; }
        public string? RelationshipType { get; set; }
        public string? Label { get; set; }
        public string? Notes { get; set; }
        public string? ItemPath { get; set; }
        public int Depth { get; set; }
        public int SortOrder { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int AddedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public string? ParentTagName { get; set; }
        public string? ChildName { get; set; }
        public string? AddedByUserName { get; set; }
    }
}
