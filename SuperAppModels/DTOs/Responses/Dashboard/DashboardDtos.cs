using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Responses.Dashboard
{
    /// <summary>
    /// Activity of one project in one week (TungRoot #1481): number of task comments
    /// (devlog/comment/decision by default) whose day — occurredAt ?? createdAt, in the user's timezone —
    /// falls in the week starting <see cref="WeekStart"/> (Monday).
    /// </summary>
    public class ActivityWeekPoint
    {
        [JsonPropertyName("weekStart")]
        public DateOnly WeekStart { get; set; }

        [JsonPropertyName("projectId")]
        public int ProjectId { get; set; }

        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("projectStatus")]
        public string ProjectStatus { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    /// <summary>One habit tracker (a task, usually type=repeat) with its entries in the range.</summary>
    public class HabitSeries
    {
        [JsonPropertyName("taskId")]
        public int TaskId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("projectId")]
        public int ProjectId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("entries")]
        public List<HabitEntry> Entries { get; set; } = new();
    }

    /// <summary>One comment of a tracker: "track" = 1 time done, "comment" = note (missed day, weekly summary...).</summary>
    public class HabitEntry
    {
        [JsonPropertyName("commentId")]
        public int CommentId { get; set; }

        /// <summary>User-timezone calendar day of occurredAt ?? createdAt.</summary>
        [JsonPropertyName("date")]
        public DateOnly Date { get; set; }

        /// <summary>The instant itself (occurredAt ?? createdAt) — time of day matters for check-ins (#1481).</summary>
        [JsonPropertyName("at")]
        public DateTime At { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>Raw comment row used to build activity/habit aggregates (repository → service).</summary>
    public class DashboardCommentRow
    {
        public int CommentId { get; set; }
        public int TaskId { get; set; }
        public int ProjectId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime At { get; set; }
    }

    /// <summary>Raw tracker task row (repository → service).</summary>
    public class DashboardTaskRow
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>Raw project row (repository → service).</summary>
    public class DashboardProjectRow
    {
        public int ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
