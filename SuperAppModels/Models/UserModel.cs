namespace SuperAppModels.Models
{
    /// <summary>
    /// Legacy user model for backward compatibility
    /// Consider migrating to the newer User domain model
    /// </summary>
    public class UserModel
    {
        /// <summary>
        /// User unique identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// User email address
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// User phone number
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// User first name
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// User last name
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// User birth date
        /// </summary>
        public DateOnly? Birthday { get; set; }

        /// <summary>
        /// User password (consider using PasswordHash in User model instead)
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Authentication token
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// Token expiration date
        /// </summary>
        public DateOnly? Expire { get; set; }

        /// <summary>
        /// Authentication type (loginDefault, signupDefault, loginByGoogle)
        /// </summary>
        public string? Type { get; set; }
    }
}