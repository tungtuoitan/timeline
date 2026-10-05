using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Responses.Finance
{
    /// <summary>Ledger row as returned by GET /api/finance/transactions (raw_json left out).</summary>
    public class FinTransactionDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("account")] public string Account { get; set; } = string.Empty;
        [JsonPropertyName("occurredAt")] public DateTime OccurredAt { get; set; }
        [JsonPropertyName("asset")] public string Asset { get; set; } = string.Empty;
        [JsonPropertyName("amount")] public decimal Amount { get; set; }
        [JsonPropertyName("valueVnd")] public decimal? ValueVnd { get; set; }
        [JsonPropertyName("kind")] public string Kind { get; set; } = string.Empty;
        [JsonPropertyName("category")] public string? Category { get; set; }
        [JsonPropertyName("counterparty")] public string? Counterparty { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("groupKey")] public string? GroupKey { get; set; }
        [JsonPropertyName("source")] public string Source { get; set; } = string.Empty;
        [JsonPropertyName("externalId")] public string? ExternalId { get; set; }
        [JsonPropertyName("note")] public string? Note { get; set; }
        [JsonPropertyName("updatedAt")] public DateTime UpdatedAt { get; set; }
    }

    /// <summary>Result of a batch upsert.</summary>
    public class FinUpsertResultDto
    {
        [JsonPropertyName("inserted")] public int Inserted { get; set; }
        [JsonPropertyName("updated")] public int Updated { get; set; }
        [JsonPropertyName("unchanged")] public int Unchanged { get; set; }
    }

    /// <summary>
    /// GET /api/finance/summary — everything the homepage needs, in VND.
    /// Net worth = Σ holdings × price (carry-forward) + outstanding debt receivable.
    /// </summary>
    public class FinSummaryDto
    {
        [JsonPropertyName("from")] public DateOnly From { get; set; }
        [JsonPropertyName("to")] public DateOnly To { get; set; }
        [JsonPropertyName("interval")] public string Interval { get; set; } = "day";
        /// <summary>End-of-period net worth points.</summary>
        [JsonPropertyName("series")] public List<FinNetWorthPointDto> Series { get; set; } = new();
        [JsonPropertyName("months")] public List<FinMonthDto> Months { get; set; } = new();
        /// <summary>Holdings at <see cref="To"/>, largest first.</summary>
        [JsonPropertyName("holdings")] public List<FinHoldingDto> Holdings { get; set; } = new();
        /// <summary>Net money put into investments since the beginning (Σ invest legs on non-VND assets).</summary>
        [JsonPropertyName("investedVnd")] public decimal InvestedVnd { get; set; }
        /// <summary>Assets that had a balance on some day without any known price (value left out).</summary>
        [JsonPropertyName("missingPrices")] public List<string> MissingPrices { get; set; } = new();
    }

    public class FinNetWorthPointDto
    {
        [JsonPropertyName("date")] public DateOnly Date { get; set; }
        [JsonPropertyName("totalVnd")] public decimal TotalVnd { get; set; }
        /// <summary>Value per account, VND.</summary>
        [JsonPropertyName("accounts")] public Dictionary<string, decimal> Accounts { get; set; } = new();
        /// <summary>Outstanding lent money (+) / borrowed or held for others (−).</summary>
        [JsonPropertyName("debtVnd")] public decimal DebtVnd { get; set; }
    }

    public class FinMonthDto
    {
        [JsonPropertyName("month")] public DateOnly Month { get; set; }
        [JsonPropertyName("incomeVnd")] public decimal IncomeVnd { get; set; }
        /// <summary>Positive number; refunds (positive expense rows) reduce it.</summary>
        [JsonPropertyName("expenseVnd")] public decimal ExpenseVnd { get; set; }
        [JsonPropertyName("investedVnd")] public decimal InvestedVnd { get; set; }
        [JsonPropertyName("netWorthEndVnd")] public decimal NetWorthEndVnd { get; set; }
        /// <summary>Rows of kind expense/income without category in that month.</summary>
        [JsonPropertyName("uncategorized")] public int Uncategorized { get; set; }
    }

    public class FinHoldingDto
    {
        [JsonPropertyName("account")] public string Account { get; set; } = string.Empty;
        [JsonPropertyName("asset")] public string Asset { get; set; } = string.Empty;
        [JsonPropertyName("quantity")] public decimal Quantity { get; set; }
        [JsonPropertyName("priceVnd")] public decimal? PriceVnd { get; set; }
        [JsonPropertyName("valueVnd")] public decimal? ValueVnd { get; set; }
    }

    /// <summary>Latest cached price date per (asset, quote) — lets the sync script fetch only what's missing.</summary>
    public class FinPriceLatestDto
    {
        [JsonPropertyName("asset")] public string Asset { get; set; } = string.Empty;
        [JsonPropertyName("quote")] public string Quote { get; set; } = string.Empty;
        [JsonPropertyName("date")] public DateOnly Date { get; set; }
    }
}
