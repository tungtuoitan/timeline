using System.Security.Cryptography;
using System.Text;

namespace SuperAppServices.Services.Auth
{
    /// <summary>
    /// Short-lived proof that the user just passed TOTP (TungRoot #1489), sent as X-Unlock-Token.
    /// Format "userId.expUnix.signature" — HMAC-SHA256 with a key derived from Jwt:Key, so it can never
    /// be confused with (or used as) an access token. Pure: key and clock are passed in.
    /// </summary>
    public static class UnlockToken
    {
        public const string HeaderName = "X-Unlock-Token";
        public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

        public static byte[] DeriveKey(string jwtKey)
            => HMACSHA256.HashData(Encoding.UTF8.GetBytes(jwtKey), Encoding.UTF8.GetBytes("superapp-unlock-token-v1"));

        public static string Issue(byte[] key, int userId, DateTime expiresUtc)
        {
            var payload = $"{userId}.{new DateTimeOffset(DateTime.SpecifyKind(expiresUtc, DateTimeKind.Utc)).ToUnixTimeSeconds()}";
            return $"{payload}.{Sign(key, payload)}";
        }

        /// <summary>True when the token was issued for <paramref name="userId"/> and has not expired.</summary>
        public static bool IsValid(byte[] key, string? token, int userId, DateTime nowUtc)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            var parts = token.Trim().Split('.');
            if (parts.Length != 3 || !int.TryParse(parts[0], out var uid) || !long.TryParse(parts[1], out var exp)) return false;
            if (uid != userId || DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime <= nowUtc) return false;
            var expected = Encoding.ASCII.GetBytes(Sign(key, $"{parts[0]}.{parts[1]}"));
            return CryptographicOperations.FixedTimeEquals(expected, Encoding.ASCII.GetBytes(parts[2]));
        }

        private static string Sign(byte[] key, string payload)
            => Convert.ToBase64String(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(payload))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
