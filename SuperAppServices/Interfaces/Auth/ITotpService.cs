using SuperAppModels.DTOs;

namespace SuperAppServices.Interfaces.Auth
{
    /// <summary>
    /// TOTP second factor guarding the private part of the homepage (TungRoot #1489).
    /// Status codes in ResultOptions: 400 wrong code, 404 not set up, 409 already enabled, 423 locked.
    /// </summary>
    public interface ITotpService
    {
        Task<ResultOptions> GetStatusAsync(int userId);

        /// <summary>New pending secret + otpauth URI for the QR code (only while not enabled).</summary>
        Task<ResultOptions> SetupAsync(int userId);

        /// <summary>First code from the app → enables TOTP and returns an unlock token right away.</summary>
        Task<ResultOptions> ConfirmAsync(int userId, string code);

        /// <summary>Code → unlock token (X-Unlock-Token) valid for 15 minutes.</summary>
        Task<ResultOptions> UnlockAsync(int userId, string code);

        /// <summary>Turn TOTP off (needs a current code).</summary>
        Task<ResultOptions> DisableAsync(int userId, string code);

        /// <summary>Is this X-Unlock-Token valid for the user right now?</summary>
        bool IsUnlocked(int userId, string? unlockToken);
    }
}
