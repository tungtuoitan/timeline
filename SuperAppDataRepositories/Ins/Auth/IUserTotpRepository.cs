using SuperAppModels.Models.Auth;

namespace SuperAppDataRepositories.Ins.Auth
{
    /// <summary>TOTP state per user (TungRoot #1489).</summary>
    public interface IUserTotpRepository
    {
        /// <summary>Tracked row, or null when the user never started setup.</summary>
        Task<UserTotp?> GetAsync(int userId);

        /// <summary>Insert (new) or save (tracked) the row; UpdatedAt is set here.</summary>
        Task SaveAsync(UserTotp totp);

        /// <summary>Account label for the authenticator app.</summary>
        Task<string?> GetEmailAsync(int userId);
    }
}
