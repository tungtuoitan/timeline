using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Reflect
{
    /// <summary>One number the scan read out of a row, before it is stored.</summary>
    public class ReflectObservationDraft
    {
        public string MetricCode { get; set; } = "";
        public string Label { get; set; } = "";
        public string Unit { get; set; } = "";
        public string Aggregate { get; set; } = "sum";
        public DateTime OccurredOn { get; set; }
        public decimal Value { get; set; }
        public string Quote { get; set; } = "";
    }

    /// <summary>What the scan found in one row.</summary>
    public class ReflectScanOutcome
    {
        public string EntryId { get; set; } = "";
        public string Hash { get; set; } = "";
        public string? Tag { get; set; }
        public List<ReflectObservationDraft> Observations { get; set; } = new();
    }

    /// <summary>A row handed to the scan, with the day it was written on the user's own calendar.</summary>
    public class ReflectScanInput
    {
        public string Id { get; set; } = "";
        public string Date { get; set; } = "";
        public string Type { get; set; } = "";
        public string Think { get; set; } = "";
        public string Real { get; set; } = "";
    }

    public class ReflectSeriesPoint
    {
        [JsonPropertyName("date")] public string Date { get; set; } = "";
        [JsonPropertyName("value")] public decimal Value { get; set; }
    }

    /// <summary>One chart: a metric and its daily points, oldest first.</summary>
    public class ReflectSeries
    {
        [JsonPropertyName("code")] public string Code { get; set; } = "";
        [JsonPropertyName("label")] public string Label { get; set; } = "";
        [JsonPropertyName("unit")] public string Unit { get; set; } = "";
        [JsonPropertyName("aggregate")] public string Aggregate { get; set; } = "sum";
        [JsonPropertyName("points")] public List<ReflectSeriesPoint> Points { get; set; } = new();
    }

    public class ReflectScanResult
    {
        [JsonPropertyName("scanned")] public int Scanned { get; set; }
        [JsonPropertyName("failed")] public int Failed { get; set; }
    }
}
