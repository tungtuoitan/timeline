using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests.Finance;

namespace SuperAppServices.Interfaces.Finance
{
    public interface IFinanceService
    {
        Task<ResultOptions> GetTransactionsAsync(int userId, string? from, string? to, string? account, string? kind, bool uncategorized, int? limit);
        Task<ResultOptions> UpsertTransactionsAsync(int userId, UpsertFinTransactionsRequest request);
        Task<ResultOptions> PatchTransactionAsync(int userId, int id, PatchFinTransactionRequest request);
        Task<ResultOptions> DeleteTransactionAsync(int userId, int id);
        Task<ResultOptions> GetSummaryAsync(int userId, string? from, string? to, string? interval);
        Task<ResultOptions> UpsertPricesAsync(UpsertFinPricesRequest request);
        Task<ResultOptions> GetLatestPricesAsync();
    }
}
