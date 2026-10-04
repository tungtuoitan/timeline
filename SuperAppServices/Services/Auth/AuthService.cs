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

namespace SuperAppServices.Services.Auth
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
        private readonly ILogger<AuthService> _logger;
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
            ILogger<AuthService> logger)
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
        private const string DefaultCliRedirectUri = "http://localhost:3000/auth/callback";
        private const string GoogleScope = "openid profile email https://www.googleapis.com/auth/drive.file";

        /// <summary>
        /// Allowed redirect URIs: the web callback plus CLI loopback URIs (comma-separated
        /// OAuth:Google:CliRedirectUris, default http://localhost:3000/auth/callback — already
        /// registered on the Google client for local dev).
        /// </summary>
        private HashSet<string> AllowedGoogleRedirectUris()
        {
            var allowed = new HashSet<string>(StringComparer.Ordinal);
            var web = _configuration["OAuth:Google:RedirectUri"];
            if (!string.IsNullOrWhiteSpace(web)) allowed.Add(web);
            var cli = _configuration["OAuth:Google:CliRedirectUris"] ?? DefaultCliRedirectUri;
            foreach (var uri in cli.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                allowed.Add(uri);
            return allowed;
        }

        public object GetGoogleCliConfig()
        {
            var cli = (_configuration["OAuth:Google:CliRedirectUris"] ?? DefaultCliRedirectUri)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return new
            {
                clientId = _configuration["OAuth:Google:ClientId"] ?? "",
                redirectUri = cli.FirstOrDefault() ?? DefaultCliRedirectUri,
                scope = GoogleScope,
                authUrl = "https://accounts.google.com/o/oauth2/v2/auth"
            };
        }

        public async Task<AuthResponse> GoogleLoginAsync(string authorizationCode, string? codeVerifier = null, string? redirectUri = null, string? deviceId = null)
        {
            try
            {
                if (redirectUri != null && !AllowedGoogleRedirectUris().Contains(redirectUri))
                {
                    _logger.LogWarning("Google login rejected: redirect URI not allowed: {RedirectUri}", redirectUri);
                    return new AuthResponse { Success = false, Message = "Google login failed", Error = "Redirect URI not allowed" };
                }

                // Enforce PKCE - codeVerifier is required
                if (string.IsNullOrEmpty(codeVerifier))
                {
                    _logger.LogWarning("Google login rejected: missing PKCE code_verifier");
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "PKCE code verifier is required",
                        Error = "Missing code_verifier"
                    };
                }

                _logger.LogInformation("Google login started. CodeLength={CodeLength}, VerifierLength={VerifierLength}",
                    authorizationCode.Length, codeVerifier.Length);

                // Step 1: Exchange authorization code for Google tokens
                GoogleTokenResponse googleTokenResponse;
                try
                {
                    googleTokenResponse = await ExchangeCodeForGoogleTokenAsync(authorizationCode, codeVerifier, redirectUri);
                    _logger.LogInformation("Google token exchange succeeded. HasRefreshToken={HasRefreshToken}, ExpiresIn={ExpiresIn}s",
                        !string.IsNullOrEmpty(googleTokenResponse.RefreshToken), googleTokenResponse.ExpiresIn);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Google token exchange failed");
                    return new AuthResponse { Success = false, Message = "Google login failed", Error = ex.Message };
                }

                // Step 2: Verify ID token using Google.Apis.Auth library (offline, validates audience)
                GoogleUserInfo googleUserInfo;
                try
                {
                    googleUserInfo = await VerifyGoogleIdTokenAsync(googleTokenResponse.IdToken);
                    _logger.LogInformation("Google ID token verified. Email={Email}, EmailVerified={EmailVerified}",
                        googleUserInfo.Email, googleUserInfo.EmailVerified);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Google ID token verification failed");
                    return new AuthResponse { Success = false, Message = "Google login failed", Error = ex.Message };
                }

                // Step 3: Get or create user in database
                var user = await GetOrCreateGoogleUserAsync(googleUserInfo);
                _logger.LogInformation("User resolved. UserId={UserId}, Email={Email}", user.Id, user.Email);

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
                        _logger.LogDebug("UserProfile loaded for UserId={UserId}, HasFilters={HasFilters}", user.Id, !string.IsNullOrEmpty(userFilters));
                    }
                    else
                    {
                        _logger.LogDebug("No UserProfile found for UserId={UserId}", user.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get user profile for UserId={UserId}, continuing without filters", user.Id);
                }

                // Step 7: Generate JWT token
                var jwtToken = GenerateJwtToken(user);
                _logger.LogDebug("JWT token generated for UserId={UserId}", user.Id);

                // Step 8: Generate refresh token and store in DB
                var (plaintext, refreshTokenEntity) = await GenerateAndStoreRefreshTokenAsync(user.Id, deviceId);
                _logger.LogDebug("Refresh token stored. TokenId={TokenId}, ExpiresAt={ExpiresAt}", refreshTokenEntity.Id, refreshTokenEntity.ExpiresAt);

                _logger.LogInformation("Google login successful. UserId={UserId}, Email={Email}", user.Id, user.Email);

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
                _logger.LogError(ex, "Unexpected error during Google login");
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
            _logger.LogInformation("[AUTH] local-login-start | Username={Username}", username);
            try
            {
                var user = await _userRepository.GetByEmailAsync(username);

                if (user == null)
                {
                    _logger.LogWarning("[AUTH] local-login-failed | Username={Username} | Reason=UserNotFound", username);
                    return new AuthResponse { Success = false, Message = "Invalid credentials", Error = "User not found" };
                }

                if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
                {
                    _logger.LogWarning("[AUTH] local-login-failed | Username={Username} | UserId={UserId} | Reason=WrongPassword", username, user.Id);
                    return new AuthResponse { Success = false, Message = "Invalid credentials", Error = "Invalid password" };
                }

                user.RecordLogin();
                await _userRepository.UpdateAsync(user);

                var jwtToken = GenerateJwtToken(user);
                var (plaintext, tokenEntity) = await GenerateAndStoreRefreshTokenAsync(user.Id);

                _logger.LogInformation(
                    "[AUTH] local-login-success | UserId={UserId} | Username={Username} | TokenId={TokenId} | RefreshExpiresAt={RefreshExpiresAt}",
                    user.Id, username, tokenEntity.Id, tokenEntity.ExpiresAt);

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
                _logger.LogError(ex, "[AUTH] local-login-exception | Username={Username}", username);
                return new AuthResponse { Success = false, Message = "Login failed", Error = ex.Message };
            }
        }

        /// <summary>
        /// Create a new local user (email + password). Hashes password with BCrypt.
        /// </summary>
        public async Task<AuthResponse> SignupAsync(string email, string password)
        {
            _logger.LogInformation("[AUTH] signup-start | Email={Email}", email);
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    return new AuthResponse { Success = false, Message = "Email and password are required", Error = "Invalid request" };
                }

                if (password.Length < 6)
                {
                    return new AuthResponse { Success = false, Message = "Password must be at least 6 characters", Error = "Invalid password" };
                }

                var existing = await _userRepository.GetByEmailAsync(email);
                if (existing != null)
                {
                    _logger.LogWarning("[AUTH] signup-failed | Email={Email} | Reason=AlreadyExists", email);
                    return new AuthResponse { Success = false, Message = "Email already registered", Error = "Duplicate email" };
                }

                var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
                var newUser = new User
                {
                    Email = email,
                    Password = passwordHash,
                    AuthType = "local",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                };

                var created = await _userRepository.CreateAsync(newUser);
                _logger.LogInformation("[AUTH] signup-success | UserId={UserId} | Email={Email}", created.Id, email);

                var jwtToken = GenerateJwtToken(created);
                var (plaintext, tokenEntity) = await GenerateAndStoreRefreshTokenAsync(created.Id);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Signup successful",
                    User = new UserData
                    {
                        Id = created.Id,
                        Email = created.Email,
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
                _logger.LogError(ex, "[AUTH] signup-exception | Email={Email}", email);
                return new AuthResponse { Success = false, Message = "Signup failed", Error = ex.Message };
            }
        }

        /// <summary>
        /// Exchange authorization code for Google tokens
        /// Supports PKCE (RFC 7636) when code_verifier is provided
        /// </summary>
        private async Task<GoogleTokenResponse> ExchangeCodeForGoogleTokenAsync(string code, string? codeVerifier = null, string? redirectUriOverride = null)
        {
            var client = _httpClientFactory.CreateClient();
            var tokenEndpoint = "https://oauth2.googleapis.com/token";

            var clientId = _configuration["OAuth:Google:ClientId"] ?? "";
            var clientSecret = _configuration["OAuth:Google:ClientSecret"] ?? "";
            var redirectUri = redirectUriOverride ?? _configuration["OAuth:Google:RedirectUri"] ?? "";

            _logger.LogDebug("Exchanging code with Google. RedirectUri={RedirectUri}, HasClientId={HasClientId}, HasClientSecret={HasClientSecret}, HasCodeVerifier={HasCodeVerifier}",
                redirectUri, !string.IsNullOrEmpty(clientId), !string.IsNullOrEmpty(clientSecret), !string.IsNullOrEmpty(codeVerifier));

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
                _logger.LogError("Google token exchange failed. StatusCode={StatusCode}, Response={ErrorContent}",
                    (int)response.StatusCode, errorContent);
                throw new Exception($"Failed to exchange code: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.IdToken))
            {
                _logger.LogError("Google returned success but token response is invalid or missing IdToken. ResponseLength={Length}", responseContent.Length);
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
                Audience = new[] { clientId },
                IssuedAtClockTolerance = TimeSpan.FromMinutes(5),
                ExpirationTimeClockTolerance = TimeSpan.FromMinutes(5)
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
        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, string? deviceId = null)
        {
            var tokenHash = HashToken(refreshToken);
            _logger.LogInformation("[AUTH] refresh-start | TokenHashPrefix={Prefix}", tokenHash[..8]);

            var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

            if (storedToken == null)
            {
                _logger.LogWarning("[AUTH] refresh-failed | Reason=TokenNotFound | TokenHashPrefix={Prefix}", tokenHash[..8]);
                return new AuthResponse { Success = false, Message = "Invalid refresh token", Error = "Token not found" };
            }

            _logger.LogInformation(
                "[AUTH] refresh-token-found | TokenId={TokenId} | UserId={UserId} | CreatedAt={CreatedAt} | ExpiresAt={ExpiresAt} | IsRevoked={IsRevoked}",
                storedToken.Id, storedToken.UserId, storedToken.CreatedAt, storedToken.ExpiresAt, storedToken.RevokedAt != null);

            if (storedToken.RevokedAt != null)
            {
                bool isRecentRotation = storedToken.ReplacedByTokenHash != null
                    && storedToken.RevokedAt > DateTime.UtcNow.AddSeconds(-30);

                if (!isRecentRotation)
                {
                    // Scope revocation to this device only when deviceId is known.
                    // Fall back to RevokeAll for legacy tokens (null DeviceId) to stay safe.
                    var effectiveDeviceId = deviceId ?? storedToken.DeviceId;
                    if (!string.IsNullOrEmpty(effectiveDeviceId))
                    {
                        _logger.LogWarning(
                            "[AUTH] refresh-reuse-attack | UserId={UserId} | TokenId={TokenId} | RevokedAt={RevokedAt} | HasReplacement={HasReplacement} | Action=RevokeDevice | DeviceId={DeviceId}",
                            storedToken.UserId, storedToken.Id, storedToken.RevokedAt, storedToken.ReplacedByTokenHash != null, effectiveDeviceId);
                        await _refreshTokenRepository.RevokeUserTokensByDeviceAsync(storedToken.UserId, effectiveDeviceId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[AUTH] refresh-reuse-attack | UserId={UserId} | TokenId={TokenId} | RevokedAt={RevokedAt} | HasReplacement={HasReplacement} | Action=RevokeAll",
                            storedToken.UserId, storedToken.Id, storedToken.RevokedAt, storedToken.ReplacedByTokenHash != null);
                        await _refreshTokenRepository.RevokeAllUserTokensAsync(storedToken.UserId);
                    }
                    return new AuthResponse { Success = false, Message = "Token reuse detected", Error = "Security violation" };
                }

                _logger.LogInformation(
                    "[AUTH] refresh-concurrent-race | UserId={UserId} | TokenId={TokenId} | RevokedAt={RevokedAt} | FollowingReplacement | Action=Redirect",
                    storedToken.UserId, storedToken.Id, storedToken.RevokedAt);

                var replacementToken = await _refreshTokenRepository.GetByTokenHashAsync(storedToken.ReplacedByTokenHash!);
                if (replacementToken == null || replacementToken.RevokedAt != null || replacementToken.ExpiresAt <= DateTime.UtcNow)
                {
                    _logger.LogWarning(
                        "[AUTH] refresh-failed | UserId={UserId} | Reason=ReplacementUnavailable | ReplacementExists={Exists} | ReplacementRevoked={Revoked}",
                        storedToken.UserId, replacementToken != null, replacementToken?.RevokedAt != null);
                    return new AuthResponse { Success = false, Message = "Refresh token expired or invalid", Error = "Token not available" };
                }

                storedToken = replacementToken;
                _logger.LogInformation("[AUTH] refresh-redirected-to-replacement | NewTokenId={TokenId} | UserId={UserId}", storedToken.Id, storedToken.UserId);
            }

            if (storedToken.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogWarning(
                    "[AUTH] refresh-failed | UserId={UserId} | TokenId={TokenId} | Reason=Expired | ExpiredAt={ExpiresAt}",
                    storedToken.UserId, storedToken.Id, storedToken.ExpiresAt);
                return new AuthResponse { Success = false, Message = "Refresh token expired", Error = "Token expired" };
            }

            var user = storedToken.User;
            var newJwtToken = GenerateJwtToken(user);
            var (newPlaintext, newTokenEntity) = await GenerateAndStoreRefreshTokenAsync(user.Id, deviceId);

            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.ReplacedByTokenHash = newTokenEntity.TokenHash;
            await _refreshTokenRepository.UpdateAsync(storedToken);

            _logger.LogInformation(
                "[AUTH] refresh-success | UserId={UserId} | Email={Email} | OldTokenId={OldId} | NewTokenId={NewId} | NewExpiresAt={NewExpires}",
                user.Id, user.Email, storedToken.Id, newTokenEntity.Id, newTokenEntity.ExpiresAt);

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
                _logger.LogInformation(
                    "[AUTH] logout-token-revoked | UserId={UserId} | TokenId={TokenId}",
                    storedToken.UserId, storedToken.Id);
            }
            else
            {
                _logger.LogInformation(
                    "[AUTH] logout-token-skip | TokenHashPrefix={Prefix} | Found={Found} | AlreadyRevoked={Revoked}",
                    tokenHash[..8], storedToken != null, storedToken?.RevokedAt != null);
            }
        }

        /// <summary>
        /// Generate a cryptographically secure refresh token, store SHA-256 hash in DB
        /// </summary>
        private async Task<(string plaintext, RefreshToken entity)> GenerateAndStoreRefreshTokenAsync(int userId, string? deviceId = null)
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            var plaintext = Convert.ToBase64String(randomBytes)
                .Replace('+', '-').Replace('/', '_').Replace("=", "");
            var tokenHash = HashToken(plaintext);

            var entity = new RefreshToken
            {
                TokenHash = tokenHash,
                UserId = userId,
                DeviceId = deviceId,
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
