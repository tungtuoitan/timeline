using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests.Finance
{
    /// <summary>One ledger row as sent by import scripts, SePay sync or manual entry (TungRoot #1482).</summary>
    public class FinTransactionInput
    {
        [JsonPropertyName("account")]
        public string Account { get; set; } = string.Empty;

        /// <summary>ISO 8601 with offset (input without offset is rejected).</summary>
        [JsonPropertyName("occurredAt")]
        public DateTime OccurredAt { get; set; }

        [JsonPropertyName("asset")]
        public string Asset { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("valueVnd")]
        public decimal? ValueVnd { get; set; }

        [JsonPropertyName("kind")]
        public string Kind { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("counterparty")]
        public string? Counterparty { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("groupKey")]
        public string? GroupKey { get; set; }

        /// <summary>sepay | binance | manual | import (default manual).</summary>
        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("externalId")]
        public string? ExternalId { get; set; }

        [JsonPropertyName("rawJson")]
        public string? RawJson { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }
    }

    /// <summary>
    /// POST /api/finance/transactions — insert a batch. Rows with an externalId already stored for the same
    /// source are updated (source facts only: account, time, asset, amount, valueVnd, description,
    /// counterparty, rawJson) — kind/category/note/groupKey edited by hand are kept.
    /// </summary>
    public class UpsertFinTransactionsRequest
    {
        [JsonPropertyName("transactions")]
        public List<FinTransactionInput> Transactions { get; set; } = new();
    }

    /// <summary>PATCH /api/finance/transactions/{id} — only non-null fields change; "" clears a text field.</summary>
    public class PatchFinTransactionRequest
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("counterparty")]
        public string? Counterparty { get; set; }

        [JsonPropertyName("groupKey")]
        public string? GroupKey { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("valueVnd")]
        public decimal? ValueVnd { get; set; }
    }

    public class FinPriceInput
    {
        [JsonPropertyName("date")]
        public DateOnly Date { get; set; }

        [JsonPropertyName("asset")]
        public string Asset { get; set; } = string.Empty;

        [JsonPropertyName("quote")]
        public string Quote { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;
    }

    /// <summary>PUT /api/finance/prices — upsert daily close prices (key: asset, quote, date).</summary>
    public class UpsertFinPricesRequest
    {
        [JsonPropertyName("prices")]
        public List<FinPriceInput> Prices { get; set; } = new();
    }
}
