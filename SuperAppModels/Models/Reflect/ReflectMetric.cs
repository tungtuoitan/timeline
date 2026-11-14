using System.Text.Json.Serialization;

namespace SuperAppModels.Models.Reflect
{
    /// <summary>A kind of number the journal tracks (km run, hours slept...). Maps to pro.reflect_metric.</summary>
    public class ReflectMetric
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonIgnore]
        public int UserId { get; set; }

        /// <summary>Stable slug, e.g. run_km; the scan reuses an existing code before it makes a new one</summary>
        [JsonPropertyName("code")]
        public string Code { get; set; } = "";

        [JsonPropertyName("label")]
        public string Label { get; set; } = "";

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "";

        /// <summary>How several values of one day combine: sum, avg or max</summary>
        [JsonPropertyName("aggregate")]
        public string Aggregate { get; set; } = "sum";

        [JsonIgnore]
        public DateTime CreatedAt { get; set; }

        [JsonIgnore]
        public DateTime? DeletedAt { get; set; }
    }

    /// <summary>One number read out of one Reflect row for one day. Maps to pro.reflect_observation.</summary>
    public class ReflectObservation
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public string EntryId { get; set; } = "";
        public int MetricId { get; set; }
        public ReflectMetric? Metric { get; set; }
        public DateTime OccurredOn { get; set; }
        public decimal Value { get; set; }
        public string Quote { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
