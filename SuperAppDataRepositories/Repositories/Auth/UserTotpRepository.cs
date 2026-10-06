using Microsoft.EntityFrameworkCore;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins.Auth;
using SuperAppModels.Models.Auth;

namespace SuperAppDataRepositories.Repositories.Auth
{
    public class UserTotpRepository : IUserTotpRepository
    {
        private readonly ApplicationDbContext _context;

        public UserTotpRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task<UserTotp?> GetAsync(int userId)
            => _context.UserTotps.FirstOrDefaultAsync(t => t.UserId == userId);

        public async Task SaveAsync(UserTotp totp)
        {
            totp.UpdatedAt = DateTime.UtcNow;
            if (_context.Entry(totp).State == EntityState.Detached)
            {
                totp.CreatedAt = totp.UpdatedAt;
                _context.UserTotps.Add(totp);
            }
            await _context.SaveChangesAsync();
        }

        public Task<string?> GetEmailAsync(int userId)
            => _context.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => (string?)u.Email).FirstOrDefaultAsync();
    }
}
