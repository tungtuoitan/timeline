using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    public class TopicFilterOptions
    {
        public int UserId { get; set; }
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public string? DeletedAt { get; set; } // "null" | "notNull" | null (all)
    }
}
