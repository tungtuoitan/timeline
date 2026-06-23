namespace SuperAppModels.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByTokenHash { get; set; }
        public string? DeviceInfo { get; set; }
        public string? DeviceId { get; set; }

        public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;
    }
}
