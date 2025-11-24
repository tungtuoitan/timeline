namespace SuperApp.Application.Common.Interfaces
{
    public interface IGoogleAuthService
    {
        Task<GoogleUserInfo?> VerifyGoogleTokenAsync(string code);
    }

    public class GoogleUserInfo
    {
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? GivenName { get; set; }
        public string? FamilyName { get; set; }
        public string? Picture { get; set; }
        public bool EmailVerified { get; set; }
    }
}
