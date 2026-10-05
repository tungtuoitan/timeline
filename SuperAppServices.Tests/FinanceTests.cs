using SuperAppModels.DTOs.Requests.Finance;
using SuperAppModels.Models.Finance;
using SuperAppModels.Time;
using SuperAppServices.Services.Finance;
using Xunit;

namespace SuperAppServices.Tests
{
    /// <summary>TungRoot #1482 — multi-asset ledger: net worth, months, prices, validation.</summary>
    public class FinanceTests
    {
        private static readonly TimeZoneInfo Vn = TimeZones.FindOrDefault("Asia/Ho_Chi_Minh");
        private static DateOnly VnDate(DateTime utc) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, Vn));
        private static DateOnly D(string s) => DateOnly.Parse(s);

        /// <summary>Instant at <paramref name="hour"/>:00 Vietnam time on day <paramref name="day"/>.</summary>
        private static DateTime VnAt(string day, int hour = 12)
            => DateTime.SpecifyKind(D(day).ToDateTime(new TimeOnly(hour, 0)).AddHours(-7), DateTimeKind.Utc);

        private static int _id;
        private static FinTransaction Tx(string account, string day, string asset, decimal amount, string kind,
            decimal? valueVnd = null, string? category = null, int hour = 12)
            => new() { Id = ++_id, Account = account, OccurredAt = VnAt(day, hour), Asset = asset, Amount = amount, Kind = kind, ValueVnd = valueVnd, Category = category };

        private static FinPrice Px(string day, string asset, string quote, decimal price)
            => new() { Date = D(day), Asset = asset, Quote = quote, Price = price, Source = "test" };

        private static readonly FinanceCalculator.PriceBook Prices = new(new[]
        {
            Px("2026-09-28", "USDT", "VND", 26000),
            Px("2026-10-03", "USDT", "VND", 26500),
            Px("2026-10-01", "BTC", "USDT", 100000),
            Px("2026-10-04", "BTC", "USDT", 110000),
        });

        private static List<FinTransaction> Ledger() => new()
        {
            Tx("BIDV", "2026-09-30", "VND", 20_000_000, FinKinds.Adjust),
            // P2P: BIDV −5tr ↔ Binance +190 USDT (both invest; only the non-VND leg counts as "invested")
            Tx("BIDV", "2026-10-01", "VND", -5_000_000, FinKinds.Invest),
            Tx("Binance", "2026-10-01", "USDT", 190, FinKinds.Invest, valueVnd: 5_000_000),
            // trade USDT → BTC
            Tx("Binance", "2026-10-02", "USDT", -100, FinKinds.Trade),
            Tx("Binance", "2026-10-02", "BTC", 0.001m, FinKinds.Trade),
            Tx("BIDV", "2026-10-03", "VND", -200_000, FinKinds.Expense, category: "Ăn uống"),
            Tx("BIDV", "2026-10-03", "VND", -50_000, FinKinds.Expense),
            Tx("BIDV", "2026-10-04", "VND", -2_000_000, FinKinds.Debt),
            Tx("BIDV", "2026-10-05", "VND", 30_000_000, FinKinds.Income, category: "Lương"),
        };

        [Fact]
        public void PriceBook_CarriesForwardAndChainsThroughUsdt()
        {
            Assert.Null(Prices.Vnd("USDT", D("2026-09-27")));            // before the first price
            Assert.Equal(26000m, Prices.Vnd("USDT", D("2026-10-02")));    // carry-forward from 28/09
            Assert.Equal(26500m, Prices.Vnd("USDT", D("2026-10-03")));
            Assert.Equal(100000m * 26500m, Prices.Vnd("BTC", D("2026-10-03")));
            Assert.Equal(1m, Prices.Vnd("VND", D("2000-01-01")));
            Assert.Null(Prices.Vnd("ETH", D("2026-10-05")));
        }

        [Fact]
        public void Summary_NetWorthIncludesPricedHoldingsAndOutstandingDebt()
        {
            var s = FinanceCalculator.BuildSummary(Ledger(), Prices, D("2026-09-30"), D("2026-10-05"), "day", VnDate);

            Assert.Equal(6, s.Series.Count);
            Assert.Equal(20_000_000m, s.Series[0].TotalVnd);
            // 01/10: BIDV 15tr + 190 USDT × 26 000
            Assert.Equal(15_000_000m + 190 * 26000m, s.Series[1].TotalVnd);

            var last = s.Series[^1];
            Assert.Equal(42_750_000m, last.Accounts["BIDV"]);
            Assert.Equal(90 * 26500m + 0.001m * 110000m * 26500m, last.Accounts["Binance"]);
            Assert.Equal(2_000_000m, last.DebtVnd);                       // lent money still counts as ours
            Assert.Equal(42_750_000m + 5_300_000m + 2_000_000m, last.TotalVnd);
            Assert.Empty(s.MissingPrices);
        }

        [Fact]
        public void Summary_MonthsSeparateSpendingFromInvesting()
        {
            var s = FinanceCalculator.BuildSummary(Ledger(), Prices, D("2026-09-30"), D("2026-10-05"), "day", VnDate);

            Assert.Equal(new[] { D("2026-09-01"), D("2026-10-01") }, s.Months.Select(m => m.Month));
            var sep = s.Months[0];
            Assert.Equal(20_000_000m, sep.NetWorthEndVnd);
            var oct = s.Months[1];
            Assert.Equal(30_000_000m, oct.IncomeVnd);
            Assert.Equal(250_000m, oct.ExpenseVnd);                       // invest and debt are not spending
            Assert.Equal(5_000_000m, oct.InvestedVnd);
            Assert.Equal(1, oct.Uncategorized);
            Assert.Equal(s.Series[^1].TotalVnd, oct.NetWorthEndVnd);
            Assert.Equal(5_000_000m, s.InvestedVnd);
        }

        [Fact]
        public void Summary_RowsBeforeFromBuildOpeningBalanceButNotMonthTotals()
        {
            var s = FinanceCalculator.BuildSummary(Ledger(), Prices, D("2026-10-04"), D("2026-10-05"), "day", VnDate);

            Assert.Single(s.Months);
            Assert.Equal(30_000_000m, s.Months[0].IncomeVnd);
            Assert.Equal(0m, s.Months[0].ExpenseVnd);                     // 03/10 expenses are before from
            Assert.Equal(5_000_000m, s.InvestedVnd);                      // invested counts from the beginning
            Assert.Equal(42_750_000m + 5_300_000m + 2_000_000m, s.Series[^1].TotalVnd);
        }

        [Fact]
        public void Summary_WeekAndMonthIntervalsKeepPeriodEndsAndTo()
        {
            var week = FinanceCalculator.BuildSummary(Ledger(), Prices, D("2026-09-28"), D("2026-10-05"), "week", VnDate);
            Assert.Equal(new[] { D("2026-10-04"), D("2026-10-05") }, week.Series.Select(p => p.Date));

            var month = FinanceCalculator.BuildSummary(Ledger(), Prices, D("2026-09-28"), D("2026-10-05"), "month", VnDate);
            Assert.Equal(new[] { D("2026-09-30"), D("2026-10-05") }, month.Series.Select(p => p.Date));
        }

        [Fact]
        public void Summary_UsesUserDayNotUtcDay()
        {
            // 00:30 on 01/10 in Vietnam = 17:30 UTC on 30/09
            var ledger = new List<FinTransaction> { Tx("BIDV", "2026-10-01", "VND", 1_000_000, FinKinds.Income, hour: 0) };
            ledger[0].OccurredAt = ledger[0].OccurredAt.AddMinutes(30);
            var s = FinanceCalculator.BuildSummary(ledger, Prices, D("2026-09-30"), D("2026-10-01"), "day", VnDate);
            Assert.Equal(0m, s.Series[0].TotalVnd);
            Assert.Equal(1_000_000m, s.Series[1].TotalVnd);
        }

        [Fact]
        public void Summary_ReportsAssetsWithoutPrice()
        {
            var ledger = Ledger();
            ledger.Add(Tx("Binance", "2026-10-05", "XYZ", 5, FinKinds.Trade));
            var s = FinanceCalculator.BuildSummary(ledger, Prices, D("2026-10-05"), D("2026-10-05"), "day", VnDate);
            Assert.Equal(new[] { "XYZ" }, s.MissingPrices);
            var xyz = Assert.Single(s.Holdings, h => h.Asset == "XYZ");
            Assert.Null(xyz.ValueVnd);
            Assert.Equal(42_750_000m, s.Holdings[0].ValueVnd);            // largest first
        }

        [Fact]
        public void TryBuild_NormalisesAndValidates()
        {
            var ok = new FinTransactionInput
            {
                Account = " BIDV ", Asset = "vnd", Amount = -12_345.6m, Kind = "Expense", OccurredAt = VnAt("2026-10-05"),
                ExternalId = "  ", Category = " Ăn uống ", Source = null,
            };
            Assert.Null(FinanceService.TryBuild(ok, out var row));
            Assert.Equal("BIDV", row!.Account);
            Assert.Equal("VND", row.Asset);
            Assert.Equal("expense", row.Kind);
            Assert.Equal(FinSources.Manual, row.Source);
            Assert.Null(row.ExternalId);
            Assert.Equal("Ăn uống", row.Category);

            Assert.NotNull(FinanceService.TryBuild(new FinTransactionInput { Account = "BIDV", Asset = "VND", Amount = 0, Kind = "expense", OccurredAt = VnAt("2026-10-05") }, out _));
            Assert.NotNull(FinanceService.TryBuild(new FinTransactionInput { Account = "BIDV", Asset = "VND", Amount = 1, Kind = "spend", OccurredAt = VnAt("2026-10-05") }, out _));
            Assert.NotNull(FinanceService.TryBuild(new FinTransactionInput { Account = "BIDV", Asset = "V-ND", Amount = 1, Kind = "income", OccurredAt = VnAt("2026-10-05") }, out _));
            Assert.NotNull(FinanceService.TryBuild(new FinTransactionInput { Account = "", Asset = "VND", Amount = 1, Kind = "income", OccurredAt = VnAt("2026-10-05") }, out _));
            Assert.NotNull(FinanceService.TryBuild(new FinTransactionInput { Account = "BIDV", Asset = "VND", Amount = 1, Kind = "income", OccurredAt = VnAt("2026-10-05"), Source = "momo" }, out _));
        }
    }
}
