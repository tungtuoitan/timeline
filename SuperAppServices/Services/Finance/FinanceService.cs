using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins.Finance;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests.Finance;
using SuperAppModels.DTOs.Responses.Finance;
using SuperAppModels.Models.Finance;
using SuperAppModels.Time;
using SuperAppServices.Interfaces.Finance;

namespace SuperAppServices.Services.Finance
{
    /// <summary>Finance ledger + prices + summary (TungRoot #1482).</summary>
    public class FinanceService : IFinanceService
    {
        public const int MaxBatch = 5000;
        public const int MaxPriceBatch = 20000;
        private const int DefaultListLimit = 200;
        private static readonly Regex AssetPattern = new("^[A-Z0-9_]{1,20}$", RegexOptions.Compiled);
        private static readonly Regex QuotePattern = new("^[A-Z]{2,10}$", RegexOptions.Compiled);

        private readonly IFinanceRepository _repository;
        private readonly ILogger<FinanceService> _logger;

        public FinanceService(IFinanceRepository repository, ILogger<FinanceService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ResultOptions> GetTransactionsAsync(int userId, string? from, string? to, string? account, string? kind, bool uncategorized, int? limit)
        {
            try
            {
                if (!TryParseOptionalDate(from, out var start) || !TryParseOptionalDate(to, out var end))
                    return BadRequest("from/to must be yyyy-MM-dd");
                if (kind != null && !FinKinds.All.Contains(kind)) return BadRequest($"kind must be one of {string.Join(", ", FinKinds.All)}");
                var take = Math.Clamp(limit ?? DefaultListLimit, 1, MaxBatch);

                var rows = await _repository.GetTransactionsAsync(userId,
                    start is DateOnly s ? UserClock.StartOfUserDayUtc(s) : null,
                    end is DateOnly e ? UserClock.StartOfUserDayUtc(e.AddDays(1)) : null,
                    string.IsNullOrWhiteSpace(account) ? null : account.Trim(), kind, uncategorized, take);
                var data = rows.Select(ToDto).Cast<object>().ToList();
                return new ResultOptions { Success = true, Message = $"Retrieved {data.Count} transactions", Data = data, TotalCount = data.Count, Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing finance transactions for userId={UserId}", userId);
                return Error(ex);
            }
        }

        public async Task<ResultOptions> UpsertTransactionsAsync(int userId, UpsertFinTransactionsRequest request)
        {
            try
            {
                var inputs = request.Transactions ?? new();
                if (inputs.Count == 0) return BadRequest("transactions is empty");
                if (inputs.Count > MaxBatch) return BadRequest($"at most {MaxBatch} transactions per call");

                var rows = new List<FinTransaction>(inputs.Count);
                for (var i = 0; i < inputs.Count; i++)
                {
                    var error = TryBuild(inputs[i], out var row);
                    if (error != null) return BadRequest($"transactions[{i}]: {error}");
                    rows.Add(row!);
                }

                var result = await _repository.UpsertTransactionsAsync(userId, rows);
                return new ResultOptions { Success = true, Message = $"{result.Inserted} inserted, {result.Updated} updated, {result.Unchanged} unchanged", Object = result, Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting finance transactions for userId={UserId}", userId);
                return Error(ex);
            }
        }

        public async Task<ResultOptions> PatchTransactionAsync(int userId, int id, PatchFinTransactionRequest request)
        {
            try
            {
                if (request.Kind != null && !FinKinds.All.Contains(request.Kind)) return BadRequest($"kind must be one of {string.Join(", ", FinKinds.All)}");
                var lengthError = CheckLength("category", request.Category, 100) ?? CheckLength("counterparty", request.Counterparty, 200)
                    ?? CheckLength("groupKey", request.GroupKey, 100) ?? CheckLength("note", request.Note, 1000);
                if (lengthError != null) return BadRequest(lengthError);

                var row = await _repository.GetByIdAsync(userId, id);
                if (row == null) return NotFound();

                if (request.Kind != null) row.Kind = request.Kind;
                if (request.Category != null) row.Category = Clean(request.Category);
                if (request.Counterparty != null) row.Counterparty = Clean(request.Counterparty);
                if (request.GroupKey != null) row.GroupKey = Clean(request.GroupKey);
                if (request.Note != null) row.Note = Clean(request.Note);
                if (request.ValueVnd != null) row.ValueVnd = Math.Round(request.ValueVnd.Value, 0);
                await _repository.SaveAsync(row);
                return new ResultOptions { Success = true, Message = "Saved", Object = ToDto(row), Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error patching finance transaction {Id} for userId={UserId}", id, userId);
                return Error(ex);
            }
        }

        public async Task<ResultOptions> DeleteTransactionAsync(int userId, int id)
        {
            try
            {
                return await _repository.SoftDeleteAsync(userId, id)
                    ? new ResultOptions { Success = true, Message = "Deleted", Status = 200 }
                    : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting finance transaction {Id} for userId={UserId}", id, userId);
                return Error(ex);
            }
        }

        public async Task<ResultOptions> GetSummaryAsync(int userId, string? from, string? to, string? interval)
        {
            try
            {
                if (!TryParseOptionalDate(from, out var start) || !TryParseOptionalDate(to, out var end))
                    return BadRequest("from/to must be yyyy-MM-dd");
                var step = string.IsNullOrWhiteSpace(interval) ? "day" : interval.Trim().ToLowerInvariant();
                if (!FinanceCalculator.Intervals.Contains(step)) return BadRequest("interval must be day, week or month");

                var last = end ?? UserClock.UserToday();
                var ledger = await _repository.GetLedgerAsync(userId, UserClock.StartOfUserDayUtc(last.AddDays(1)));
                var first = start ?? (ledger.Count > 0 ? UserClock.ToUserDate(ledger[0].OccurredAt) : last);
                if (first > last) return BadRequest("from must be on or before to");

                var assets = ledger.Select(t => t.Asset).Append(FinanceCalculator.Usdt).Distinct().ToList();
                var prices = new FinanceCalculator.PriceBook(await _repository.GetPricesAsync(assets, last));
                var summary = FinanceCalculator.BuildSummary(ledger, prices, first, last, step, UserClock.ToUserDate);
                return new ResultOptions { Success = true, Message = $"Summary {first:yyyy-MM-dd} → {last:yyyy-MM-dd}", Object = summary, Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building finance summary for userId={UserId}", userId);
                return Error(ex);
            }
        }

        public async Task<ResultOptions> UpsertPricesAsync(UpsertFinPricesRequest request)
        {
            try
            {
                var inputs = request.Prices ?? new();
                if (inputs.Count == 0) return BadRequest("prices is empty");
                if (inputs.Count > MaxPriceBatch) return BadRequest($"at most {MaxPriceBatch} prices per call");

                var rows = new List<FinPrice>(inputs.Count);
                for (var i = 0; i < inputs.Count; i++)
                {
                    var p = inputs[i];
                    var asset = (p.Asset ?? "").Trim().ToUpperInvariant();
                    var quote = (p.Quote ?? "").Trim().ToUpperInvariant();
                    var source = (p.Source ?? "").Trim().ToLowerInvariant();
                    if (!AssetPattern.IsMatch(asset)) return BadRequest($"prices[{i}]: asset must match [A-Z0-9_]{{1,20}}");
                    if (!QuotePattern.IsMatch(quote)) return BadRequest($"prices[{i}]: quote must be 2-10 letters");
                    if (asset == quote) return BadRequest($"prices[{i}]: asset and quote must differ");
                    if (p.Date == default) return BadRequest($"prices[{i}]: date is required");
                    if (p.Price <= 0) return BadRequest($"prices[{i}]: price must be > 0");
                    if (source.Length is 0 or > 20) return BadRequest($"prices[{i}]: source is required (max 20)");
                    rows.Add(new FinPrice { Date = p.Date, Asset = asset, Quote = quote, Price = Math.Round(p.Price, 10), Source = source });
                }
                // last one wins inside a batch
                rows = rows.GroupBy(r => (r.Asset, r.Quote, r.Date)).Select(g => g.Last()).ToList();

                var written = await _repository.UpsertPricesAsync(rows);
                return new ResultOptions { Success = true, Message = $"{written} prices saved", TotalCount = written, Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting finance prices");
                return Error(ex);
            }
        }

        public async Task<ResultOptions> GetLatestPricesAsync()
        {
            try
            {
                var data = (await _repository.GetLatestPriceDatesAsync()).Cast<object>().ToList();
                return new ResultOptions { Success = true, Message = $"{data.Count} price series", Data = data, TotalCount = data.Count, Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading latest finance prices");
                return Error(ex);
            }
        }

        /// <summary>Validate + normalise one input row; returns an error message or null.</summary>
        public static string? TryBuild(FinTransactionInput input, out FinTransaction? row)
        {
            row = null;
            var account = (input.Account ?? "").Trim();
            var asset = (input.Asset ?? "").Trim().ToUpperInvariant();
            var kind = (input.Kind ?? "").Trim().ToLowerInvariant();
            var source = string.IsNullOrWhiteSpace(input.Source) ? FinSources.Manual : input.Source.Trim().ToLowerInvariant();

            if (account.Length is 0 or > 50) return "account is required (max 50)";
            if (!AssetPattern.IsMatch(asset)) return "asset must match [A-Z0-9_]{1,20}";
            if (input.Amount == 0) return "amount must not be 0";
            if (input.OccurredAt == default) return "occurredAt is required";
            if (!FinKinds.All.Contains(kind)) return $"kind must be one of {string.Join(", ", FinKinds.All)}";
            if (!FinSources.All.Contains(source)) return $"source must be one of {string.Join(", ", FinSources.All)}";
            var lengthError = CheckLength("externalId", input.ExternalId, 200) ?? CheckLength("category", input.Category, 100)
                ?? CheckLength("counterparty", input.Counterparty, 200) ?? CheckLength("description", input.Description, 1000)
                ?? CheckLength("groupKey", input.GroupKey, 100) ?? CheckLength("note", input.Note, 1000);
            if (lengthError != null) return lengthError;

            row = new FinTransaction
            {
                Account = account,
                OccurredAt = UserClock.AsUtc(input.OccurredAt),
                Asset = asset,
                Amount = Math.Round(input.Amount, 10),
                ValueVnd = input.ValueVnd is decimal v ? Math.Round(v, 0) : null,
                Kind = kind,
                Category = Clean(input.Category),
                Counterparty = Clean(input.Counterparty),
                Description = Clean(input.Description),
                GroupKey = Clean(input.GroupKey),
                Source = source,
                ExternalId = Clean(input.ExternalId),
                RawJson = string.IsNullOrWhiteSpace(input.RawJson) ? null : input.RawJson,
                Note = Clean(input.Note),
            };
            return null;
        }

        public static FinTransactionDto ToDto(FinTransaction t) => new()
        {
            Id = t.Id,
            Account = t.Account,
            OccurredAt = t.OccurredAt,
            Asset = t.Asset,
            Amount = t.Amount,
            ValueVnd = t.ValueVnd,
            Kind = t.Kind,
            Category = t.Category,
            Counterparty = t.Counterparty,
            Description = t.Description,
            GroupKey = t.GroupKey,
            Source = t.Source,
            ExternalId = t.ExternalId,
            Note = t.Note,
            UpdatedAt = t.UpdatedAt,
        };

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string? CheckLength(string field, string? value, int max)
            => value != null && value.Trim().Length > max ? $"{field} is limited to {max} characters" : null;

        private static bool TryParseOptionalDate(string? value, out DateOnly? date)
        {
            date = null;
            if (string.IsNullOrWhiteSpace(value)) return true;
            if (!TimeParsing.TryParseDate(value, out var d)) return false;
            date = d;
            return true;
        }

        private static ResultOptions BadRequest(string message) => new() { Success = false, Message = message, Status = 400 };
        private static ResultOptions NotFound() => new() { Success = false, Message = "Transaction not found", Status = 404 };
        private static ResultOptions Error(Exception ex) => new() { Success = false, Message = ex.Message, Status = 500 };
    }
}
