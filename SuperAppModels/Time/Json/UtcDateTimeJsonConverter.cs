using System.Text.Json;
using System.Text.Json.Serialization;

namespace SuperAppModels.Time.Json
{
    /// <summary>
    /// System.Text.Json converter for instants (also used for <c>DateTime?</c>).
    ///   * Write: the value is a UTC instant (Kind Utc, or Unspecified = stored UTC); it is written in
    ///     the ambient user timezone (<see cref="UserClock.TimeZone"/>) as ISO 8601 with offset,
    ///     e.g. "2026-10-04T09:00:00.000+07:00".
    ///   * Read: the string must carry 'Z' or an offset; the result is UTC (Kind Utc).
    ///     A string without offset is rejected (ASP.NET Core turns the JsonException into 400).
    /// </summary>
    public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
    {
        private readonly Func<TimeZoneInfo> _timeZone;

        /// <summary>Uses the ambient request timezone (<see cref="UserClock.TimeZone"/>).</summary>
        public UtcDateTimeJsonConverter() : this(() => UserClock.TimeZone) { }

        /// <summary>Uses a custom timezone source (tests).</summary>
        public UtcDateTimeJsonConverter(Func<TimeZoneInfo> timeZone) => _timeZone = timeZone;

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException($"Expected an ISO 8601 date-time string with offset, got {reader.TokenType}.");

            var s = reader.GetString();
            if (TimeParsing.TryParseInstant(s, out var utc)) return utc;

            if (!string.IsNullOrWhiteSpace(s) && !TimeParsing.HasExplicitOffset(s))
                throw new JsonException(
                    $"Date-time '{s}' has no timezone offset. Send an ISO 8601 instant with 'Z' or an offset, " +
                    "e.g. \"2026-10-04T09:00:00+07:00\" or \"2026-10-04T02:00:00Z\".");

            throw new JsonException($"Invalid ISO 8601 date-time '{s}'.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            => writer.WriteStringValue(UserClock.ToIsoString(value, _timeZone()));
    }
}
