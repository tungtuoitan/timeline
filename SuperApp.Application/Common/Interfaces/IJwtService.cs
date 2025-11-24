using SuperAppModels.Models;

namespace SuperApp.Application.Common.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        string GenerateToken(string email, string? displayName = null);
    }
}
