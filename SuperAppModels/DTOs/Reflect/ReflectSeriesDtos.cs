using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Reflect
{
    /// <summary>One thing read out of a Reflect row for one day. Only what applies is filled: a number, a text, an item.</summary>
    public class ReflectSeriesPoint
    {
        [JsonPropertyName("date")] public string Date { get; set; } = "";
        [JsonPropertyName("value")] public decimal? Value { get; set; }
        [JsonPropertyName("text")] public string? Text { get; set; }
        /// <summary>Sub-key inside a metric, e.g. which suggestion sentence; empty when the metric has no items</summary>
        [JsonPropertyName("item")] public string Item { get; set; } = "";
        /// <summary>The words the point was read from</summary>
        [JsonPropertyName("quote")] public string Quote { get; set; } = "";
    }

    /// <summary>One metric with all its points, oldest first. The page decides how to draw it from `shape`.</summary>
    public class ReflectSeries
    {
        [JsonPropertyName("code")] public string Code { get; set; } = "";
        [JsonPropertyName("label")] public string Label { get; set; } = "";
        [JsonPropertyName("unit")] public string Unit { get; set; } = "";
        /// <summary>How several values of one day combine: sum, avg or max</summary>
        [JsonPropertyName("aggregate")] public string Aggregate { get; set; } = "sum";
        /// <summary>Drawing hint: auto, flag, count, level, items, range or list</summary>
        [JsonPropertyName("shape")] public string Shape { get; set; } = "auto";
        /// <summary>Hidden behind privacy mode on the page</summary>
        [JsonPropertyName("sensitive")] public bool Sensitive { get; set; }
        [JsonPropertyName("points")] public List<ReflectSeriesPoint> Points { get; set; } = new();
    }
}
