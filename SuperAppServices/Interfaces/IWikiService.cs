using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperAppServices.Interfaces
{
    public interface IWikiService
    {
        Task<WikiGetAllResponse> GetAllAsync(int userId);
        Task<ResultOptions> CreateKeywordAsync(WikiCreateKeywordRequest request);
        Task<ResultOptions> UpsertInfoAsync(WikiUpsertInfoRequest request);
        Task<ResultOptions> SoftDeleteInfoAsync(int id, int userId);
        Task<ResultOptions> RestoreInfoAsync(int id, int userId);
        Task<ResultOptions> SoftDeleteKeywordAsync(int id, int userId);
        Task<ResultOptions> UpdateKeywordMetaAsync(int id, WikiUpdateKeywordRequest request);
        Task<ResultOptions> SavePinnedPositionAsync(int keywordId, WikiSavePinnedPositionRequest request);
        Task<ResultOptions> IncrementInteractionAsync(int keywordId, int userId, string interactionType);
        Task<ResultOptions> RescanAllLinksAsync(int userId);
    }
}
