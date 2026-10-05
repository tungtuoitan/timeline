namespace SuperAppModels.Models.Finance
{
    /// <summary>
    /// Daily close price of an asset — maps to pro.fin_price_cache (TungRoot #1482). Global (not per user),
    /// fetched from public APIs by the sync script; safe to truncate and refill.
    /// Crypto / tokenized stocks are quoted in USDT, USDT is quoted in VND.
    /// </summary>
    public class FinPrice
    {
        public DateOnly Date { get; set; }
        public string Asset { get; set; } = string.Empty;
        public string Quote { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Source { get; set; } = string.Empty;
        public DateTime FetchedAt { get; set; }
    }
}
