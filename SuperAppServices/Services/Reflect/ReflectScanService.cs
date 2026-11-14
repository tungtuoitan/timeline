using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins.Reflect;
using SuperAppModels.DTOs.Reflect;
using SuperAppModels.Models.Reflect;
using SuperAppModels.Time;
using SuperAppServices.Interfaces;
using SuperAppServices.Interfaces.Reflect;

namespace SuperAppServices.Services.Reflect
{
    /// <summary>
    /// Reads Reflect rows with the model: one tag per row plus the numbers in it (km run, hours slept, times suggested...).
    /// A row is read once: its scan hash is the hash of its text, and only rows whose text changed since are read again.
    /// </summary>
    public class ReflectScanService : IReflectScanService
    {
        public static readonly string[] Tags = { "thought", "health", "money", "work", "emotion", "relationship", "learning", "other" };
        private static readonly string[] Aggregates = { "sum", "avg", "max" };
        private const int BatchSize = 15;
        private const int MaxBatchesPerRun = 40;
        private const int MaxTextChars = 2000;

        private const string SystemPrompt =
            "You read entries of a personal journal (any language, mostly Vietnamese) and return strict JSON, nothing else.\n" +
            "Input: a JSON array of entries {id, date, type, think, real}; date is the day the entry was written (yyyy-MM-dd).\n" +
            "Output: a JSON array with one object per input id: {\"id\": string, \"tag\": string, \"observations\": [ ... ]}.\n" +
            "tag: exactly one of thought, health, money, work, emotion, relationship, learning, other.\n" +
            "observations: numbers the text states about something that HAPPENED (past or today). Ignore plans, predictions, goals and intentions.\n" +
            "Each observation: {\"metric\": code, \"label\": string, \"unit\": string, \"aggregate\": \"sum\"|\"avg\"|\"max\", \"date\": yyyy-MM-dd, \"value\": number, \"quote\": the words it was read from, max 120 chars}.\n" +
            "Resolve relative days (yesterday, last night, this morning) from the entry date; date is the day the thing happened.\n" +
            "Reuse a metric code from the existing list whenever the same kind of number fits (even if worded differently). Only make a new code when none fits: lowercase letters, digits and underscores, with the unit at the end (run_km, sleep_hours, suggestion_count).\n" +
            "aggregate says how several numbers of one day combine: sum for amounts (km, count), avg for levels (mood 1-10), max for peaks.\n" +
            "Do not invent numbers. When there is no number, observations is an empty array.";

        private readonly IReflectScanRepository _repository;
        private readonly IClaudibleService _ai;
        private readonly ILogger<ReflectScanService> _logger;

        public ReflectScanService(IReflectScanRepository repository, IClaudibleService ai, ILogger<ReflectScanService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _ai = ai ?? throw new ArgumentNullException(nameof(ai));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<List<ReflectSeries>> GetSeriesAsync(int userId, DateTime? from, DateTime? to) => _repository.GetSeriesAsync(userId, from, to);

        public async Task<int> ScanAllAsync(CancellationToken ct)
        {
            var total = 0;
            foreach (var userId in await _repository.GetUserIdsWithPendingAsync())
            {
                ct.ThrowIfCancellationRequested();
                total += (await ScanUserAsync(userId, ct)).Scanned;
            }
            return total;
        }

        public async Task<ReflectScanResult> ScanUserAsync(int userId, CancellationToken ct)
        {
            var result = new ReflectScanResult();
            var tz = TimeZones.FindOrDefault(await _repository.GetTimezoneIdAsync(userId));

            for (var batchNo = 0; batchNo < MaxBatchesPerRun; batchNo++)
            {
                ct.ThrowIfCancellationRequested();
                var readAt = DateTime.UtcNow;
                var entries = await _repository.GetPendingEntriesAsync(userId, BatchSize);
                if (entries.Count == 0) break;

                // Rows whose text is the same as at the last scan (or empty) need no model call
                var skip = new Dictionary<string, string>();
                var toRead = new List<(ReflectEntry Entry, string Hash)>();
                foreach (var e in entries)
                {
                    var hash = HashOf(e);
                    if (e.ScannedHash == hash || IsBlank(e)) skip[e.Id] = hash;
                    else toRead.Add((e, hash));
                }
                if (skip.Count > 0) await _repository.MarkScannedAsync(userId, skip, readAt);
                result.Scanned += skip.Count;
                if (toRead.Count == 0) continue;

                try
                {
                    var outcomes = await ReadBatchAsync(userId, toRead, tz);
                    await _repository.SaveOutcomesAsync(userId, outcomes, readAt);
                    result.Scanned += outcomes.Count;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // The rows stay unscanned and are tried again on the next run
                    _logger.LogError(ex, "Reflect scan failed for user {UserId} ({Count} rows)", userId, toRead.Count);
                    result.Failed += toRead.Count;
                    break;
                }
            }
            return result;
        }

        private async Task<List<ReflectScanOutcome>> ReadBatchAsync(int userId, List<(ReflectEntry Entry, string Hash)> rows, TimeZoneInfo tz)
        {
            var inputs = rows.Select(r => new ReflectScanInput
            {
                Id = r.Entry.Id,
                Date = LocalDay(r.Entry.CreatedAt, tz).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Type = r.Entry.Type,
                Think = Cut(r.Entry.Think),
                Real = Cut(r.Entry.Real)
            }).ToList();

            var metrics = await _repository.GetMetricsAsync(userId);
            var existing = metrics.Count == 0 ? "(none yet)" : string.Join("\n", metrics.Select(m => $"{m.Code} | {m.Label} | {m.Unit} | {m.Aggregate}"));
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            var prompt = "Existing metrics (code | label | unit | aggregate):\n" + existing + "\n\nEntries:\n" + JsonSerializer.Serialize(inputs, options);

            var answer = await _ai.ChatAsync(prompt, SystemPrompt);
            var byId = ParseAnswer(answer);

            return rows.Select(r =>
            {
                var outcome = new ReflectScanOutcome { EntryId = r.Entry.Id, Hash = r.Hash };
                if (byId.TryGetValue(r.Entry.Id, out var item))
                {
                    outcome.Tag = item.Tag;
                    outcome.Observations = item.Observations;
                }
                return outcome;
            }).ToList();
        }

        private record ParsedItem(string? Tag, List<ReflectObservationDraft> Observations);

        private static Dictionary<string, ParsedItem> ParseAnswer(string answer)
        {
            var json = Regex.Replace(answer.Trim(), @"^```(?:json)?\s*|\s*```$", "", RegexOptions.IgnoreCase);
            using var doc = JsonDocument.Parse(json);
            var map = new Dictionary<string, ParsedItem>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var id = item.GetProperty("id").GetString();
                if (string.IsNullOrEmpty(id)) continue;
                var tag = item.TryGetProperty("tag", out var t) ? t.GetString()?.Trim().ToLowerInvariant() : null;
                if (tag == null || !Tags.Contains(tag)) tag = "other";

                var list = new List<ReflectObservationDraft>();
                if (item.TryGetProperty("observations", out var obs) && obs.ValueKind == JsonValueKind.Array)
                    foreach (var o in obs.EnumerateArray())
                        if (TryParseObservation(o, out var draft)) list.Add(draft);
                map[id] = new ParsedItem(tag, list);
            }
            return map;
        }

        private static bool TryParseObservation(JsonElement o, out ReflectObservationDraft draft)
        {
            draft = new ReflectObservationDraft();
            if (!o.TryGetProperty("metric", out var m) || !o.TryGetProperty("value", out var v) || !o.TryGetProperty("date", out var d)) return false;
            if (v.ValueKind != JsonValueKind.Number || !v.TryGetDecimal(out var value)) return false;
            if (!DateTime.TryParseExact(d.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)) return false;

            var code = Regex.Replace((m.GetString() ?? "").Trim().ToLowerInvariant(), @"[^a-z0-9_]+", "_").Trim('_');
            if (code.Length == 0) return false;
            if (code.Length > 64) code = code[..64];

            var aggregate = o.TryGetProperty("aggregate", out var a) ? a.GetString()?.ToLowerInvariant() : null;
            draft = new ReflectObservationDraft
            {
                MetricCode = code,
                Label = Clip(o.TryGetProperty("label", out var l) ? l.GetString() : null, 128, code),
                Unit = Clip(o.TryGetProperty("unit", out var u) ? u.GetString() : null, 32, ""),
                Aggregate = aggregate != null && Aggregates.Contains(aggregate) ? aggregate : "sum",
                OccurredOn = day,
                Value = value,
                Quote = Clip(o.TryGetProperty("quote", out var q) ? q.GetString() : null, 400, "")
            };
            return true;
        }

        private static string Clip(string? text, int max, string fallback)
        {
            var s = (text ?? "").Trim();
            if (s.Length == 0) return fallback;
            return s.Length > max ? s[..max] : s;
        }

        private static string Cut(string text) => text.Length > MaxTextChars ? text[..MaxTextChars] : text;

        private static bool IsBlank(ReflectEntry e) => string.IsNullOrWhiteSpace(e.Think) && string.IsNullOrWhiteSpace(e.Real) && string.IsNullOrWhiteSpace(e.Type);

        private static DateTime LocalDay(DateTime utc, TimeZoneInfo tz) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz).Date;

        /// <summary>Hash of exactly the text the scan reads (type, think, real)</summary>
        private static string HashOf(ReflectEntry e)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{e.Type}\u0000{e.Think}\u0000{e.Real}"));
            return Convert.ToHexString(bytes);
        }
    }
}
