using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;

namespace SuperAppDataRepositories.Ins
{
    public interface IKtRepository
    {
        // Knowledge
        Task<ResultOptions> GetKnowledgesAsync(KtKnowledgeFilterOptions filter);
        Task<ResultOptions> UpsertKnowledgesAsync(List<SuperAppModels.Models.KtKnowledge> items);

        // Cards
        Task<ResultOptions> GetCardsAsync(KtCardFilterOptions filter);
        Task<ResultOptions> UpsertCardsAsync(List<SuperAppModels.Models.KtCard> cards, Dictionary<int, List<int>> linkedCardIdsMap);
    }
}
