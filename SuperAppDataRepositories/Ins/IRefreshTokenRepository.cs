using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
        Task<RefreshToken> CreateAsync(RefreshToken token);
        Task UpdateAsync(RefreshToken token);
        Task RevokeAllUserTokensAsync(int userId);
    }
}
