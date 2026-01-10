namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// Response DTO for hashtag details
    /// </summary>
    public class HashtagResponse
    {
        /// <summary>
        /// Tag ID
        /// </summary>
        public int TagId { get; set; }

        /// <summary>
        /// User ID (owner)
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Tag name (without # prefix)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Display name (with # prefix, e.g., "#urgent")
        /// </summary>
        public string DisplayName => "#" + Name;

        /// <summary>
        /// URL-friendly slug
        /// </summary>
        //public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Tag color (hex format)
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Number of entities using this tag
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// Created timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
