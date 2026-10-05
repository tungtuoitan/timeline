using SuperAppModels.DTOs.Responses.Dashboard;
using SuperAppModels.Models;

namespace SuperAppServices.Services.Dashboard
{
    /// <summary>
    /// Pure helpers for the progress dashboard (TungRoot #1481): query parsing, week bucketing,
    /// aggregation. No DB, no clock — the caller passes the "instant → user day" function.
    /// </summary>
    public static class DashboardBuckets
    {
        /// <summary>Longest range one request may cover.</summary>
        public const int MaxRangeDays = 3 * 366;

        public static readonly IReadOnlyList<string> DefaultActivityTypes =
            new[] { TaskCommentTypes.Devlog, TaskCommentTypes.Comment, TaskCommentTypes.Decision };

        public static readonly IReadOnlyList<string> HabitEntryTypes =
            new[] { TaskCommentTypes.Track, TaskCommentTypes.Comment };

        /// <summary>Monday of the ISO week containing <paramref name="day"/>.</summary>
        public static DateOnly WeekStart(DateOnly day)
            => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));

        /// <summary>"1,2, 3" → [1,2,3]; null/blank → null. Throws FormatException on a non-integer token.</summary>
        public static List<int>? ParseIds(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return null;
            var ids = new List<int>();
            foreach (var token in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!int.TryParse(token, out var id) || id <= 0)
                    throw new FormatException($"Invalid id '{token}'");
                ids.Add(id);
            }
            return ids.Count > 0 ? ids.Distinct().ToList() : null;
        }

        /// <summary>"devlog,comment" → validated list; null/blank → defaults. Throws FormatException on an unknown type.</summary>
        public static List<string> ParseTypes(string? csv, IReadOnlyList<string> defaults)
        {
            if (string.IsNullOrWhiteSpace(csv)) return defaults.ToList();
            var types = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.ToLowerInvariant())
                .Distinct()
                .ToList();
            var unknown = types.FirstOrDefault(t => !TaskCommentTypes.All.Contains(t));
            if (unknown != null) throw new FormatException($"Unknown comment type '{unknown}'");
            return types;
        }

        /// <summary>
        /// Fill missing bounds (to = today, from = to - defaultDays + 1) and validate.
        /// Throws ArgumentException when from &gt; to or the range is longer than <see cref="MaxRangeDays"/>.
        /// </summary>
        public static (DateOnly From, DateOnly To) ResolveRange(DateOnly? from, DateOnly? to, DateOnly today, int defaultDays)
        {
            var end = to ?? today;
            var start = from ?? end.AddDays(-(defaultDays - 1));
            if (start > end) throw new ArgumentException("from must be on or before to");
            if (end.DayNumber - start.DayNumber + 1 > MaxRangeDays)
                throw new ArgumentException($"Range is limited to {MaxRangeDays} days");
            return (start, end);
        }

        /// <summary>Count comments per (week, project). Projects not in <paramref name="projects"/> are skipped.</summary>
        public static List<ActivityWeekPoint> BuildActivity(
            IEnumerable<DashboardCommentRow> rows,
            IEnumerable<DashboardProjectRow> projects,
            Func<DateTime, DateOnly> toUserDate)
        {
            var projectById = projects.ToDictionary(p => p.ProjectId);
            return rows
                .Where(r => projectById.ContainsKey(r.ProjectId))
                .GroupBy(r => (Week: WeekStart(toUserDate(r.At)), r.ProjectId))
                .Select(g => new ActivityWeekPoint
                {
                    WeekStart = g.Key.Week,
                    ProjectId = g.Key.ProjectId,
                    ProjectName = projectById[g.Key.ProjectId].Name,
                    ProjectStatus = projectById[g.Key.ProjectId].Status,
                    Count = g.Count()
                })
                .OrderBy(p => p.WeekStart)
                .ThenBy(p => p.ProjectId)
                .ToList();
        }

        /// <summary>One series per task (same order as <paramref name="tasks"/>), entries sorted by day.</summary>
        public static List<HabitSeries> BuildHabits(
            IEnumerable<DashboardTaskRow> tasks,
            IEnumerable<DashboardCommentRow> comments,
            Func<DateTime, DateOnly> toUserDate)
        {
            var byTask = comments
                .GroupBy(c => c.TaskId)
                .ToDictionary(g => g.Key, g => g.OrderBy(c => c.At).ToList());

            return tasks.Select(t => new HabitSeries
            {
                TaskId = t.TaskId,
                Title = t.Title,
                ProjectId = t.ProjectId,
                Status = t.Status,
                Entries = byTask.TryGetValue(t.TaskId, out var list)
                    ? list.Select(c => new HabitEntry
                    {
                        CommentId = c.CommentId,
                        Date = toUserDate(c.At),
                        Type = c.Type,
                        Content = c.Content
                    }).ToList()
                    : new List<HabitEntry>()
            }).ToList();
        }
    }
}
