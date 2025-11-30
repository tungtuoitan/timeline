namespace SuperAppModels.Models
{
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
        public DateTime? DateOfBirth { get; set; }

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
        /// Timezone (default: UTC)
        /// </summary>
        public string? Timezone { get; set; } = "UTC";

        /// <summary>
        /// Language (default: en)
        /// </summary>
        public string? Language { get; set; } = "en";

        /// <summary>
        /// When the profile was created (UTC)
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// When the profile was last updated (UTC)
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

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
            Timezone = "UTC";
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
            DateTime? dateOfBirth = null,
            string? gender = null,
            string? country = null,
            string? city = null,
            string? timezone = null,
            string? language = null)
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
            
            UpdatedAt = DateTime.UtcNow;
        }
    }
}