using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
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
            var list = await _repo.GetAllKnowledgesByUserIdAsync(userId, filterOptions);
            return list.Select(k => new KKnowledgeSummary
            {
                Id          = k.Id,
                UserId      = k.UserId,
                Name        = k.Name,
                Description = k.Description,
                StatusCode  = k.StatusCode,
                CreatedAt   = k.CreatedAt,
                UpdatedAt   = k.UpdatedAt,
                DeletedAt   = k.DeletedAt
            }).ToList();
        }

        public async Task<KKnowledgeDTO> GetKnowledgeTreeAsync(int knowledgeId, int userId)
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
                PathIds     = n.PathIds,
                PathDepth   = n.PathDepth,
                CreatedAt   = n.CreatedAt,
                UpdatedAt   = n.UpdatedAt,
                DeletedAt   = n.DeletedAt
            }).ToList();

            _logger.LogInformation("Retrieved {Count} nodes for knowledge {KnowledgeId}", flatData.Count, knowledgeId);

            return new KKnowledgeDTO
            {
                Id          = knowledge.Id,
                UserId      = knowledge.UserId,
                Name        = knowledge.Name,
                Description = knowledge.Description,
                StatusCode  = knowledge.StatusCode,
                CreatedAt   = knowledge.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt   = knowledge.UpdatedAt,
                DeletedAt   = knowledge.DeletedAt,
                FlatData    = flatData
            };
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
    }
}
