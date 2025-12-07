namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// Workspace DTO for API responses
    /// </summary>
    public class WorkspaceDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; } // ✅ Track if workspace is deleted
        
        // Note: UserId removed for security - sensitive data should not be exposed in API responses
    }
}
