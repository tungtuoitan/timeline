using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Utils;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services.K
{
    public class KKnowledgeService : IKKnowledgeService
    {
        private readonly IKKnowledgeRepository _repo;
        private readonly ILogger<KKnowledgeService> _logger;

        public KKnowledgeService(
            IKKnowledgeRepository repo,
            ILogger<KKnowledgeService> logger)
        {
            _repo   = repo   ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<KKnowledgeSummary>> GetAllKnowledgesAsync(int userId, FilterOptions? filterOptions = null)
        {
            try
            {
                var list = await _repo.GetAllKnowledgesByUserIdAsync(userId, filterOptions);
                return list.Select(k => new KKnowledgeSummary
                {
                    Id          = k.Id,
                    UserId      = k.UserId,
                    Name        = k.Name,
                    Description = k.Description,
                    StatusCode  = k.StatusCode,
                    ImageBase64 = k.ImageBase64,
                    CreatedAt   = k.CreatedAt,
                    UpdatedAt   = k.UpdatedAt,
                    DeletedAt   = k.DeletedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all knowledges for user {UserId}", userId);
                throw;
            }
        }

        public async Task<KKnowledgeDTO> GetKnowledgeTreeAsync(int knowledgeId, int userId)
        {
            try
            {
                _logger.LogInformation("Getting knowledge tree for KnowledgeId: {KnowledgeId}", knowledgeId);

                var knowledge = await _repo.GetKnowledgeByIdAsync(knowledgeId, userId);
                if (knowledge == null)
                    throw new KeyNotFoundException($"Knowledge with ID {knowledgeId} not found");

                var tree = await _repo.GetKnowledgeTreeAsync(knowledgeId, userId);
                if (tree == null)
                    throw new InvalidOperationException($"Failed to retrieve tree for knowledge {knowledgeId}");

                var flatData = tree.Nodes.Select(n => new KNodeResponse
                {
                    Id          = n.Id,
                    KnowledgeId = n.KnowledgeId,
                    ParentId    = n.ParentId,
                    Name        = n.Name,
                    Description = n.Description,
                    Color       = n.Color,
                    Icon        = n.Icon,
                    StatusCode  = n.StatusCode,
                    PathIds     = n.PathIds,
                    PathDepth   = n.PathDepth,
                    CreatedAt   = n.CreatedAt,
                    UpdatedAt   = n.UpdatedAt,
                    DeletedAt   = n.DeletedAt,
                    DueSrsCount        = tree.NodeDueCounts.GetValueOrDefault(n.Id, 0),
                    DraftQuestionCount = tree.NodeDraftCounts.GetValueOrDefault(n.Id, 0)
                }).ToList();

                _logger.LogInformation("Retrieved {Count} nodes for knowledge {KnowledgeId}", flatData.Count, knowledgeId);

                return new KKnowledgeDTO
                {
                    Id          = knowledge.Id,
                    UserId      = knowledge.UserId,
                    Name        = knowledge.Name,
                    Description = knowledge.Description,
                    StatusCode  = knowledge.StatusCode,
                    CreatedAt   = knowledge.CreatedAt ?? VietnamDateTime.Now(),
                    UpdatedAt   = knowledge.UpdatedAt,
                    DeletedAt   = knowledge.DeletedAt,
                    FlatData    = flatData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting knowledge tree for knowledge {KnowledgeId}, user {UserId}", knowledgeId, userId);
                throw;
            }
        }

        public async Task<ResultOptions> DeleteNodesAsync(int knowledgeId, int userId, KDeleteNodesRequest request)
        {
            try
            {
                _logger.LogInformation("Deleting {Count} nodes in knowledge {KnowledgeId}", request.NodeIds.Count, knowledgeId);
                return await _repo.DeleteNodesAsync(knowledgeId, request.NodeIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting nodes in knowledge {KnowledgeId}", knowledgeId);
                return new ResultOptions { Success = false, Message = "An error occurred while deleting nodes", Status = 500 };
            }
        }

        public async Task<ResultOptions> CreateKnowledgeAsync(KUpsertKnowledgeRequest request)
        {
            try
            {
                var entity = await _repo.CreateAsync(request);
                var summary = new KKnowledgeSummary
                {
                    Id          = entity.Id,
                    UserId      = entity.UserId,
                    Name        = entity.Name,
                    Description = entity.Description,
                    ImageBase64 = entity.ImageBase64,
                    StatusCode  = entity.StatusCode,
                    CreatedAt   = entity.CreatedAt,
                    UpdatedAt   = entity.UpdatedAt,
                    DeletedAt   = entity.DeletedAt,
                };
                return new ResultOptions { Success = true, Message = "Knowledge created", Object = summary, Status = 201 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating knowledge for user {UserId}", request.UserId);
                return new ResultOptions { Success = false, Message = "Failed to create knowledge", Status = 500 };
            }
        }

        public async Task<ResultOptions> UpdateKnowledgeAsync(int id, KUpsertKnowledgeRequest request)
        {
            try
            {
                var entity = await _repo.UpdateAsync(id, request);
                if (entity == null)
                    return new ResultOptions { Success = false, Message = "Knowledge not found", Status = 404 };

                var summary = new KKnowledgeSummary
                {
                    Id          = entity.Id,
                    UserId      = entity.UserId,
                    Name        = entity.Name,
                    Description = entity.Description,
                    ImageBase64 = entity.ImageBase64,
                    StatusCode  = entity.StatusCode,
                    CreatedAt   = entity.CreatedAt,
                    UpdatedAt   = entity.UpdatedAt,
                    DeletedAt   = entity.DeletedAt,
                };
                return new ResultOptions { Success = true, Message = "Knowledge updated", Object = summary, Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating knowledge {Id}", id);
                return new ResultOptions { Success = false, Message = "Failed to update knowledge", Status = 500 };
            }
        }

        public async Task<ResultOptions> SoftDeleteKnowledgeAsync(int id, int userId)
        {
            try
            {
                var deleted = await _repo.SoftDeleteAsync(id, userId);
                if (!deleted)
                    return new ResultOptions { Success = false, Message = "Knowledge not found", Status = 404 };

                return new ResultOptions { Success = true, Message = "Knowledge deleted", Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft-deleting knowledge {Id}", id);
                return new ResultOptions { Success = false, Message = "Failed to delete knowledge", Status = 500 };
            }
        }
    }
}
