namespace SuperAppModels.Models
{
    /// <summary>
    /// Knowledge base entity — k.knowledge table
    /// </summary>
    public class KKnowledge : ITimestampEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? StatusCode { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public User User { get; set; } = null!;
        public ICollection<KNodeEntity> Nodes { get; set; } = new List<KNodeEntity>();

        public KKnowledge()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public KKnowledge(string name, int userId) : this()
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            UserId = userId;
        }

        public void Update(string name, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));
            Name = name;
            Description = description;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SoftDelete()
        {
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
