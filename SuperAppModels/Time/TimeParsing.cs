using System.Globalization;
using System.Text.RegularExpressions;

namespace SuperAppModels.Time
{
    /// <summary>
    /// String -> date/time parsing shared by the JSON converters and query-string parameters.
    /// Always culture-invariant and independent of the server's timezone.
    /// </summary>
    public static class TimeParsing
    {
        private static readonly Regex _plainDate = new(@"^\d{4}-\d{2}-\d{2}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Explicit UTC designator or numeric offset at the end of an ISO date-time.
        private static readonly Regex _hasOffset = new(@"(?:[zZ]|[+-]\d{2}(?::?\d{2})?)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>True if <paramref name="s"/> is a date-time ending with 'Z' or a numeric offset.</summary>
        public static bool HasExplicitOffset(string s)
        {
            s = s.Trim();
            // A plain date "2026-10-04" ends with "-04", which is not an offset.
            if (_plainDate.IsMatch(s)) return false;
            var timeStart = s.IndexOfAny(new[] { 'T', 't', ' ' });
            return timeStart > 0 && _hasOffset.IsMatch(s[timeStart..]);
        }

        /// <summary>
        /// Parses an ISO 8601 instant that MUST carry 'Z' or an offset. Returns UTC (Kind Utc).
        /// </summary>
        public static bool TryParseInstant(string? s, out DateTime utc)
        {
            utc = default;
            if (string.IsNullOrWhiteSpace(s) || !HasExplicitOffset(s)) return false;
            if (!DateTimeOffset.TryParse(s.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto)) return false;
            utc = dto.UtcDateTime;
            return true;
        }

        /// <summary>
        /// Lenient instant parsing for query strings (filters): with an offset -> that instant;
        /// without an offset (e.g. "2026-10-04" or "2026-10-04T08:00") -> wall-clock time in the
        /// current user's timezone. Returns UTC, or null when unparsable.
        /// </summary>
        public static DateTime? ParseInstantLenient(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (TryParseInstant(s, out var utc)) return utc;
            if (DateTime.TryParse(s.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
                return UserClock.FromUserLocal(DateTime.SpecifyKind(local, DateTimeKind.Unspecified));
            return null;
        }

        /// <summary>
        /// Calendar date parsing: "yyyy-MM-dd" (canonical). For backward compatibility also accepts a
        /// full ISO date-time: with an offset/'Z' the instant is converted to the user's timezone before
        /// taking the date; without an offset the date part is taken as written.
        /// </summary>
        public static bool TryParseDate(string? s, out DateOnly date)
        {
            date = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim();

            if (_plainDate.IsMatch(s))
                return DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

            if (TryParseInstant(s, out var utc))
            {
                date = UserClock.ToUserDate(utc);
                return true;
            }

            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
            {
                date = DateOnly.FromDateTime(local);
                return true;
            }
            return false;
        }

        /// <summary>Nullable convenience wrapper around <see cref="TryParseDate"/>.</summary>
        public static DateOnly? ParseDateOrNull(string? s) => TryParseDate(s, out var d) ? d : null;
    }
}
