using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests.Auth
{
    /// <summary>A 6-digit code from the authenticator app (TungRoot #1489).</summary>
    public class TotpCodeRequest
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
    }
}
