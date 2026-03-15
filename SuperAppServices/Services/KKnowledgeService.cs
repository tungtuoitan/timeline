using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;
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
                ImageBase64 = k.ImageBase64,
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

            var flatData = tree.Nodes.Select(n =>
            {
                // For shortcut nodes: resolve all display fields from target.
                // target == null  → target was hard-deleted (edge case, trigger cleans up later)
                // target.DeletedAt != null → target soft-deleted → shortcut inherits deleted_at
                KNodeEntity? target = null;
                if (n.TypeCode == "shortcut" && n.RefTargetId.HasValue)
                    tree.ShortcutTargets.TryGetValue(n.RefTargetId.Value, out target);

                return new KNodeResponse
                {
                    Id          = n.Id,
                    KnowledgeId = n.KnowledgeId,
                    ParentId    = n.ParentId,
                    TypeCode             = n.TypeCode,
                    RefTargetId          = n.RefTargetId,
                    RefTargetKnowledgeId = n.RefTargetKnowledgeId,
                    // Resolved fields — shortcut reads from target, regular node from self
                    Name        = target?.Name        ?? n.Name,
                    Description = target?.Description ?? n.Description,
                    Color       = target?.Color       ?? n.Color,
                    Icon        = target?.Icon        ?? n.Icon,
                    PathIds     = n.PathIds,
                    PathDepth   = n.PathDepth,
                    CreatedAt   = n.CreatedAt,
                    UpdatedAt   = n.UpdatedAt,
                    // deleted_at: shortcut inherits target's deleted_at (key design)
                    DeletedAt   = target != null ? target.DeletedAt : n.DeletedAt,
                };
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

        public async Task<ResultOptions> DeleteShortcutAsync(int knowledgeId, int nodeId, int userId)
        {
            try
            {
                // Verify the knowledge belongs to this user
                var knowledge = await _repo.GetKnowledgeByIdAsync(knowledgeId, userId);
                if (knowledge == null)
                    return new ResultOptions { Success = false, Message = "Knowledge not found", Status = 404 };

                var deleted = await _repo.HardDeleteShortcutAsync(knowledgeId, nodeId);
                if (!deleted)
                    return new ResultOptions
                    {
                        Success = false,
                        Message = $"Node {nodeId} not found in knowledge {knowledgeId}, or it is not a shortcut",
                        Status  = 404
                    };

                _logger.LogInformation(
                    "Shortcut node {NodeId} hard-deleted from knowledge {KnowledgeId} by user {UserId}",
                    nodeId, knowledgeId, userId);

                return new ResultOptions { Success = true, Message = "Shortcut deleted", Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting shortcut {NodeId} in knowledge {KnowledgeId}", nodeId, knowledgeId);
                return new ResultOptions { Success = false, Message = "Failed to delete shortcut", Status = 500 };
            }
        }
    }
}
