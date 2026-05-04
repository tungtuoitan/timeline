using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services.Wiki
{
    public class WikiService : IWikiService
    {
        private readonly IWikiRepository _repo;
        private readonly ILogger<WikiService> _logger;

        public WikiService(IWikiRepository repo, ILogger<WikiService> logger)
        {
            _repo   = repo   ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<WikiGetAllResponse> GetAllAsync(int userId)
        {
            var keywords = await _repo.GetAllKeywordsAsync(userId);
            var infos    = await _repo.GetAllInfosAsync(userId);
            return new WikiGetAllResponse
            {
                Keywords = keywords.Select(MapKeyword).ToList(),
                Infos    = infos.Select(MapInfo).ToList()
            };
        }

        public async Task<ResultOptions> CreateKeywordAsync(WikiCreateKeywordRequest request)
        {
            try
            {
                var created = await _repo.CreateKeywordAsync(request.Name.Trim(), request.UserId);

                if (request.Synonyms.Count > 0)
                    await _repo.UpdateKeywordMetaAsync(created.Id, request.UserId, null, null, request.Synonyms);

                var keyword = await _repo.GetKeywordByIdAsync(created.Id, request.UserId);
                return new ResultOptions { Success = true, Message = "Keyword created", Object = MapKeyword(keyword!), Status = 201 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating wiki keyword for user {UserId}", request.UserId);
                return new ResultOptions { Success = false, Message = "An error occurred", Status = 500 };
            }
        }

        public async Task<ResultOptions> UpsertInfoAsync(WikiUpsertInfoRequest request)
        {
            try
            {
                WikiInfo? saved;
                if (request.Id.HasValue)
                {
                    saved = await _repo.UpdateInfoAsync(request);
                    if (saved == null)
                        return new ResultOptions { Success = false, Message = "Info not found", Status = 404 };
                }
                else
                {
                    saved = await _repo.CreateInfoAsync(request);
                }

                var fresh = await _repo.GetInfoByIdAsync(saved.Id, request.UserId);
                return new ResultOptions
                {
                    Success = true,
                    Message = request.Id.HasValue ? "Info updated" : "Info created",
                    Object  = MapInfo(fresh!),
                    Status  = request.Id.HasValue ? 200 : 201
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting wiki info for user {UserId}", request.UserId);
                return new ResultOptions { Success = false, Message = "An error occurred", Status = 500 };
            }
        }

        public async Task<ResultOptions> SoftDeleteInfoAsync(int id, int userId)
        {
            var ok = await _repo.SoftDeleteInfoAsync(id, userId);
            return ok
                ? new ResultOptions { Success = true, Message = "Info deleted", Status = 200 }
                : new ResultOptions { Success = false, Message = "Info not found", Status = 404 };
        }

        public async Task<ResultOptions> RestoreInfoAsync(int id, int userId)
        {
            var ok = await _repo.RestoreInfoAsync(id, userId);
            return ok
                ? new ResultOptions { Success = true, Message = "Info restored", Status = 200 }
                : new ResultOptions { Success = false, Message = "Info not found", Status = 404 };
        }

        public async Task<ResultOptions> SoftDeleteKeywordAsync(int id, int userId)
        {
            var deleted = await _repo.SoftDeleteKeywordAsync(id, userId);
            return deleted
                ? new ResultOptions { Success = true, Message = "Keyword deleted", Status = 200 }
                : new ResultOptions { Success = false, Message = "Keyword not found", Status = 404 };
        }

        public async Task<ResultOptions> UpdateKeywordMetaAsync(int id, WikiUpdateKeywordRequest request)
        {
            try
            {
                var updated = await _repo.UpdateKeywordMetaAsync(
                    id, request.UserId, request.Name, request.IconBase64, request.Synonyms);

                if (updated == null)
                    return new ResultOptions { Success = false, Message = "Keyword not found", Status = 404 };

                if (request.AddInfoIds?.Count > 0)
                    await _repo.AddInfoLinksAsync(id, request.AddInfoIds, request.UserId);

                if (request.RemoveInfoIds?.Count > 0)
                    await _repo.RemoveInfoLinksAsync(id, request.RemoveInfoIds);

                var fresh = await _repo.GetKeywordByIdAsync(id, request.UserId);
                return new ResultOptions { Success = true, Message = "Keyword updated", Object = MapKeyword(fresh!), Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating wiki keyword {Id}", id);
                return new ResultOptions { Success = false, Message = "An error occurred", Status = 500 };
            }
        }

        public async Task<ResultOptions> SavePinnedPositionAsync(int keywordId, WikiSavePinnedPositionRequest request)
        {
            var ok = await _repo.SavePinnedPositionAsync(keywordId, request.UserId, request.X, request.Y);
            return ok
                ? new ResultOptions { Success = true, Message = "Position saved", Status = 200 }
                : new ResultOptions { Success = false, Message = "Keyword not found", Status = 404 };
        }

        public async Task<ResultOptions> IncrementInteractionAsync(int keywordId, int userId, string interactionType)
        {
            switch (interactionType.ToLowerInvariant())
            {
                case "view": await _repo.IncrementKeywordViewAsync(keywordId, userId); break;
                case "read": await _repo.IncrementKeywordReadAsync(keywordId, userId); break;
                case "edit": await _repo.IncrementKeywordEditAsync(keywordId, userId); break;
                default:
                    return new ResultOptions { Success = false, Message = $"Unknown interaction type: {interactionType}", Status = 400 };
            }
            return new ResultOptions { Success = true, Message = "Interaction recorded", Status = 200 };
        }

        public async Task<ResultOptions> RescanAllLinksAsync(int userId)
        {
            try
            {
                var count = await _repo.RescanAllLinksAsync(userId);
                _logger.LogInformation("Rescan all links for user {UserId}: {Count} links", userId, count);
                return new ResultOptions { Success = true, Message = $"Rebuilt {count} links", Object = count, Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rescanning all links for user {UserId}", userId);
                return new ResultOptions { Success = false, Message = "An error occurred", Status = 500 };
            }
        }

        // ── Mapping ───────────────────────────────────────────────────────────

        private static WikiKeywordDto MapKeyword(WikiKeyword k) => new()
        {
            Id             = k.Id,
            Name           = k.Name,
            Description    = k.Description,
            Icon           = k.IconBase64,
            Synonyms       = k.Synonyms.Select(s => s.Synonym).ToList(),
            InfoIds        = k.InfoKeywords.Select(ik => ik.InfoId).ToList(),
            Views          = k.Views,
            Reads          = k.Reads,
            Edits          = k.Edits,
            PosX           = k.PosX,
            PosY           = k.PosY,
            PinnedPosition = k.PinnedPosition,
            DeletedAt      = k.DeletedAt?.ToString("O")
        };

        private static WikiInfoDto MapInfo(WikiInfo i) => new()
        {
            Id         = i.Id,
            Title      = i.Title,
            Content    = i.Content,
            KeywordIds = i.InfoKeywords.Select(ik => ik.KeywordId).ToList(),
            CreatedAt  = (i.CreatedAt ?? DateTime.UtcNow).ToString("O"),
            UpdatedAt  = (i.UpdatedAt ?? i.CreatedAt ?? DateTime.UtcNow).ToString("O"),
            DeletedAt  = i.DeletedAt?.ToString("O")
        };
    }
}
