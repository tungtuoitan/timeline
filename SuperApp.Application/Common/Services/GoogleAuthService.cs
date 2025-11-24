using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Common.Interfaces;

namespace SuperApp.Application.Common.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GoogleAuthService> _logger;

        public GoogleAuthService(
            IConfiguration config,
            IHttpClientFactory httpClientFactory,
            ILogger<GoogleAuthService> logger)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<GoogleUserInfo?> VerifyGoogleTokenAsync(string code)
        {
            try
            {
                var googleSettings = _config.GetSection("Google");
                var clientId = googleSettings["ClientId"];
                var clientSecret = googleSettings["ClientSecret"];
                var redirectUri = googleSettings["RedirectUri"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    _logger.LogError("Google OAuth settings not configured");
                    return null;
                }

                var httpClient = _httpClientFactory.CreateClient();

                // Exchange authorization code for access token
                var tokenResponse = await httpClient.PostAsync(
                    "https://oauth2.googleapis.com/token",
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["code"] = code,
                        ["client_id"] = clientId,
                        ["client_secret"] = clientSecret,
                        ["redirect_uri"] = redirectUri ?? "",
                        ["grant_type"] = "authorization_code"
                    }));

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to exchange Google auth code: {Error}", errorContent);
                    return null;
                }

                var tokenData = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>();
                if (tokenData == null || string.IsNullOrEmpty(tokenData.AccessToken))
                {
                    _logger.LogError("Invalid token response from Google");
                    return null;
                }

                // Get user info using access token
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenData.AccessToken);

                var userInfoResponse = await httpClient.GetAsync(
                    "https://www.googleapis.com/oauth2/v2/userinfo");

                if (!userInfoResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get Google user info");
                    return null;
                }

                var googleUserResponse = await userInfoResponse.Content.ReadFromJsonAsync<GoogleUserInfoResponse>();
                if (googleUserResponse == null)
                {
                    _logger.LogError("Invalid user info response from Google");
                    return null;
                }

                return new GoogleUserInfo
                {
                    Email = googleUserResponse.Email ?? string.Empty,
                    Name = googleUserResponse.Name,
                    GivenName = googleUserResponse.GivenName,
                    FamilyName = googleUserResponse.FamilyName,
                    Picture = googleUserResponse.Picture,
                    EmailVerified = googleUserResponse.VerifiedEmail ?? false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Google token");
                return null;
            }
        }

        private class GoogleTokenResponse
        {
            public string? AccessToken { get; set; }
            public string? TokenType { get; set; }
            public int? ExpiresIn { get; set; }
            public string? RefreshToken { get; set; }
            public string? IdToken { get; set; }
        }

        private class GoogleUserInfoResponse
        {
            public string? Id { get; set; }
            public string? Email { get; set; }
            public bool? VerifiedEmail { get; set; }
            public string? Name { get; set; }
            public string? GivenName { get; set; }
            public string? FamilyName { get; set; }
            public string? Picture { get; set; }
        }
    }
}
