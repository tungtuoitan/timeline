namespace SuperAppModels.Time
{
    /// <summary>
    /// Ambient "display timezone" of the current request user + day helpers.
    ///
    /// Convention (TungRoot #1450):
    ///   * Instants (created_at, changed_at, srs_next_review_at, ...) are stored and handled in UTC
    ///     (<see cref="DateTime.UtcNow"/>). They are converted to the user's timezone only for display
    ///     (JSON output) and for "which day is this?" logic.
    ///   * Calendar dates (task/project start/end, daily log date, birthday) are <see cref="DateOnly"/>
    ///     and never carry a timezone.
    ///
    /// The timezone is set per request by the API middleware (from urm.user_profiles.timezone)
    /// via <see cref="Use"/>; outside a request (background services, tests) it falls back to
    /// <see cref="TimeZones.Default"/> (Asia/Ho_Chi_Minh).
    /// </summary>
    public static class UserClock
    {
        private static readonly AsyncLocal<TimeZoneInfo?> _current = new();

        /// <summary>Timezone of the current request user (or the default).</summary>
        public static TimeZoneInfo TimeZone => _current.Value ?? TimeZones.Default;

        /// <summary>
        /// Sets the ambient timezone for the current async flow; dispose to restore the previous one.
        /// </summary>
        public static IDisposable Use(TimeZoneInfo timeZone)
        {
            ArgumentNullException.ThrowIfNull(timeZone);
            var previous = _current.Value;
            _current.Value = timeZone;
            return new Restore(previous);
        }

        /// <summary>
        /// Normalizes a stored instant to UTC: Kind Utc is kept, Kind Unspecified is assumed to be UTC
        /// (that is how EF/ADO hand back datetime2 values), Kind Local is converted.
        /// </summary>
        public static DateTime AsUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

        /// <summary>UTC instant -> wall-clock time in the current user's timezone (Kind Unspecified).</summary>
        public static DateTime ToUserLocal(DateTime utc) => ToLocal(utc, TimeZone);

        /// <summary>UTC instant -> wall-clock time in <paramref name="timeZone"/> (Kind Unspecified).</summary>
        public static DateTime ToLocal(DateTime utc, TimeZoneInfo timeZone)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(AsUtc(utc), timeZone);
            return DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        }

        /// <summary>ISO 8601 string with offset in the current user's timezone, e.g. "2026-10-04T09:00:00.000+07:00".</summary>
        public static string ToUserIsoString(DateTime utc) => ToIsoString(utc, TimeZone);

        /// <summary>ISO 8601 string with offset in <paramref name="timeZone"/> (same format as the JSON output).</summary>
        public static string ToIsoString(DateTime utc, TimeZoneInfo timeZone)
        {
            var u = AsUtc(utc);
            try
            {
                var offset = timeZone.GetUtcOffset(u);
                var local = DateTime.SpecifyKind(u.Add(offset), DateTimeKind.Unspecified);
                return new DateTimeOffset(local, offset).ToString(IsoFormat, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (ArgumentOutOfRangeException)
            {
                // DateTime.MinValue/MaxValue shifted out of range — fall back to plain UTC.
                return u.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        /// <summary>Output format for instants: milliseconds + numeric offset.</summary>
        public const string IsoFormat = "yyyy-MM-dd'T'HH:mm:ss.fffzzz";

        /// <summary>The user's calendar day containing the UTC instant <paramref name="utc"/>.</summary>
        public static DateOnly ToUserDate(DateTime utc) => DateOnly.FromDateTime(ToUserLocal(utc));

        /// <summary>Today in the current user's timezone.</summary>
        public static DateOnly UserToday() => UserToday(DateTime.UtcNow);

        /// <summary>The user's calendar day at the instant <paramref name="utcNow"/> (testable overload).</summary>
        public static DateOnly UserToday(DateTime utcNow) => ToUserDate(utcNow);

        /// <summary>
        /// UTC instant at which <paramref name="day"/> starts (00:00) in the current user's timezone.
        /// Use for range filters on instant columns: [StartOfUserDayUtc(d), StartOfUserDayUtc(d + 1)).
        /// </summary>
        public static DateTime StartOfUserDayUtc(DateOnly day) => StartOfDayUtc(day, TimeZone);

        /// <summary>UTC instant at which <paramref name="day"/> starts in <paramref name="timeZone"/>.</summary>
        public static DateTime StartOfDayUtc(DateOnly day, TimeZoneInfo timeZone)
        {
            var local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            // Midnight can fall inside a DST gap in some zones — move forward until valid.
            while (timeZone.IsInvalidTime(local)) local = local.AddMinutes(30);
            return TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
        }

        /// <summary>
        /// User wall-clock time (no offset) -> UTC instant. Used only for lenient query-string parsing.
        /// </summary>
        public static DateTime FromUserLocal(DateTime local)
        {
            if (local.Kind == DateTimeKind.Utc) return local;
            if (local.Kind == DateTimeKind.Local) return local.ToUniversalTime();
            var tz = TimeZone;
            while (tz.IsInvalidTime(local)) local = local.AddMinutes(30);
            return TimeZoneInfo.ConvertTimeToUtc(local, tz);
        }

        private sealed class Restore : IDisposable
        {
            private readonly TimeZoneInfo? _previous;
            private bool _disposed;

            public Restore(TimeZoneInfo? previous) => _previous = previous;

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _current.Value = _previous;
            }
        }
    }
}
