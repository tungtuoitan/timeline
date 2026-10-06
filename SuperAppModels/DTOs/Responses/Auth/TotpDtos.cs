using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Responses.Auth
{
    /// <summary>GET /api/security/totp/status (TungRoot #1489)</summary>
    public class TotpStatusDto
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        /// <summary>Set while wrong codes have locked the factor.</summary>
        [JsonPropertyName("lockedUntil")]
        public DateTime? LockedUntil { get; set; }
    }

    /// <summary>POST /api/security/totp/setup — show as a QR code (otpauth URI) or type the secret.</summary>
    public class TotpSetupDto
    {
        [JsonPropertyName("otpauthUri")]
        public string OtpauthUri { get; set; } = string.Empty;

        [JsonPropertyName("secret")]
        public string Secret { get; set; } = string.Empty;
    }

    /// <summary>Result of confirm/unlock: a short-lived token for the X-Unlock-Token header.</summary>
    public class UnlockTokenDto
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("expiresAt")]
        public DateTime ExpiresAt { get; set; }
    }
}
