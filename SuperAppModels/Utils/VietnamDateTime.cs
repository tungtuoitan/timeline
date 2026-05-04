namespace SuperAppModels.Utils
{
    /// <summary>
    /// Date/time helper for the Fake-UTC convention used throughout SuperApp.
    ///
    /// Convention: all datetimes stored in DB represent Vietnam local time (GMT+7).
    /// No timezone conversion happens at any layer — FE sends local time formatted as UTC,
    /// BE stores it as-is, and returns it unchanged.
    ///
    /// Always use VietnamDateTime.Now() instead of DateTime.Now or DateTime.UtcNow
    /// when generating server-side timestamps, so the value is correct regardless of
    /// the server OS timezone setting.
    /// </summary>
    public static class VietnamDateTime
    {
        private static readonly TimeZoneInfo _tz = GetVietnamTz();

        /// <summary>Returns the current date and time in Vietnam (GMT+7).</summary>
        public static DateTime Now() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz);

        /// <summary>Vietnam timezone (GMT+7). Exposed for callers that need the TimeZoneInfo directly.</summary>
        public static TimeZoneInfo TimeZone => _tz;

        // Try IDs in order: Windows ID (dev), IANA IDs (Ubuntu/Linux prod).
        // "Asia/Ho_Chi_Minh" requires tzdata which is installed by default on Ubuntu.
        // Throws with a clear message if none are found (e.g. minimal Alpine without tzdata).
        private static TimeZoneInfo GetVietnamTz()
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById("SE Asia Standard Time", out var tz)) return tz;
            if (TimeZoneInfo.TryFindSystemTimeZoneById("Asia/Ho_Chi_Minh",      out tz))     return tz;
            if (TimeZoneInfo.TryFindSystemTimeZoneById("Asia/Bangkok",          out tz))     return tz;
            throw new InvalidOperationException(
                "Cannot find Vietnam (GMT+7) timezone. " +
                "On Ubuntu: ensure tzdata is installed (apt install tzdata). " +
                "On Alpine: add tzdata to the Dockerfile.");
        }
    }
}
