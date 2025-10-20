namespace SuperAppModels.Models
{
    public class User : ITimestampEntity
    {
        // Database columns - must match exactly with 'users' table schema
        public int UserId { get; set; } // PRIMARY KEY: user_id
        public string Email { get; set; } = string.Empty; // UNIQUE NOT NULL
        public string Username { get; set; } = string.Empty; // UNIQUE NOT NULL
        public string PasswordHash { get; set; } = string.Empty; // NOT NULL
        
        // Profile fields
        public string? DisplayName { get; set; } // display_name
        public string? AvatarUrl { get; set; } // avatar_url
        public string? Bio { get; set; } // bio
        public string? Preferences { get; set; } // preferences (JSON)
        
        // Status flags
        public bool IsActive { get; set; } = true; // is_active
        public bool EmailVerified { get; set; } // email_verified
        
        // Timestamps (ITimestampEntity)
        public DateTime? CreatedAt { get; set; } // created_at
        public DateTime? UpdatedAt { get; set; } // updated_at
        public DateTime? LastLoginAt { get; set; } // last_login_at
        public DateTime? DeletedAt { get; set; } // deleted_at (soft delete)
        
        // Navigation properties for EF Core
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public ICollection<Workspace> Workspaces { get; set; } = new List<Workspace>();
        public ICollection<Note> Notes { get; set; } = new List<Note>();

        public User()
        {
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
            EmailVerified = false;
        }

        public User(string username, string email, string passwordHash) : this()
        {
            Username = username ?? throw new ArgumentNullException(nameof(username));
            Email = email ?? throw new ArgumentNullException(nameof(email));
            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        }

        /// <summary>
        /// Updates user profile information
        /// </summary>
        public void UpdateProfile(string? displayName, string? bio, string? avatarUrl = null)
        {
            DisplayName = displayName;
            Bio = bio;
            AvatarUrl = avatarUrl;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Records successful login
        /// </summary>
        public void RecordLogin()
        {
            LastLoginAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Deactivates user account (soft delete)
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            DeletedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Verifies user email
        /// </summary>
        public void VerifyEmail()
        {
            EmailVerified = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    // Interface for entities with timestamps (used by EF Core)
    public interface ITimestampEntity
    {
        DateTime? CreatedAt { get; set; }
        DateTime? UpdatedAt { get; set; }
    }
}