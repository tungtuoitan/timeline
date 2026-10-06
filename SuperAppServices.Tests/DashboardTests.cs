using SuperAppModels.DTOs.Responses.Dashboard;
using SuperAppModels.Time;
using SuperAppServices.Services.Dashboard;
using Xunit;

namespace SuperAppServices.Tests
{
    /// <summary>TungRoot #1481 — homepage progress dashboard: bucketing, parsing.</summary>
    public class DashboardTests
    {
        private static readonly TimeZoneInfo Vn = TimeZones.FindOrDefault("Asia/Ho_Chi_Minh");
        private static DateOnly VnDate(DateTime utc) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, Vn));
        private static DateTime Utc(int y, int m, int d, int h = 0, int min = 0) => new(y, m, d, h, min, 0, DateTimeKind.Utc);

        [Theory]
        [InlineData("2026-10-05", "2026-10-05")] // Monday
        [InlineData("2026-10-04", "2026-09-28")] // Sunday belongs to the week that started Monday 28/09
        [InlineData("2026-10-01", "2026-09-28")]
        [InlineData("2026-03-23", "2026-03-23")]
        public void WeekStart_IsMonday(string day, string expected)
            => Assert.Equal(DateOnly.Parse(expected), DashboardBuckets.WeekStart(DateOnly.Parse(day)));

        [Fact]
        public void ParseIds_HandlesBlankSpacesAndDuplicates()
        {
            Assert.Null(DashboardBuckets.ParseIds(null));
            Assert.Null(DashboardBuckets.ParseIds("  "));
            Assert.Equal(new[] { 1458, 1460 }, DashboardBuckets.ParseIds("1458, 1460,1458"));
            Assert.Throws<FormatException>(() => DashboardBuckets.ParseIds("1458,abc"));
            Assert.Throws<FormatException>(() => DashboardBuckets.ParseIds("-1"));
        }

        [Fact]
        public void ParseTypes_DefaultsAndValidation()
        {
            Assert.Equal(DashboardBuckets.DefaultActivityTypes, DashboardBuckets.ParseTypes(null, DashboardBuckets.DefaultActivityTypes));
            Assert.Equal(new[] { "devlog", "track" }, DashboardBuckets.ParseTypes("Devlog, track", DashboardBuckets.DefaultActivityTypes));
            Assert.Throws<FormatException>(() => DashboardBuckets.ParseTypes("devlog,foo", DashboardBuckets.DefaultActivityTypes));
        }

        [Fact]
        public void ResolveRange_DefaultsAndLimits()
        {
            var today = new DateOnly(2026, 10, 5);
            Assert.Equal((new DateOnly(2026, 8, 11), today), DashboardBuckets.ResolveRange(null, null, today, 56));
            Assert.Equal((new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30)),
                DashboardBuckets.ResolveRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), today, 56));
            Assert.Throws<ArgumentException>(() => DashboardBuckets.ResolveRange(today, today.AddDays(-1), today, 56));
            Assert.Throws<ArgumentException>(() => DashboardBuckets.ResolveRange(today.AddYears(-4), today, today, 56));
        }

        [Fact]
        public void BuildActivity_GroupsByUserWeekAndProject()
        {
            var projects = new[]
            {
                new DashboardProjectRow { ProjectId = 11, Name = "SuperApp", Status = "active" },
                new DashboardProjectRow { ProjectId = 23, Name = "TungRoot", Status = "completed" }
            };
            var rows = new[]
            {
                // Sunday 04/10 23:30 VN = 16:30 UTC → week of 28/09
                new DashboardCommentRow { ProjectId = 11, At = Utc(2026, 10, 4, 16, 30) },
                // Sunday 04/10 17:30 UTC = Monday 05/10 00:30 VN → week of 05/10
                new DashboardCommentRow { ProjectId = 11, At = Utc(2026, 10, 4, 17, 30) },
                new DashboardCommentRow { ProjectId = 23, At = Utc(2026, 9, 30, 3) },
                new DashboardCommentRow { ProjectId = 23, At = Utc(2026, 10, 1, 3) },
                // Unknown (deleted / foreign) project → skipped
                new DashboardCommentRow { ProjectId = 99, At = Utc(2026, 10, 1, 3) }
            };

            var points = DashboardBuckets.BuildActivity(rows, projects, VnDate);

            Assert.Equal(3, points.Count);
            Assert.Equal((new DateOnly(2026, 9, 28), 11, 1), (points[0].WeekStart, points[0].ProjectId, points[0].Count));
            Assert.Equal((new DateOnly(2026, 9, 28), 23, 2), (points[1].WeekStart, points[1].ProjectId, points[1].Count));
            Assert.Equal((new DateOnly(2026, 10, 5), 11, 1), (points[2].WeekStart, points[2].ProjectId, points[2].Count));
            Assert.Equal("TungRoot", points[1].ProjectName);
        }

        [Fact]
        public void BuildHabits_KeepsTaskOrderAndUsesUserDay()
        {
            var tasks = new[]
            {
                new DashboardTaskRow { TaskId = 1460, Title = "Chạy bộ", ProjectId = 15, Status = "background_progress" },
                new DashboardTaskRow { TaskId = 1458, Title = "Ăn đúng giờ", ProjectId = 15, Status = "background_progress" }
            };
            var comments = new[]
            {
                new DashboardCommentRow { CommentId = 2, TaskId = 1460, Type = "track", Content = "b", At = Utc(2026, 10, 1, 23) }, // 02/10 06:00 VN
                new DashboardCommentRow { CommentId = 1, TaskId = 1460, Type = "track", Content = "a", At = Utc(2026, 9, 28, 1) }
            };

            var series = DashboardBuckets.BuildHabits(tasks, comments, VnDate);

            Assert.Equal(new[] { 1460, 1458 }, series.Select(s => s.TaskId));
            Assert.Equal(new[] { new DateOnly(2026, 9, 28), new DateOnly(2026, 10, 2) }, series[0].Entries.Select(e => e.Date));
            Assert.Equal(Utc(2026, 10, 1, 23), series[0].Entries[1].At); // instant kept for time-of-day
            Assert.Empty(series[1].Entries);
        }
    }
}
