namespace SuperAppModels.Models
{
    /// <summary>
    /// Keyword for markdown editor
    /// TargetItemId stores:
    ///   - workspace → Workspace.Id
    ///   - folder/note/file → WorkspaceItem.Id
    ///   - project → Project.Id
    ///   - task → ProTask.Id
    ///   - log → LifeLogLog.Id
    ///   - track → LifeLogTrack.Id
    /// </summary>
    public class Keyword : ITimestampEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // workspace/folder/note/file/external/project/task/log/track

        public int? TargetItemId { get; set; }

        public string Link { get; set; } = string.Empty;
        public string? Description { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? HardDeletedAt { get; set; }

        public User User { get; set; } = null!;
        public WorkspaceItemEntity? TargetItem { get; set; }

        public Keyword()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public Keyword(
            string name,
            string link,
            string type,
            int userId,
            int? targetItemId = null)
            : this()
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Link = link ?? throw new ArgumentNullException(nameof(link));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            UserId = userId;
            TargetItemId = targetItemId;
        }

        public void Update(
            string name,
            string link,
            string type,
            string? description = null,
            int? targetItemId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));
            if (string.IsNullOrWhiteSpace(link))
                throw new ArgumentException("Link cannot be empty", nameof(link));
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentException("Type cannot be empty", nameof(type));

            Name = name;
            Link = link;
            Type = type;
            Description = description;
            TargetItemId = targetItemId;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
