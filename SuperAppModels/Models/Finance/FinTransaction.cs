namespace SuperAppModels.Models.Finance
{
    /// <summary>
    /// One balance change of one asset in one account — maps to pro.fin_transaction (TungRoot #1482).
    /// The ledger is multi-asset: VND, USDT, BTC, EQ_TSLA… are all "assets"; balance of (account, asset)
    /// = sum of <see cref="Amount"/>. A trade / P2P order has several legs sharing <see cref="GroupKey"/>.
    /// </summary>
    public class FinTransaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Account { get; set; } = string.Empty;
        /// <summary>UTC instant.</summary>
        public DateTime OccurredAt { get; set; }
        public string Asset { get; set; } = string.Empty;
        /// <summary>Signed: + in, − out, in units of <see cref="Asset"/>.</summary>
        public decimal Amount { get; set; }
        /// <summary>VND value at the time when known (bank transfer, P2P fiat); null → priced from fin_price_cache.</summary>
        public decimal? ValueVnd { get; set; }
        public string Kind { get; set; } = FinKinds.Adjust;
        public string? Category { get; set; }
        public string? Counterparty { get; set; }
        public string? Description { get; set; }
        public string? GroupKey { get; set; }
        public string Source { get; set; } = FinSources.Manual;
        public string? ExternalId { get; set; }
        public string? RawJson { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    /// <summary>Allowed values of fin_transaction.kind (CHECK constraint in the migration).</summary>
    public static class FinKinds
    {
        /// <summary>Spending — the only kind counted as "chi tiêu".</summary>
        public const string Expense = "expense";
        public const string Income = "income";
        /// <summary>Money moved into/out of an investment (BIDV → P2P → Binance). Not spending.</summary>
        public const string Invest = "invest";
        /// <summary>Swap inside an account (USDT → BTC). Legs share group_key.</summary>
        public const string Trade = "trade";
        /// <summary>Own money between own accounts.</summary>
        public const string Transfer = "transfer";
        /// <summary>Lend / borrow / hold for someone; outstanding = −Σ per counterparty, counted in net worth.</summary>
        public const string Debt = "debt";
        /// <summary>Opening balance or reconciliation with the real balance.</summary>
        public const string Adjust = "adjust";

        public static readonly string[] All = { Expense, Income, Invest, Trade, Transfer, Debt, Adjust };
    }

    public static class FinSources
    {
        public const string Sepay = "sepay";
        public const string Binance = "binance";
        public const string Manual = "manual";
        public const string Import = "import";

        public static readonly string[] All = { Sepay, Binance, Manual, Import };
    }
}
