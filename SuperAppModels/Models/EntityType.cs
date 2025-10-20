using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.Models
{
    /// <summary>
    /// Entity type registry for supported workspace entities
    /// Primary table: entity_types
    /// </summary>
    public class EntityType : ITimestampEntity
    {
        // Primary Key - type_name (NVARCHAR(50))
        [Key]
        public string TypeName { get; set; } = string.Empty;

        // Display info
        public string DisplayName { get; set; } = string.Empty;
        public string DisplayPlural { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public string? Description { get; set; }

        // Technical info
        public string? TableName { get; set; }
        public bool SupportsVersioning { get; set; } = false;
        public bool SupportsSharing { get; set; } = false;

        // Status
        public bool IsEnabled { get; set; } = true;

        // Timestamps (ITimestampEntity)
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public EntityType()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public EntityType(string typeName, string displayName, string displayPlural) : this()
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            DisplayPlural = displayPlural ?? throw new ArgumentNullException(nameof(displayPlural));
        }

        /// <summary>
        /// Updates entity type configuration
        /// </summary>
        public void Update(string displayName, string displayPlural, string? icon = null, string? color = null, string? description = null)
        {
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            DisplayPlural = displayPlural ?? throw new ArgumentNullException(nameof(displayPlural));
            if (icon != null) Icon = icon;
            if (color != null) Color = color;
            if (description != null) Description = description;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Enables or disables the entity type
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}