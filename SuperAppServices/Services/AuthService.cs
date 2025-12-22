using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly Microsoft.Extensions.Logging.ILogger<AuthService> _logger;
        private readonly string _jwtKey;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _jwtExpirationMinutes;

        public AuthService(
            IUserRepository userRepository,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            Microsoft.Extensions.Logging.ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;

            // Load JWT configuration
            _jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key not configured");
            _jwtIssuer = _configuration["Jwt:Issuer"] ?? "SuperApp";
            _jwtAudience = _configuration["Jwt:Audience"] ?? "SuperApp-API";
            _jwtExpirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");
        }

        /// <summary>
        /// Authenticate user with Google OAuth authorization code
        /// </summary>
        public async Task<AuthResponse> GoogleLoginAsync(string authorizationCode)
        {
            try
            {
                _logger.LogInformation("Starting Google login with authorization code");

                // Step 1: Exchange authorization code for Google tokens
                var googleTokenResponse = await ExchangeCodeForGoogleTokenAsync(authorizationCode);
                _logger.LogInformation("Successfully exchanged code for Google tokens");

                // Step 2: Verify ID token and get user info
                var googleUserInfo = await VerifyGoogleIdTokenAsync(googleTokenResponse.IdToken);
                _logger.LogInformation("Successfully verified Google ID token for email: {Email}", googleUserInfo.Email);

                // Step 3: Get or create user in database
                var user = await GetOrCreateGoogleUserAsync(googleUserInfo);
                _logger.LogInformation("User found/created with ID: {UserId}", user.Id);

                // Step 4: Update last login
                user.RecordLogin();
                await _userRepository.UpdateAsync(user);

                // Step 5: Generate JWT token
                var jwtToken = GenerateJwtToken(user);

                // Step 6: Build response
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
                        TokenType = "Bearer"
                    },
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes)
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
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes)
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
        /// </summary>
        private async Task<GoogleTokenResponse> ExchangeCodeForGoogleTokenAsync(string code)
        {
            var client = _httpClientFactory.CreateClient();
            var tokenEndpoint = "https://oauth2.googleapis.com/token";

            var requestData = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", _configuration["OAuth:Google:ClientId"] ?? "" },
                { "client_secret", _configuration["OAuth:Google:ClientSecret"] ?? "" },
                { "redirect_uri", _configuration["OAuth:Google:RedirectUri"] ?? "" },
                { "grant_type", "authorization_code" }
            };

            var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(requestData));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to exchange code: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent, new JsonSerializerOptions
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
        /// Verify Google ID token and extract user info
        /// </summary>
        private async Task<GoogleUserInfo> VerifyGoogleIdTokenAsync(string idToken)
        {
            var client = _httpClientFactory.CreateClient();
            var verifyEndpoint = $"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}";

            var response = await client.GetAsync(verifyEndpoint);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Invalid Google ID token");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (userInfo == null || string.IsNullOrEmpty(userInfo.Email))
            {
                throw new Exception("Invalid user info from Google");
            }

            return userInfo;
        }

        /// <summary>
        /// Get existing user or create new Google user
        /// </summary>
        private async Task<User> GetOrCreateGoogleUserAsync(GoogleUserInfo googleUserInfo)
        {
            var existingUser = await _userRepository.GetByEmailAsync(googleUserInfo.Email);

            if (existingUser != null)
            {
                // Update auth type if it was previously local
                if (existingUser.AuthType != "google")
                {
                    existingUser.AuthType = "google";
                    await _userRepository.UpdateAsync(existingUser);
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
    /// Google user info model
    /// </summary>
    internal class GoogleUserInfo
    {
        [JsonPropertyName("sub")]
        public string? Sub { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("email_verified")]
        public string EmailVerifiedString { get; set; } = "false";

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("given_name")]
        public string? GivenName { get; set; }

        [JsonPropertyName("family_name")]
        public string? FamilyName { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }

        [JsonPropertyName("iat")]
        public string? Iat { get; set; }

        [JsonPropertyName("exp")]
        public string? Exp { get; set; }

        // Computed properties
        public bool EmailVerified => EmailVerifiedString?.ToLower() == "true";
        public string? FirstName => GivenName;
        public string? LastName => FamilyName;
    }
}
