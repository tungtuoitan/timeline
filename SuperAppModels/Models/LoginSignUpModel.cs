namespace SuperAppModels.Models
{
    /// <summary>
    /// Model for login and signup response data
    /// </summary>
    public class LoginSignUpModel
    {
        /// <summary>
        /// User email address
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User first name
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// User last name
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Authentication token
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// Token expiration date
        /// </summary>
        public DateTime? Expire { get; set; }

        /// <summary>
        /// Authentication type (loginDefault, signupDefault, loginByGoogle)
        /// </summary>
        public string? Type { get; set; }
    }
}