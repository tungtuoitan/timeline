namespace SuperAppModels.DTOs.Responses
{
    public class TagDto
    {
        public int TagId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public int? Depth { get; set; }
        
        // Note: CreatedBy and UpdatedAt removed for security and simplicity - not needed in responses
    }
}