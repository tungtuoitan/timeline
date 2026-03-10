using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
{
    public class KtService : IKtService
    {
        private readonly IKtRepository _repo;
        private readonly ILogger<KtService> _logger;

        public KtService(IKtRepository repo, ILogger<KtService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        // ── Knowledge ────────────────────────────────────────────────────────

        public Task<ResultOptions> GetKnowledgesAsync(KtKnowledgeFilterOptions filter)
            => _repo.GetKnowledgesAsync(filter);

        public async Task<ResultOptions> UpsertKnowledgesAsync(List<UpsertKtKnowledgeRequest> requests)
        {
            try
            {
                var models = requests.Select(r => new KtKnowledge
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    ParentId = r.ParentId,
                    Title = r.Title,
                    Description = r.Description,
                    DeletedAt = r.DeletedAt,
                }).ToList();

                return await _repo.UpsertKnowledgesAsync(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpsertKnowledgesAsync service failed");
                return ResultOptions.Fail(ex.Message);
            }
        }

        // ── Cards ─────────────────────────────────────────────────────────────

        public Task<ResultOptions> GetCardsAsync(KtCardFilterOptions filter)
            => _repo.GetCardsAsync(filter);

        public async Task<ResultOptions> UpsertCardsAsync(List<UpsertKtCardRequest> requests)
        {
            try
            {
                var models = requests.Select(r => new KtCard
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    KnowledgeId = r.KnowledgeId,
                    ParentCardId = r.ParentCardId,
                    Keyword = r.Keyword,
                    Title = r.Title,
                    Description = r.Description,
                    IsDefinition = r.IsDefinition,
                    DeletedAt = r.DeletedAt,
                }).ToList();

                // map request index → linkedCardIds
                var linkedCardIdsMap = new Dictionary<int, List<int>>();
                for (int i = 0; i < requests.Count; i++)
                {
                    if (!requests[i].IsDefinition && requests[i].LinkedCardIds.Count > 0)
                        linkedCardIdsMap[i] = requests[i].LinkedCardIds;
                }

                return await _repo.UpsertCardsAsync(models, linkedCardIdsMap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpsertCardsAsync service failed");
                return ResultOptions.Fail(ex.Message);
            }
        }
    }
}
