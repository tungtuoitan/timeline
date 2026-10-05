using SuperAppModels.DTOs.Responses.Finance;
using SuperAppModels.Models.Finance;

namespace SuperAppDataRepositories.Ins.Finance
{
    public interface IFinanceRepository
    {
        /// <summary>Non-deleted rows of the user, newest first, filtered by [fromUtc, toUtc) and optional account/kind.</summary>
        Task<List<FinTransaction>> GetTransactionsAsync(int userId, DateTime? fromUtc, DateTime? toUtc,
            string? account, string? kind, bool uncategorizedOnly, int limit);

        /// <summary>All non-deleted rows before <paramref name="toUtc"/>, oldest first, without raw_json (summary input).</summary>
        Task<List<FinTransaction>> GetLedgerAsync(int userId, DateTime toUtc);

        Task<FinTransaction?> GetByIdAsync(int userId, int id);

        /// <summary>Insert rows; a row whose (source, externalId) already exists updates the source facts instead.</summary>
        Task<FinUpsertResultDto> UpsertTransactionsAsync(int userId, List<FinTransaction> rows);

        /// <summary>Persist changes made to a tracked row from <see cref="GetByIdAsync"/>.</summary>
        Task SaveAsync(FinTransaction row);

        Task<bool> SoftDeleteAsync(int userId, int id);

        /// <summary>Cached prices of the given assets up to <paramref name="to"/> (inclusive).</summary>
        Task<List<FinPrice>> GetPricesAsync(IReadOnlyCollection<string> assets, DateOnly to);

        /// <summary>Insert or replace prices; returns rows written.</summary>
        Task<int> UpsertPricesAsync(List<FinPrice> prices);

        Task<List<FinPriceLatestDto>> GetLatestPriceDatesAsync();
    }
}
