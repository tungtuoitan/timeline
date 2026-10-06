namespace SuperAppModels.Models.Auth
{
    /// <summary>
    /// TOTP second factor of one user (auth.user_totp, TungRoot #1489) — guards the private part of
    /// the homepage. Secrets are base32 (RFC 4648), stored as is (decision 06/10: the DB already holds
    /// everything they protect).
    /// </summary>
    public class UserTotp
    {
        public int UserId { get; set; }

        /// <summary>Active secret; null until the first code is confirmed.</summary>
        public string? Secret { get; set; }

        /// <summary>Secret shown as a QR code, waiting for its first code.</summary>
        public string? PendingSecret { get; set; }

        public DateTime? EnabledAt { get; set; }

        /// <summary>Time step of the last accepted code — a code can be used once.</summary>
        public long? LastUsedStep { get; set; }

        /// <summary>Wrong codes in a row (reset on success).</summary>
        public int FailedCount { get; set; }

        public DateTime? LockedUntil { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
