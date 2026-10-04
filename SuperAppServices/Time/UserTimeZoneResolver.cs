using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppModels.Time;

namespace SuperAppServices.Time
{
    /// <summary>
    /// Looks up a user's display timezone (urm.user_profiles.timezone, IANA id).
    /// The API middleware uses it to set the ambient <see cref="UserClock"/> per request.
    /// </summary>
    public interface IUserTimeZoneResolver
    {
        /// <summary>The user's timezone, or Asia/Ho_Chi_Minh when unset/unknown. Never throws.</summary>
        Task<TimeZoneInfo> GetTimeZoneAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>Drops the cached value (call after the user's profile timezone changes).</summary>
        void Invalidate(int userId);
    }

    public sealed class UserTimeZoneResolver : IUserTimeZoneResolver
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<UserTimeZoneResolver> _logger;

        public UserTimeZoneResolver(ApplicationDbContext context, IMemoryCache cache, ILogger<UserTimeZoneResolver> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static string CacheKey(int userId) => $"user-timezone:{userId}";

        public async Task<TimeZoneInfo> GetTimeZoneAsync(int userId, CancellationToken cancellationToken = default)
        {
            if (userId <= 0) return TimeZones.Default;
            if (_cache.TryGetValue(CacheKey(userId), out TimeZoneInfo? cached) && cached != null) return cached;

            try
            {
                var id = await _context.UserProfiles.AsNoTracking()
                    .Where(p => p.UserId == userId)
                    .Select(p => p.Timezone)
                    .FirstOrDefaultAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(id) && !TimeZones.TryFind(id.Trim(), out _))
                    _logger.LogWarning("Unknown timezone '{TimeZone}' for user {UserId}; using {Default}", id, userId, TimeZones.DefaultId);

                var tz = TimeZones.FindOrDefault(id);
                _cache.Set(CacheKey(userId), tz, CacheDuration);
                return tz;
            }
            catch (OperationCanceledException)
            {
                return TimeZones.Default;
            }
            catch (Exception ex)
            {
                // Never fail a request because of display timezone — fall back without caching.
                _logger.LogWarning(ex, "Could not resolve timezone for user {UserId}; using {Default}", userId, TimeZones.DefaultId);
                return TimeZones.Default;
            }
        }

        public void Invalidate(int userId) => _cache.Remove(CacheKey(userId));
    }
}
