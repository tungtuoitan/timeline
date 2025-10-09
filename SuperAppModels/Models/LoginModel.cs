namespace SuperAppModels.Models
{
    /// <summary>
    /// Model for user login credentials
    /// </summary>
    public class LoginModel
    {
        /// <summary>
        /// Username (email)
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// User password
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }
}