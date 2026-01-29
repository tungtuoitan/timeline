using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;

namespace SuperAppServices.Interfaces
{
    /// <summary>
    /// Authentication service interface
    /// Handles user authentication operations including OAuth and local login
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Authenticate user with Google OAuth authorization code
        /// </summary>
        /// <param name="authorizationCode">Authorization code from Google OAuth flow</param>
        /// <param name="codeVerifier">PKCE code verifier (optional for backward compatibility)</param>
        /// <returns>Authentication response with JWT token and user info</returns>
        Task<AuthResponse> GoogleLoginAsync(string authorizationCode, string? codeVerifier = null);

        /// <summary>
        /// Authenticate user with username and password (local login)
        /// </summary>
        /// <param name="username">Username or email</param>
        /// <param name="password">Password</param>
        /// <returns>Authentication response with JWT token and user info</returns>
        Task<AuthResponse> LocalLoginAsync(string username, string password);

        /// <summary>
        /// Generate JWT token for authenticated user
        /// </summary>
        /// <param name="user">User entity</param>
        /// <returns>JWT token string</returns>
        string GenerateJwtToken(User user);
    }
}
