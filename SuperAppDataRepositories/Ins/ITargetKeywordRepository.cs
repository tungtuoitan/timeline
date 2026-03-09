using SuperAppModels.DTOs;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface ITargetKeywordRepository
    {
        Task<ResultOptions> GetByTargetAsync(int targetId, string targetType);
        Task<ResultOptions> GetByKeywordIdAsync(int keywordId);
        Task<ResultOptions> CreateAsync(TargetKeyword item);
        Task<ResultOptions> DeleteAsync(int id);
    }
}
