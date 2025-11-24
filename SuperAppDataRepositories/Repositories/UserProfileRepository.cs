using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    public class UserProfileRepository : IUserProfileRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserProfileRepository> _logger;

        public UserProfileRepository(ApplicationDbContext context, ILogger<UserProfileRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UserProfile?> GetByEmailAsync(string email)
        {
            try
            {
                return await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile by email: {Email}", email);
                throw;
            }
        }

        public async Task<List<UserProfile>> GetAllAsync()
        {
            try
            {
                return await _context.UserProfiles
                    .Where(up => up.IsActive)
                    .OrderBy(up => up.Email)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all user profiles");
                throw;
            }
        }

        public async Task<UserProfile> CreateAsync(UserProfile userProfile)
        {
            try
            {
                userProfile.CreatedAt = DateTime.UtcNow;
                _context.UserProfiles.Add(userProfile);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created user profile for email: {Email}", userProfile.Email);
                return userProfile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user profile for email: {Email}", userProfile.Email);
                throw;
            }
        }

        public async Task<UserProfile> UpdateAsync(UserProfile userProfile)
        {
            try
            {
                userProfile.UpdatedAt = DateTime.UtcNow;
                _context.UserProfiles.Update(userProfile);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated user profile for email: {Email}", userProfile.Email);
                return userProfile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for email: {Email}", userProfile.Email);
                throw;
            }
        }

        public async Task<UserProfile> CreateOrUpdateAsync(UserProfile userProfile)
        {
            try
            {
                var existing = await GetByEmailAsync(userProfile.Email);
                if (existing != null)
                {
                    existing.AppC = userProfile.AppC;
                    existing.Parents = userProfile.Parents;
                    existing.Priorities = userProfile.Priorities;
                    existing.Statuses = userProfile.Statuses;
                    existing.Types = userProfile.Types;
                    existing.RepeatTypes = userProfile.RepeatTypes;
                    existing.IsUpdatedTodays = userProfile.IsUpdatedTodays;
                    existing.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated existing user profile for email: {Email}", userProfile.Email);
                    return existing;
                }
                else
                {
                    return await CreateAsync(userProfile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating or updating user profile for email: {Email}", userProfile.Email);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string email)
        {
            try
            {
                var userProfile = await GetByEmailAsync(email);
                if (userProfile == null)
                    return false;

                userProfile.IsActive = false;
                userProfile.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Soft deleted user profile for email: {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user profile for email: {Email}", email);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(string email)
        {
            try
            {
                return await _context.UserProfiles
                    .AnyAsync(up => up.Email == email && up.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user profile exists: {Email}", email);
                throw;
            }
        }
    }
}
