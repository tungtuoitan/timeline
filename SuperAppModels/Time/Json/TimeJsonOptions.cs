using System.Text.Json;

namespace SuperAppModels.Time.Json
{
    public static class TimeJsonOptions
    {
        /// <summary>
        /// Registers the instant (<see cref="DateTime"/>/<c>DateTime?</c>) and calendar date
        /// (<see cref="DateOnly"/>/<c>DateOnly?</c>) converters. Nullable types are handled by
        /// System.Text.Json's nullable wrapper (null stays null).
        /// </summary>
        public static JsonSerializerOptions AddTimeConverters(this JsonSerializerOptions options)
        {
            options.Converters.Add(new UtcDateTimeJsonConverter());
            options.Converters.Add(new LenientDateOnlyJsonConverter());
            return options;
        }
    }
}
