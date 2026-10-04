using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SuperAppModels.Time.Json
{
    /// <summary>
    /// System.Text.Json converter for calendar dates (also used for <c>DateOnly?</c>).
    ///   * Write: "yyyy-MM-dd".
    ///   * Read: "yyyy-MM-dd"; for backward compatibility with older frontends also a full ISO
    ///     date-time — with 'Z'/offset the instant is converted to the user's timezone before taking
    ///     the date ("2026-10-04T17:00:00Z" -> 2026-10-05 in Asia/Ho_Chi_Minh), without offset the
    ///     date part is taken as written.
    /// </summary>
    public sealed class LenientDateOnlyJsonConverter : JsonConverter<DateOnly>
    {
        public const string Format = "yyyy-MM-dd";

        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException($"Expected a date string \"yyyy-MM-dd\", got {reader.TokenType}.");

            var s = reader.GetString();
            if (TimeParsing.TryParseDate(s, out var date)) return date;

            throw new JsonException($"Invalid date '{s}'. Expected \"yyyy-MM-dd\".");
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}
