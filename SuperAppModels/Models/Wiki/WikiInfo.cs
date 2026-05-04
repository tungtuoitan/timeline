namespace SuperAppModels.Models
{
    /// <summary>
    /// Wiki info entry — wiki.info table
    /// </summary>
    public class WikiInfo : ITimestampEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public User User { get; set; } = null!;
        public ICollection<WikiInfoKeyword> InfoKeywords { get; set; } = new List<WikiInfoKeyword>();

        public WikiInfo() { CreatedAt = DateTime.UtcNow; }

        public WikiInfo(string title, string content, int userId) : this()
        {
            Title   = title ?? string.Empty;
            Content = content ?? string.Empty;
            UserId  = userId;
        }

        public void Update(string title, string content)
        {
            Title     = title ?? string.Empty;
            Content   = content ?? string.Empty;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SoftDelete()
        {
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Restore()
        {
            DeletedAt = null;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
