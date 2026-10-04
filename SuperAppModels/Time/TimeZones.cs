using System.Collections.Concurrent;

namespace SuperAppModels.Time
{
    /// <summary>
    /// Resolves IANA timezone ids (e.g. "Asia/Ho_Chi_Minh", stored in urm.user_profiles.timezone)
    /// to <see cref="TimeZoneInfo"/> on both Linux (IANA native) and Windows (IANA via ICU,
    /// or the Windows id as a fallback). Unknown ids fall back to <see cref="Default"/>.
    /// </summary>
    public static class TimeZones
    {
        /// <summary>IANA id used when a user has no (valid) timezone set.</summary>
        public const string DefaultId = "Asia/Ho_Chi_Minh";

        private const string DefaultWindowsId = "SE Asia Standard Time";

        private static readonly ConcurrentDictionary<string, TimeZoneInfo> _cache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Asia/Ho_Chi_Minh (UTC+7, no DST).</summary>
        public static TimeZoneInfo Default { get; } = ResolveDefault();

        /// <summary>
        /// Returns the timezone for <paramref name="id"/>, or <see cref="Default"/> when the id is
        /// empty or unknown on this machine. Never throws.
        /// </summary>
        public static TimeZoneInfo FindOrDefault(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return Default;
            return TryFind(id.Trim(), out var tz) ? tz : Default;
        }

        /// <summary>True when <paramref name="id"/> resolves to a timezone on this machine.</summary>
        public static bool TryFind(string id, out TimeZoneInfo tz)
        {
            if (_cache.TryGetValue(id, out tz!)) return true;

            if (TryFindCore(id, out tz))
            {
                _cache[id] = tz;
                return true;
            }
            return false;
        }

        private static bool TryFindCore(string id, out TimeZoneInfo tz)
        {
            // .NET 6+ accepts IANA ids on Windows when ICU is available, and Windows ids on Linux.
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out tz!)) return true;

            if (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var windowsId)
                && TimeZoneInfo.TryFindSystemTimeZoneById(windowsId, out tz!)) return true;

            if (TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out var ianaId)
                && TimeZoneInfo.TryFindSystemTimeZoneById(ianaId, out tz!)) return true;

            tz = null!;
            return false;
        }

        private static TimeZoneInfo ResolveDefault()
        {
            if (TryFindCore(DefaultId, out var tz)) return tz;
            if (TimeZoneInfo.TryFindSystemTimeZoneById(DefaultWindowsId, out tz!)) return tz;
            if (TimeZoneInfo.TryFindSystemTimeZoneById("Asia/Bangkok", out tz!)) return tz;

            // Minimal containers without tzdata: Vietnam has no DST, a fixed +07:00 zone is exact.
            return TimeZoneInfo.CreateCustomTimeZone(DefaultId, TimeSpan.FromHours(7), "Asia/Ho_Chi_Minh", "Indochina Time");
        }
    }
}
