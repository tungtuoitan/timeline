using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SuperAppDataRepositories.Data;
using SuperAppModels.Models;
using SuperAppModels.Models.DailyLog;
using SuperAppModels.Time;
using SuperAppModels.Time.Json;
using Xunit;

namespace SuperAppServices.Tests
{
    /// <summary>
    /// TungRoot #1450 — instants are UTC (JSON out with the user's offset, in must carry an offset),
    /// calendar dates are DateOnly ("yyyy-MM-dd"), day boundaries follow the user's timezone.
    /// </summary>
    public class TimeHandlingTests
    {
        private static readonly TimeZoneInfo Vn = TimeZones.FindOrDefault("Asia/Ho_Chi_Minh");

        private static JsonSerializerOptions Options(TimeZoneInfo? tz = null)
        {
            var o = new JsonSerializerOptions();
            o.Converters.Add(tz == null ? new UtcDateTimeJsonConverter() : new UtcDateTimeJsonConverter(() => tz));
            o.Converters.Add(new LenientDateOnlyJsonConverter());
            return o;
        }

        private sealed class InstantDto
        {
            public DateTime At { get; set; }
            public DateTime? MaybeAt { get; set; }
        }

        private sealed class DateDto
        {
            public DateOnly Day { get; set; }
            public DateOnly? MaybeDay { get; set; }
        }

        /// <summary>String value of a top-level JSON property, unescaped (the default encoder writes '+' as +).</summary>
        private static string? Str(string json, string property)
        {
            using var doc = JsonDocument.Parse(json);
            var el = doc.RootElement.GetProperty(property);
            return el.ValueKind == JsonValueKind.Null ? null : el.GetString();
        }

        private static DateTime Utc(int y, int mo, int d, int h = 0, int mi = 0, int s = 0)
            => new(y, mo, d, h, mi, s, DateTimeKind.Utc);

        // ── timezone resolution ──────────────────────────────────────────────────

        [Fact]
        public void DefaultTimeZone_IsPlus7()
        {
            Assert.Equal(TimeSpan.FromHours(7), TimeZones.Default.GetUtcOffset(Utc(2026, 10, 4)));
            Assert.Equal(TimeSpan.FromHours(7), Vn.GetUtcOffset(Utc(2026, 1, 1)));
        }

        [Fact]
        public void UnknownTimeZoneId_FallsBackToDefault()
        {
            Assert.Same(TimeZones.Default, TimeZones.FindOrDefault("Not/AZone"));
            Assert.Same(TimeZones.Default, TimeZones.FindOrDefault(null));
            Assert.Same(TimeZones.Default, TimeZones.FindOrDefault(""));
        }

        // ── DateTime converter: write ────────────────────────────────────────────

        [Fact]
        public void DateTimeConverter_WritesUserOffset_AsiaHoChiMinh()
        {
            var json = JsonSerializer.Serialize(new InstantDto { At = Utc(2026, 10, 4, 2, 0, 0) }, Options(Vn));
            Assert.Equal("2026-10-04T09:00:00.000+07:00", Str(json, "At"));
            Assert.Null(Str(json, "MaybeAt"));
        }

        [Fact]
        public void DateTimeConverter_TreatsUnspecifiedKindAsUtc()
        {
            var unspecified = new DateTime(2026, 10, 4, 17, 30, 0, DateTimeKind.Unspecified);
            var json = JsonSerializer.Serialize(new InstantDto { At = unspecified, MaybeAt = unspecified }, Options(Vn));
            Assert.Equal("2026-10-05T00:30:00.000+07:00", Str(json, "At"));
            Assert.Equal("2026-10-05T00:30:00.000+07:00", Str(json, "MaybeAt"));
        }

        [Fact]
        public void DateTimeConverter_UsesAmbientUserTimeZone()
        {
            var utc = TimeZones.FindOrDefault("UTC");
            using (UserClock.Use(utc))
            {
                var json = JsonSerializer.Serialize(new InstantDto { At = Utc(2026, 10, 4, 2, 0, 0) }, Options());
                Assert.Equal("2026-10-04T02:00:00.000+00:00", Str(json, "At"));
            }
            // Outside the scope: default Asia/Ho_Chi_Minh
            var json2 = JsonSerializer.Serialize(new InstantDto { At = Utc(2026, 10, 4, 2, 0, 0) }, Options());
            Assert.Equal("2026-10-04T09:00:00.000+07:00", Str(json2, "At"));
        }

        [Fact]
        public void DateTimeConverter_MinValue_DoesNotThrow()
        {
            var json = JsonSerializer.Serialize(new InstantDto { At = DateTime.MinValue, MaybeAt = DateTime.MaxValue }, Options(Vn));
            Assert.Equal("0001-01-01T07:00:00.000+07:00", Str(json, "At"));
            Assert.NotNull(Str(json, "MaybeAt"));
        }

        // ── DateTime converter: read ─────────────────────────────────────────────

        [Fact]
        public void DateTimeConverter_ReadsZAndOffset_ToSameUtc()
        {
            var z = JsonSerializer.Deserialize<InstantDto>("{\"At\":\"2026-10-04T02:00:00Z\"}", Options(Vn))!;
            var plus7 = JsonSerializer.Deserialize<InstantDto>("{\"At\":\"2026-10-04T09:00:00+07:00\",\"MaybeAt\":\"2026-10-04T09:00:00.000+07:00\"}", Options(Vn))!;

            Assert.Equal(Utc(2026, 10, 4, 2, 0, 0), z.At);
            Assert.Equal(DateTimeKind.Utc, z.At.Kind);
            Assert.Equal(z.At, plus7.At);
            Assert.Equal(DateTimeKind.Utc, plus7.At.Kind);
            Assert.Equal(z.At, plus7.MaybeAt);
            Assert.Equal(DateTimeKind.Utc, plus7.MaybeAt!.Value.Kind);
        }

        [Fact]
        public void DateTimeConverter_NullableNull_StaysNull()
        {
            var dto = JsonSerializer.Deserialize<InstantDto>("{\"At\":\"2026-10-04T02:00:00Z\",\"MaybeAt\":null}", Options(Vn))!;
            Assert.Null(dto.MaybeAt);
        }

        [Theory]
        [InlineData("2026-10-04T09:00:00")]
        [InlineData("2026-10-04T09:00:00.000")]
        [InlineData("2026-10-04")]
        public void DateTimeConverter_RejectsStringWithoutOffset(string value)
        {
            var ex = Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<InstantDto>($"{{\"At\":\"{value}\"}}", Options(Vn)));
            Assert.Contains("offset", ex.Message);

            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<InstantDto>($"{{\"At\":\"2026-10-04T02:00:00Z\",\"MaybeAt\":\"{value}\"}}", Options(Vn)));
        }

        [Fact]
        public void DateTimeConverter_RejectsGarbage()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<InstantDto>("{\"At\":\"not a date\"}", Options(Vn)));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<InstantDto>("{\"At\":123}", Options(Vn)));
        }

        [Fact]
        public void DateTimeConverter_RoundTrips()
        {
            var original = new InstantDto { At = Utc(2026, 10, 4, 16, 59, 59).AddMilliseconds(123) };
            var back = JsonSerializer.Deserialize<InstantDto>(JsonSerializer.Serialize(original, Options(Vn)), Options(Vn))!;
            Assert.Equal(original.At, back.At);
        }

        // ── DateOnly converter ───────────────────────────────────────────────────

        [Fact]
        public void DateOnlyConverter_ReadsPlainDate()
        {
            var dto = JsonSerializer.Deserialize<DateDto>("{\"Day\":\"2026-10-04\",\"MaybeDay\":\"2026-12-31\"}", Options(Vn))!;
            Assert.Equal(new DateOnly(2026, 10, 4), dto.Day);
            Assert.Equal(new DateOnly(2026, 12, 31), dto.MaybeDay);
        }

        [Fact]
        public void ParseInstantLenientEnd_PlainDay_IncludesWholeUserDay()
        {
            using (UserClock.Use(Vn))
            {
                // 2026-10-04 VN ends at 2026-10-04T17:00Z (exclusive)
                var end = TimeParsing.ParseInstantLenientEnd("2026-10-04")!.Value;
                Assert.Equal(DateTimeKind.Utc, end.Kind);
                Assert.Equal(new DateTime(2026, 10, 4, 17, 0, 0, DateTimeKind.Utc).AddTicks(-1), end);
            }
        }

        [Fact]
        public void ParseInstantLenientEnd_Month_IncludesWholeUserMonth()
        {
            using (UserClock.Use(Vn))
            {
                var end = TimeParsing.ParseInstantLenientEnd("2026-10")!.Value;
                Assert.Equal(new DateTime(2026, 10, 31, 17, 0, 0, DateTimeKind.Utc).AddTicks(-1), end);
            }
        }

        [Fact]
        public void ParseInstantLenientEnd_InstantWithOffset_IsExact()
        {
            var end = TimeParsing.ParseInstantLenientEnd("2026-10-04T08:00:00+07:00")!.Value;
            Assert.Equal(new DateTime(2026, 10, 4, 1, 0, 0, DateTimeKind.Utc), end);
            Assert.Null(TimeParsing.ParseInstantLenientEnd(""));
            Assert.Null(TimeParsing.ParseInstantLenientEnd("not a date"));
        }

        [Fact]
        public void DateOnlyConverter_InstantWithZ_UsesUserTimeZone()
        {
            using (UserClock.Use(Vn))
            {
                var dto = JsonSerializer.Deserialize<DateDto>("{\"Day\":\"2026-10-04T17:00:00Z\",\"MaybeDay\":\"2026-10-04T16:59:59.000Z\"}", Options(Vn))!;
                Assert.Equal(new DateOnly(2026, 10, 5), dto.Day);
                Assert.Equal(new DateOnly(2026, 10, 4), dto.MaybeDay);
            }
        }

        [Fact]
        public void DateOnlyConverter_InstantWithOffset_UsesUserTimeZone()
        {
            using (UserClock.Use(Vn))
            {
                var dto = JsonSerializer.Deserialize<DateDto>("{\"Day\":\"2026-10-05T00:00:00+07:00\"}", Options(Vn))!;
                Assert.Equal(new DateOnly(2026, 10, 5), dto.Day);
            }
        }

        [Fact]
        public void DateOnlyConverter_DateTimeWithoutOffset_TakesDatePart()
        {
            var dto = JsonSerializer.Deserialize<DateDto>("{\"Day\":\"2026-10-05T00:00:00\",\"MaybeDay\":\"2026-10-05T23:59:59.999\"}", Options(Vn))!;
            Assert.Equal(new DateOnly(2026, 10, 5), dto.Day);
            Assert.Equal(new DateOnly(2026, 10, 5), dto.MaybeDay);
        }

        [Fact]
        public void DateOnlyConverter_WritesIsoDate_AndNull()
        {
            var json = JsonSerializer.Serialize(new DateDto { Day = new DateOnly(2026, 10, 5) }, Options(Vn));
            Assert.Equal("2026-10-05", Str(json, "Day"));
            Assert.Null(Str(json, "MaybeDay"));
        }

        [Fact]
        public void DateOnlyConverter_RejectsGarbage()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateDto>("{\"Day\":\"05/10/2026x\"}", Options(Vn)));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateDto>("{\"Day\":20261005}", Options(Vn)));
        }

        // ── UserClock: midnight boundaries ───────────────────────────────────────

        [Fact]
        public void UserToday_SwitchesAtUserMidnight_NotUtcMidnight()
        {
            using (UserClock.Use(Vn))
            {
                // 23:59:59 in Vietnam
                Assert.Equal(new DateOnly(2026, 10, 4), UserClock.UserToday(Utc(2026, 10, 4, 16, 59, 59)));
                // 00:00:00 in Vietnam (still 2026-10-04 in UTC)
                Assert.Equal(new DateOnly(2026, 10, 5), UserClock.UserToday(Utc(2026, 10, 4, 17, 0, 0)));
                // 06:59 in Vietnam = 23:59 UTC of the previous day
                Assert.Equal(new DateOnly(2026, 10, 5), UserClock.UserToday(Utc(2026, 10, 4, 23, 59, 0)));
            }
        }

        [Fact]
        public void ToUserLocal_ConvertsAcrossMidnight()
        {
            using (UserClock.Use(Vn))
            {
                var local = UserClock.ToUserLocal(Utc(2026, 10, 4, 17, 0, 0));
                Assert.Equal(new DateTime(2026, 10, 5, 0, 0, 0), local);
                Assert.Equal(DateTimeKind.Unspecified, local.Kind);

                Assert.Equal(new DateTime(2026, 10, 4, 23, 59, 59), UserClock.ToUserLocal(Utc(2026, 10, 4, 16, 59, 59)));
                Assert.Equal(new DateOnly(2026, 10, 5), UserClock.ToUserDate(Utc(2026, 10, 4, 17, 0, 0)));
            }
        }

        [Fact]
        public void StartOfUserDayUtc_IsUserMidnightInUtc()
        {
            using (UserClock.Use(Vn))
            {
                var start = UserClock.StartOfUserDayUtc(new DateOnly(2026, 10, 5));
                Assert.Equal(Utc(2026, 10, 4, 17, 0, 0), start);
                Assert.Equal(DateTimeKind.Utc, start.Kind);
            }
        }

        [Fact]
        public void UserClock_DefaultsToVietnam_OutsideRequest()
        {
            Assert.Equal(TimeSpan.FromHours(7), UserClock.TimeZone.GetUtcOffset(Utc(2026, 10, 4)));
        }

        [Fact]
        public void UserClock_Use_RestoresPrevious()
        {
            var utc = TimeZones.FindOrDefault("UTC");
            using (UserClock.Use(Vn))
            {
                using (UserClock.Use(utc))
                    Assert.Equal(new DateOnly(2026, 10, 4), UserClock.UserToday(Utc(2026, 10, 4, 17, 0, 0)));
                Assert.Equal(new DateOnly(2026, 10, 5), UserClock.UserToday(Utc(2026, 10, 4, 17, 0, 0)));
            }
        }

        // ── query-string parsing ─────────────────────────────────────────────────

        [Fact]
        public void ParseInstantLenient_OffsetOrUserLocal()
        {
            using (UserClock.Use(Vn))
            {
                Assert.Equal(Utc(2026, 10, 4, 2, 0, 0), TimeParsing.ParseInstantLenient("2026-10-04T02:00:00Z"));
                Assert.Equal(Utc(2026, 10, 4, 2, 0, 0), TimeParsing.ParseInstantLenient("2026-10-04T09:00:00+07:00"));
                // No offset -> user local wall time
                Assert.Equal(Utc(2026, 10, 3, 17, 0, 0), TimeParsing.ParseInstantLenient("2026-10-04"));
                Assert.Null(TimeParsing.ParseInstantLenient("garbage"));
                Assert.Null(TimeParsing.ParseInstantLenient(null));
            }
        }

        [Fact]
        public void ParseDateOrNull_HandlesQueryFormats()
        {
            using (UserClock.Use(Vn))
            {
                Assert.Equal(new DateOnly(2026, 10, 4), TimeParsing.ParseDateOrNull("2026-10-04"));
                Assert.Equal(new DateOnly(2026, 10, 5), TimeParsing.ParseDateOrNull("2026-10-04T17:00:00.000Z"));
                Assert.Null(TimeParsing.ParseDateOrNull("nope"));
                Assert.Null(TimeParsing.ParseDateOrNull(""));
            }
        }

        // ── EF model: UTC convention + DateOnly mapping (model only, no DB connection) ──

        private static ApplicationDbContext NewContext() => new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=model-only;Database=none;Trusted_Connection=True;TrustServerCertificate=True")
                .Options);

        [Fact]
        public void EfModel_EveryDateTimeProperty_HasUtcConverter()
        {
            using var ctx = NewContext();
            var dateTimeProps = ctx.Model.GetEntityTypes()
                .SelectMany(e => e.GetProperties())
                .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?))
                .ToList();

            Assert.NotEmpty(dateTimeProps);
            Assert.All(dateTimeProps, p => Assert.IsType<UtcDateTimeConverter>(p.GetValueConverter()));

            var converter = new UtcDateTimeConverter();
            var read = (DateTime)converter.ConvertFromProvider(new DateTime(2026, 10, 4, 2, 0, 0, DateTimeKind.Unspecified))!;
            Assert.Equal(DateTimeKind.Utc, read.Kind);
            var local = new DateTime(2026, 10, 4, 9, 0, 0, DateTimeKind.Local);
            Assert.Equal(local.ToUniversalTime(), (DateTime)converter.ConvertToProvider(local)!);
        }

        [Fact]
        public void EfModel_CalendarDates_AreDateOnlyMappedToSqlDate()
        {
            using var ctx = NewContext();
            void AssertDate<T>(string property)
            {
                var p = ctx.Model.FindEntityType(typeof(T))!.FindProperty(property)!;
                Assert.Equal(typeof(DateOnly?), p.ClrType);
                Assert.Equal("date", p.GetColumnType());
            }
            AssertDate<Project>(nameof(Project.StartDate));
            AssertDate<Project>(nameof(Project.EndDate));
            AssertDate<ProTask>(nameof(ProTask.StartDate));
            AssertDate<ProTask>(nameof(ProTask.EndDate));
            AssertDate<UserProfile>(nameof(UserProfile.DateOfBirth));
            Assert.Equal(typeof(DateOnly), ctx.Model.FindEntityType(typeof(DailyLog))!.FindProperty(nameof(DailyLog.LogDate))!.ClrType);

            var tz = ctx.Model.FindEntityType(typeof(UserProfile))!.FindProperty(nameof(UserProfile.Timezone))!;
            Assert.Equal("timezone", tz.GetColumnName());
            Assert.Equal(64, tz.GetMaxLength());
        }
    }
}
