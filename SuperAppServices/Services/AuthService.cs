using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
{
    /// <summary>
    /// Authentication service implementation
    /// Handles Google OAuth and local authentication
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly Microsoft.Extensions.Logging.ILogger<AuthService> _logger;
        private readonly string _jwtKey;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _jwtExpirationMinutes;

        public AuthService(
            IUserRepository userRepository,
            IUserProfileRepository userProfileRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            Microsoft.Extensions.Logging.ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _userProfileRepository = userProfileRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;

            // Load JWT configuration
            _jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key not configured");
            _jwtIssuer = _configuration["Jwt:Issuer"] ?? "SuperApp";
            _jwtAudience = _configuration["Jwt:Audience"] ?? "SuperApp-API";
            _jwtExpirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "15");
        }

        /// <summary>
        /// Authenticate user with Google OAuth authorization code
        /// </summary>
        /// <param name="authorizationCode">Authorization code from Google OAuth flow</param>
        /// <param name="codeVerifier">PKCE code verifier (required)</param>
        public async Task<AuthResponse> GoogleLoginAsync(string authorizationCode, string? codeVerifier = null)
        {
            try
            {
                // Enforce PKCE - codeVerifier is required
                if (string.IsNullOrEmpty(codeVerifier))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "PKCE code verifier is required",
                        Error = "Missing code_verifier"
                    };
                }

                _logger.LogInformation("Starting Google login with authorization code (PKCE enabled)");

                // Step 1: Exchange authorization code for Google tokens
                var googleTokenResponse = await ExchangeCodeForGoogleTokenAsync(authorizationCode, codeVerifier);
                _logger.LogInformation("Successfully exchanged code for Google tokens");

                // Step 2: Verify ID token using Google.Apis.Auth library (offline, validates audience)
                var googleUserInfo = await VerifyGoogleIdTokenAsync(googleTokenResponse.IdToken);
                _logger.LogInformation("Successfully verified Google ID token for email: {Email}", googleUserInfo.Email);

                // Step 3: Get or create user in database
                var user = await GetOrCreateGoogleUserAsync(googleUserInfo);
                _logger.LogInformation("User found/created with ID: {UserId}", user.Id);

                // Step 4: Save Google tokens for Drive access
                user.GoogleAccessToken = googleTokenResponse.AccessToken;
                user.GoogleRefreshToken = googleTokenResponse.RefreshToken;
                user.GoogleTokenExpiresAt = DateTime.UtcNow.AddSeconds(googleTokenResponse.ExpiresIn);

                // Step 5: Update last login
                user.RecordLogin();
                await _userRepository.UpdateAsync(user);

                // Step 6: Get UserProfile (if exists) to include filters
                string? userFilters = null;
                try
                {
                    var userProfile = await _userProfileRepository.GetByUserIdAsync(user.Id);
                    if (userProfile != null)
                    {
                        userFilters = userProfile.Filters;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get user profile for userId: {UserId}", user.Id);
                    // Continue without filters - this is not a critical error
                }

                // Step 7: Generate JWT token
                var jwtToken = GenerateJwtToken(user);

                // Step 8: Generate refresh token and store in DB
                var (plaintext, refreshTokenEntity) = await GenerateAndStoreRefreshTokenAsync(user.Id);

                // Step 9: Build response
                return new AuthResponse
                {
                    Success = true,
                    Message = "Login successful",
                    User = new UserData
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = googleUserInfo.FirstName,
                        LastName = googleUserInfo.LastName,
                        Picture = googleUserInfo.Picture,
                        AuthType = "google",
                        Token = jwtToken,
                        TokenType = "Bearer",
                        Filters = userFilters
                    },
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
                    RefreshTokenPlaintext = plaintext
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google login failed: {ErrorMessage}", ex.Message);
                return new AuthResponse
                {
                    Success = false,
                    Message = "Google login failed",
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Authenticate user with username and password
        /// </summary>
        public async Task<AuthResponse> LocalLoginAsync(string username, string password)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(username);

                if (user == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid credentials",
                        Error = "User not found"
                    };
                }

                // Verify password (assuming BCrypt is used)
                if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid credentials",
                        Error = "Invalid password"
                    };
                }

                // Update last login
                user.RecordLogin();
                await _userRepository.UpdateAsync(user);

                // Generate JWT token
                var jwtToken = GenerateJwtToken(user);

                // Generate refresh token and store in DB
                var (plaintext, _) = await GenerateAndStoreRefreshTokenAsync(user.Id);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Login successful",
                    User = new UserData
                    {
                        Id = user.Id,
                        Email = user.Email,
                        Phone = user.Phone,
                        AuthType = "local",
                        Token = jwtToken,
                        TokenType = "Bearer"
                    },
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
                    RefreshTokenPlaintext = plaintext
                };
            }
            catch (Exception ex)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Login failed",
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Exchange authorization code for Google tokens
        /// Supports PKCE (RFC 7636) when code_verifier is provided
        /// </summary>
        private async Task<GoogleTokenResponse> ExchangeCodeForGoogleTokenAsync(string code, string? codeVerifier = null)
        {
            var client = _httpClientFactory.CreateClient();
            var tokenEndpoint = "https://oauth2.googleapis.com/token";

            var clientId = _configuration["OAuth:Google:ClientId"] ?? "";
            var clientSecret = _configuration["OAuth:Google:ClientSecret"] ?? "";
            var redirectUri = _configuration["OAuth:Google:RedirectUri"] ?? "";

            var requestData = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" }
            };

            if (!string.IsNullOrEmpty(codeVerifier))
            {
                requestData.Add("code_verifier", codeVerifier);
            }

            var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(requestData));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Google token exchange failed. Status: {StatusCode}", response.StatusCode);
                throw new Exception($"Failed to exchange code: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.IdToken))
            {
                throw new Exception("Invalid token response from Google");
            }

            return tokenResponse;
        }

        /// <summary>
        /// Verify Google ID token using Google.Apis.Auth library (offline validation, validates audience)
        /// </summary>
        private async Task<GoogleUserInfo> VerifyGoogleIdTokenAsync(string idToken)
        {
            var clientId = _configuration["OAuth:Google:ClientId"] ?? "";

            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);

            if (!payload.EmailVerified)
            {
                throw new Exception("Google account email is not verified");
            }

            return new GoogleUserInfo
            {
                Sub = payload.Subject,
                Email = payload.Email,
                EmailVerified = payload.EmailVerified,
                GivenName = payload.GivenName,
                FamilyName = payload.FamilyName,
                Picture = payload.Picture,
            };
        }

        /// <summary>
        /// Get existing user or create new Google user
        /// </summary>
        private async Task<User> GetOrCreateGoogleUserAsync(GoogleUserInfo googleUserInfo)
        {
            var existingUser = await _userRepository.GetByEmailAsync(googleUserInfo.Email);

            if (existingUser != null)
            {
                // If previously a local account, keep authType as-is - do not silently overwrite
                if (existingUser.AuthType != "google")
                {
                    _logger.LogWarning(
                        "User {Email} previously authenticated via '{AuthType}', now logging in via Google. AuthType not changed.",
                        existingUser.Email, existingUser.AuthType);
                }
                return existingUser;
            }

            // Create new user
            var newUser = new User
            {
                Email = googleUserInfo.Email,
                Password = "", // No password for OAuth users
                AuthType = "google",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            return await _userRepository.CreateAsync(newUser);
        }

        /// <summary>
        /// Generate JWT token for authenticated user
        /// </summary>
        public string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtIssuer,
                audience: _jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Refresh access token using a valid refresh token (token rotation)
        /// </summary>
        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var tokenHash = HashToken(refreshToken);
            var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

            if (storedToken == null)
            {
                return new AuthResponse { Success = false, Message = "Invalid refresh token", Error = "Token not found" };
            }

            // Reuse attack detection: token was already revoked
            if (storedToken.RevokedAt != null)
            {
                _logger.LogWarning("Refresh token reuse detected for userId: {UserId}. Revoking all tokens.", storedToken.UserId);
                await _refreshTokenRepository.RevokeAllUserTokensAsync(storedToken.UserId);
                return new AuthResponse { Success = false, Message = "Token reuse detected", Error = "Security violation" };
            }

            if (storedToken.ExpiresAt <= DateTime.UtcNow)
            {
                return new AuthResponse { Success = false, Message = "Refresh token expired", Error = "Token expired" };
            }

            var user = storedToken.User;

            // Generate new tokens
            var newJwtToken = GenerateJwtToken(user);
            var (newPlaintext, newTokenEntity) = await GenerateAndStoreRefreshTokenAsync(user.Id);

            // Revoke old token and link to new one
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.ReplacedByTokenHash = newTokenEntity.TokenHash;
            await _refreshTokenRepository.UpdateAsync(storedToken);

            string? userFilters = null;
            try
            {
                var userProfile = await _userProfileRepository.GetByUserIdAsync(user.Id);
                if (userProfile != null) userFilters = userProfile.Filters;
            }
            catch { }

            return new AuthResponse
            {
                Success = true,
                Message = "Token refreshed",
                User = new UserData
                {
                    Id = user.Id,
                    Email = user.Email,
                    Phone = user.Phone,
                    AuthType = user.AuthType,
                    Token = newJwtToken,
                    TokenType = "Bearer",
                    Filters = userFilters
                },
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
                RefreshTokenPlaintext = newPlaintext
            };
        }

        /// <summary>
        /// Revoke a refresh token (logout)
        /// </summary>
        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var tokenHash = HashToken(refreshToken);
            var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);
            if (storedToken != null && storedToken.RevokedAt == null)
            {
                storedToken.RevokedAt = DateTime.UtcNow;
                await _refreshTokenRepository.UpdateAsync(storedToken);
            }
        }

        /// <summary>
        /// Generate a cryptographically secure refresh token, store SHA-256 hash in DB
        /// </summary>
        private async Task<(string plaintext, RefreshToken entity)> GenerateAndStoreRefreshTokenAsync(int userId)
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            var plaintext = Convert.ToBase64String(randomBytes)
                .Replace('+', '-').Replace('/', '_').Replace("=", "");
            var tokenHash = HashToken(plaintext);

            var entity = new RefreshToken
            {
                TokenHash = tokenHash,
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            await _refreshTokenRepository.CreateAsync(entity);
            return (plaintext, entity);
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }

    /// <summary>
    /// Google token response model
    /// </summary>
    internal class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("id_token")]
        public string IdToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
    }

    /// <summary>
    /// Google user info model (populated from verified JWT payload)
    /// </summary>
    internal class GoogleUserInfo
    {
        public string? Sub { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public string? GivenName { get; set; }
        public string? FamilyName { get; set; }
        public string? Picture { get; set; }

        public string? FirstName => GivenName;
        public string? LastName => FamilyName;
    }
}
