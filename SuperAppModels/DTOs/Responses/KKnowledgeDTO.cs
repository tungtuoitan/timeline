using SuperAppModels.DTOs.Responses;

namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// Knowledge base DTO — workspace metadata + flat node list
    /// </summary>
    public class KKnowledgeDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? StatusCode { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        /// <summary>Flat list of nodes — frontend builds hierarchy via parentId</summary>
        public List<KNodeResponse> FlatData { get; set; } = new();
    }
}
