using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User?> GetByIdAsync(int userId)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                throw;
            }
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by username: {Username}", username);
                throw;
            }
        }

        public async Task<List<User>> GetAllAsync()
        {
            try
            {
                return await _context.Users
                    .OrderBy(u => u.Email)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                throw;
            }
        }

        public async Task<User> CreateAsync(User user)
        {
            try
            {
                user.CreatedAt = DateTime.UtcNow;
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created user with email: {Email}", user.Email);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user with email: {Email}", user.Email);
                throw;
            }
        }

        public async Task<User> UpdateAsync(User user)
        {
            try
            {
                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated user with ID: {UserId}", user.UserId);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID: {UserId}", user.UserId);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int userId)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                // Soft delete
                user.Deactivate();
                await _context.SaveChangesAsync();

                _logger.LogInformation("Soft deleted user with ID: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(string email)
        {
            try
            {
                return await _context.Users
                    .AnyAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user exists: {Email}", email);
                throw;
            }
        }

        public async Task<bool> ValidateCredentialsAsync(string email, string passwordHash)
        {
            try
            {
                return await _context.Users
                    .AnyAsync(u => u.Email == email && u.PasswordHash == passwordHash && u.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials for: {Email}", email);
                throw;
            }
        }
    }
}
