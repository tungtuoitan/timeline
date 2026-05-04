namespace SuperAppModels.Models
{
    /// <summary>
    /// Wiki keyword entity — wiki.keyword table
    /// </summary>
    public class WikiKeyword : ITimestampEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconBase64 { get; set; }
        public int Views { get; set; } = 0;
        public int Reads { get; set; } = 0;
        public int Edits { get; set; } = 0;
        public double? PosX { get; set; }
        public double? PosY { get; set; }
        public bool PinnedPosition { get; set; } = false;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public User User { get; set; } = null!;
        public ICollection<WikiKeywordSynonym> Synonyms { get; set; } = new List<WikiKeywordSynonym>();
        public ICollection<WikiInfoKeyword> InfoKeywords { get; set; } = new List<WikiInfoKeyword>();

        public WikiKeyword() { CreatedAt = DateTime.UtcNow; }

        public WikiKeyword(string name, int userId) : this()
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            UserId = userId;
        }

        public void UpdateMeta(string? name, string? iconBase64)
        {
            if (!string.IsNullOrWhiteSpace(name))
                Name = name.Trim();
            IconBase64 = iconBase64;
            UpdatedAt  = DateTime.UtcNow;
        }

        public void IncrementView()  { Views++;  UpdatedAt = DateTime.UtcNow; }
        public void IncrementRead()  { Reads++;  UpdatedAt = DateTime.UtcNow; }
        public void IncrementEdit()  { Edits++;  UpdatedAt = DateTime.UtcNow; }

        public void SavePosition(double x, double y)
        {
            PosX = x;
            PosY = y;
            PinnedPosition = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SoftDelete()
        {
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
