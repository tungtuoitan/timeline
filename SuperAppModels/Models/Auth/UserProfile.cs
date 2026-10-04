namespace SuperAppModels.Models
{
    using System.ComponentModel.DataAnnotations.Schema;
    /// <summary>
    /// Domain model representing user profile - EXACTLY matches urm.user_profiles schema
    /// </summary>
    public class UserProfile
    {
        /// <summary>
        /// Profile ID (Primary Key)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// User ID (Foreign Key to urm.users, Unique)
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// First name
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// Last name
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Avatar URL
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Biography
        /// </summary>
        public string? Bio { get; set; }

        /// <summary>
        /// Date of birth
        /// </summary>
        public DateOnly? DateOfBirth { get; set; }

        /// <summary>
        /// Gender
        /// </summary>
        public string? Gender { get; set; }

        /// <summary>
        /// Country
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// City
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// IANA timezone id used to display instants and compute "today" for this user
        /// (column timezone, default Asia/Ho_Chi_Minh). See SuperAppModels.Time.UserClock.
        /// </summary>
        public string? Timezone { get; set; } = SuperAppModels.Time.TimeZones.DefaultId;

        /// <summary>
        /// Language (default: en)
        /// </summary>
        public string? Language { get; set; } = "en";

        /// <summary>
        /// User filter preferences (JSON string)
        /// Stores filter settings for different views (noteGrid, wsGrid, workspace)
        /// </summary>
        public string? Filters { get; set; }

        // ── K Repo Sync ──────────────────────────────────────────────────────────

        [Column("k_repo_url")]
        public string? KRepoUrl { get; set; }
        [Column("k_repo_branch")]
        public string? KRepoBranch { get; set; }
        [Column("k_repo_pat")]
        public string? KRepoPat { get; set; }
        [Column("k_repo_last_push_sha")]
        public string? KRepoLastPushSha { get; set; }
        [Column("k_repo_last_push_at")]
        public DateTime? KRepoLastPushAt { get; set; }
        [Column("k_repo_last_check_at")]
        public DateTime? KRepoLastCheckAt { get; set; }
        [Column("k_repo_last_remote_sha")]
        public string? KRepoLastRemoteSha { get; set; }
        [Column("k_repo_status_code")]
        public string? KRepoStatusCode { get; set; } = "idle";
        [Column("k_repo_content_hash")]
        public string? KRepoContentHash { get; set; }

        /// <summary>
        /// When the profile was created (UTC)
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// When the profile was last updated (UTC)
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Soft delete timestamp
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Navigation property to User
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// Default constructor
        /// </summary>
        public UserProfile()
        {
            CreatedAt = DateTime.UtcNow;
            Timezone = SuperAppModels.Time.TimeZones.DefaultId;
            Language = "en";
        }

        /// <summary>
        /// Constructor with user ID
        /// </summary>
        /// <param name="userId">User ID</param>
        public UserProfile(int userId) : this()
        {
            UserId = userId;
        }

        /// <summary>
        /// Updates the profile information
        /// </summary>
        public void Update(
            string? firstName = null,
            string? lastName = null,
            string? avatarUrl = null,
            string? bio = null,
            DateOnly? dateOfBirth = null,
            string? gender = null,
            string? country = null,
            string? city = null,
            string? timezone = null,
            string? language = null,
            string? filters = null)
        {
            if (firstName != null) FirstName = firstName;
            if (lastName != null) LastName = lastName;
            if (avatarUrl != null) AvatarUrl = avatarUrl;
            if (bio != null) Bio = bio;
            if (dateOfBirth != null) DateOfBirth = dateOfBirth;
            if (gender != null) Gender = gender;
            if (country != null) Country = country;
            if (city != null) City = city;
            if (timezone != null) Timezone = timezone;
            if (language != null) Language = language;
            if (filters != null) Filters = filters;

            UpdatedAt = DateTime.UtcNow;
        }
    }
}