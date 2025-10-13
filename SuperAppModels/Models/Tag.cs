namespace SuperAppModels.Models
{
    public class Tag
    {
        // Database columns - must match exactly
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string? Path { get; set; }
        public string? Slug { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public string? Description { get; set; }
        public bool? IsPublic { get; set; }
        public string? PublicSlug { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? CreatedBy { get; set; }

        // Hierarchy navigation property - not stored in database
        public int? Depth { get; set; }

        public Tag()
        {
            CreatedAt = DateTime.UtcNow;
            IsPublic = false;
        }

        public Tag(string name, int userId, int? createdBy = null) : this()
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            UserId = userId;
            CreatedBy = createdBy;
        }

        /// <summary>
        /// Updates the tag content with validation and automatic timestamp management
        /// </summary>
        /// <param name="name">New name</param>
        /// <param name="description">New description</param>
        /// <param name="color">New color</param>
        public void Update(string name, string? description = null, string? color = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));

            Name = name;
            Description = description;
            Color = color;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the tag as deleted with soft delete pattern
        /// </summary>
        public void SoftDelete()
        {
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Toggles the public visibility status
        /// </summary>
        public void TogglePublic()
        {
            IsPublic = !IsPublic;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}