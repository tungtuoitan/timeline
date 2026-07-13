using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Responses.DailyLog
{
    /// <summary>
    /// One data point for a single-field history query. Value is the raw JSON token
    /// pulled from daily_log.values_json (string, number, or boolean) as a string.
    /// </summary>
    public class DailyLogHistoryPoint
    {
        [JsonPropertyName("logDate")]
        public DateTime LogDate { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }
}
