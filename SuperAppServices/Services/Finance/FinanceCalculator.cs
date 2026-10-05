using SuperAppModels.DTOs.Responses.Finance;
using SuperAppModels.Models.Finance;

namespace SuperAppServices.Services.Finance
{
    /// <summary>
    /// Pure finance math for GET /api/finance/summary (TungRoot #1482) — no DB, no clock.
    ///
    /// Model: the ledger is multi-asset; holding of (account, asset) on day d = Σ amount of rows dated ≤ d.
    /// Value in VND = holding × price, price = latest cached close ≤ d (carry-forward over weekends/gaps):
    /// direct (asset, VND) if cached, else (asset, USDT) × (USDT, VND). VND itself = 1, USDT/USDT = 1.
    /// Debt rows (lend/borrow/hold) move cash but not net worth: outstanding = −Σ debt values.
    /// </summary>
    public static class FinanceCalculator
    {
        public const string Vnd = "VND";
        public const string Usdt = "USDT";
        public static readonly string[] Intervals = { "day", "week", "month" };
        private const decimal DustQuantity = 0.000000001m;

        /// <summary>Price lookup with carry-forward. Prices outside the summary range are fine (only ≤ d is used).</summary>
        public sealed class PriceBook
        {
            private readonly Dictionary<(string Asset, string Quote), (DateOnly[] Dates, decimal[] Prices)> _series;

            public PriceBook(IEnumerable<FinPrice> prices)
            {
                _series = prices
                    .GroupBy(p => (p.Asset, p.Quote))
                    .ToDictionary(g => g.Key, g =>
                    {
                        var ordered = g.OrderBy(p => p.Date).ToArray();
                        return (ordered.Select(p => p.Date).ToArray(), ordered.Select(p => p.Price).ToArray());
                    });
            }

            /// <summary>Latest price of asset in quote on or before <paramref name="day"/>; null when none.</summary>
            public decimal? Latest(string asset, string quote, DateOnly day)
            {
                if (asset == quote) return 1m;
                if (!_series.TryGetValue((asset, quote), out var s)) return null;
                var i = Array.BinarySearch(s.Dates, day);
                if (i < 0) i = ~i - 1; // index of the last date < day
                return i >= 0 ? s.Prices[i] : null;
            }

            /// <summary>VND per 1 unit of <paramref name="asset"/> on <paramref name="day"/>; null when unknown.</summary>
            public decimal? Vnd(string asset, DateOnly day)
            {
                if (asset == FinanceCalculator.Vnd) return 1m;
                var direct = Latest(asset, FinanceCalculator.Vnd, day);
                if (direct != null) return direct;
                var inUsdt = Latest(asset, Usdt, day);
                var usdtVnd = Latest(Usdt, FinanceCalculator.Vnd, day);
                return inUsdt != null && usdtVnd != null ? inUsdt * usdtVnd : null;
            }
        }

        /// <summary>True when <paramref name="day"/> closes a period of <paramref name="interval"/> (week = Mon–Sun).</summary>
        public static bool IsPeriodEnd(DateOnly day, string interval) => interval switch
        {
            "week" => day.DayOfWeek == DayOfWeek.Sunday,
            "month" => day.AddDays(1).Day == 1,
            _ => true,
        };

        /// <param name="ledger">All rows of the user up to the end of <paramref name="to"/>, any order.</param>
        /// <param name="toUserDate">UTC instant → user's calendar day.</param>
        public static FinSummaryDto BuildSummary(IReadOnlyList<FinTransaction> ledger, PriceBook prices,
            DateOnly from, DateOnly to, string interval, Func<DateTime, DateOnly> toUserDate)
        {
            var summary = new FinSummaryDto { From = from, To = to, Interval = interval };
            var missing = new SortedSet<string>(StringComparer.Ordinal);

            var rows = ledger
                .Select(t => (Tx: t, Day: toUserDate(t.OccurredAt)))
                .Where(r => r.Day <= to)
                .OrderBy(r => r.Day).ThenBy(r => r.Tx.OccurredAt).ThenBy(r => r.Tx.Id)
                .ToList();

            decimal TxValue(FinTransaction t, DateOnly day)
            {
                if (t.ValueVnd is decimal v) return v;
                var px = prices.Vnd(t.Asset, day);
                if (px == null) { missing.Add(t.Asset); return 0m; }
                return t.Amount * px.Value;
            }

            var holdings = new Dictionary<(string Account, string Asset), decimal>();
            decimal debtVnd = 0m, investedVnd = 0m;
            var months = new SortedDictionary<DateOnly, FinMonthDto>();

            FinMonthDto Month(DateOnly day)
            {
                var key = new DateOnly(day.Year, day.Month, 1);
                if (!months.TryGetValue(key, out var m)) months[key] = m = new FinMonthDto { Month = key };
                return m;
            }

            void Apply(FinTransaction t, DateOnly day, bool inRange)
            {
                var key = (t.Account, t.Asset);
                holdings[key] = holdings.GetValueOrDefault(key) + t.Amount;

                var isInvestLeg = t.Kind == FinKinds.Invest && t.Asset != Vnd;
                if (t.Kind == FinKinds.Debt || isInvestLeg || (inRange && (t.Kind == FinKinds.Income || t.Kind == FinKinds.Expense)))
                {
                    var value = TxValue(t, day);
                    if (t.Kind == FinKinds.Debt) debtVnd -= value;
                    if (isInvestLeg) investedVnd += value;
                    if (!inRange) return;
                    var m = Month(day);
                    if (t.Kind == FinKinds.Income) m.IncomeVnd += value;
                    if (t.Kind == FinKinds.Expense) m.ExpenseVnd -= value;
                    if (isInvestLeg) m.InvestedVnd += value;
                }
                if (inRange && t.Category == null && (t.Kind == FinKinds.Income || t.Kind == FinKinds.Expense))
                    Month(day).Uncategorized++;
            }

            FinNetWorthPointDto Value(DateOnly day)
            {
                var point = new FinNetWorthPointDto { Date = day, DebtVnd = Math.Round(debtVnd, 0) };
                decimal total = debtVnd;
                foreach (var ((account, asset), qty) in holdings)
                {
                    if (Math.Abs(qty) < DustQuantity) continue;
                    var px = prices.Vnd(asset, day);
                    if (px == null) { missing.Add(asset); continue; }
                    var v = qty * px.Value;
                    point.Accounts[account] = point.Accounts.GetValueOrDefault(account) + v;
                    total += v;
                }
                foreach (var k in point.Accounts.Keys.ToList()) point.Accounts[k] = Math.Round(point.Accounts[k], 0);
                point.TotalVnd = Math.Round(total, 0);
                return point;
            }

            var i = 0;
            for (; i < rows.Count && rows[i].Day < from; i++) Apply(rows[i].Tx, rows[i].Day, inRange: false);

            for (var day = from; day <= to; day = day.AddDays(1))
            {
                for (; i < rows.Count && rows[i].Day == day; i++) Apply(rows[i].Tx, day, inRange: true);

                var monthEnd = day.AddDays(1).Day == 1 || day == to;
                var seriesPoint = IsPeriodEnd(day, interval) || day == to;
                if (!monthEnd && !seriesPoint) continue;

                var point = Value(day);
                if (seriesPoint) summary.Series.Add(point);
                if (monthEnd) Month(day).NetWorthEndVnd = point.TotalVnd;
            }

            foreach (var ((account, asset), qty) in holdings.OrderBy(h => h.Key.Account).ThenBy(h => h.Key.Asset))
            {
                if (Math.Abs(qty) < DustQuantity) continue;
                var px = prices.Vnd(asset, to);
                summary.Holdings.Add(new FinHoldingDto
                {
                    Account = account,
                    Asset = asset,
                    Quantity = qty,
                    PriceVnd = px,
                    ValueVnd = px == null ? null : Math.Round(qty * px.Value, 0),
                });
            }
            summary.Holdings = summary.Holdings.OrderByDescending(h => h.ValueVnd ?? 0).ToList();

            foreach (var m in months.Values)
            {
                m.IncomeVnd = Math.Round(m.IncomeVnd, 0);
                m.ExpenseVnd = Math.Round(m.ExpenseVnd, 0);
                m.InvestedVnd = Math.Round(m.InvestedVnd, 0);
            }
            summary.Months = months.Values.ToList();
            summary.InvestedVnd = Math.Round(investedVnd, 0);
            summary.MissingPrices = missing.ToList();
            return summary;
        }
    }
}
