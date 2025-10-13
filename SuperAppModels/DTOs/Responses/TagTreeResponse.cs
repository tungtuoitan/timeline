namespace SuperAppModels.DTOs.Responses
{
    public class TagTreeResponse
    {
        public int TagId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string? Path { get; set; }
        public string? Slug { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public string AccessType { get; set; } = string.Empty; // 'owner' or 'shared'
        public int Level { get; set; }
        public int UsageCount { get; set; }
        public int ChildrenCount { get; set; }
        
        // Navigation properties for tree structure
        public List<TagTreeResponse> Children { get; set; } = new List<TagTreeResponse>();
        public bool IsExpanded { get; set; } = false;
        public bool IsSelected { get; set; } = false;
    }
}