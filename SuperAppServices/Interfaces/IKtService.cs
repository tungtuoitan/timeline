using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;

namespace SuperAppServices.Interfaces
{
    public interface IKtService
    {
        // Knowledge
        Task<ResultOptions> GetKnowledgesAsync(KtKnowledgeFilterOptions filter);
        Task<ResultOptions> UpsertKnowledgesAsync(List<UpsertKtKnowledgeRequest> requests);

        // Cards
        Task<ResultOptions> GetCardsAsync(KtCardFilterOptions filter);
        Task<ResultOptions> UpsertCardsAsync(List<UpsertKtCardRequest> requests);
    }
}
