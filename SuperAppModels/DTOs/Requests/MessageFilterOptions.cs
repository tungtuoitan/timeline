namespace SuperAppModels.DTOs.Requests
{
    public class MessageFilterOptions
    {
        public int UserId { get; set; }
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public int? TopicId { get; set; }           // null = no filter, -1 = entity-level only (topic_id IS NULL)
        public bool EntityLevelOnly { get; set; }   // true = topic_id IS NULL + entity matches
        public string? DeletedAt { get; set; }      // "null" | "notNull" | null (all)
    }
}
