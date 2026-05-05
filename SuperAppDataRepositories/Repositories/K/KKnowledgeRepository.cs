using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;
using SuperAppModels.Utils;
using System.Data;
using System.Text.Json;

namespace SuperAppDataRepositories.Repositories
{
    public class KKnowledgeRepository : IKKnowledgeRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<KKnowledgeRepository> _logger;

        public KKnowledgeRepository(
            ApplicationDbContext context,
            ILogger<KKnowledgeRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<KKnowledgeWithTree?> GetKnowledgeTreeAsync(int knowledgeId, int userId)
        {
            try
            {
                var knowledge = await _context.KKnowledges
                    .Where(k => k.Id == knowledgeId)
                    .FirstOrDefaultAsync();

                if (knowledge == null)
                {
                    _logger.LogWarning("KKnowledge not found: {KnowledgeId}", knowledgeId);
                    return null;
                }

                var nodes = await _context.KNodes
                    .Where(n => n.KnowledgeId == knowledgeId
                    //&& n.DeletedAt == null
                    )
                    .ToListAsync();

                _logger.LogInformation("Found {Count} nodes in knowledge {KnowledgeId}", nodes.Count, knowledgeId);

                // Per-node reviewable count: dueCount + newCount (matches qFlow canReview logic)
                // due  = srsNextReviewAt != null && srsNextReviewAt <= now
                // new  = srsNextReviewAt == null (never reviewed)
                var now = VietnamDateTime.Now();
                var nodeIds = nodes.Select(n => n.Id).ToHashSet();
                var dueCounts = await _context.KQuestions
                    .Where(q => q.NodeId.HasValue
                             && nodeIds.Contains(q.NodeId.Value)
                             && q.StatusCode == "learning"
                             && q.DeletedAt == null
                             && (q.SrsNextReviewAt == null || q.SrsNextReviewAt <= now))
                    .GroupBy(q => q.NodeId!.Value)
                    .Select(g => new { NodeId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.NodeId, x => x.Count);

                return new KKnowledgeWithTree
                {
                    KnowledgeId = knowledge.Id,
                    Name = knowledge.Name,
                    Description = knowledge.Description,
                    UserId = knowledge.UserId,
                    CreatedAt = knowledge.CreatedAt ?? VietnamDateTime.Now(),
                    UpdatedAt = knowledge.UpdatedAt,
                    Nodes = nodes,
                    NodeDueCounts = dueCounts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting knowledge tree for knowledgeId: {KnowledgeId}", knowledgeId);
                throw;
            }
        }

        public async Task<KKnowledge?> GetKnowledgeByIdAsync(int knowledgeId, int userId)
        {
            try
            {
                return await _context.KKnowledges
                    .Where(k => k.Id == knowledgeId)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting knowledge by ID: {KnowledgeId}", knowledgeId);
                throw;
            }
        }

        public async Task<List<KKnowledge>> GetAllKnowledgesByUserIdAsync(int userId, FilterOptions? filterOptions = null)
        {
            try
            {
                var query = _context.KKnowledges.Where(k => k.UserId == userId);

                if (filterOptions != null)
                {
                    if (filterOptions.StatusCodes != null && filterOptions.StatusCodes.Any())
                        query = query.Where(k => k.StatusCode != null && filterOptions.StatusCodes.Contains(k.StatusCode));

                    if (!string.IsNullOrEmpty(filterOptions.DeletedAt))
                    {
                        if (filterOptions.DeletedAt == "null")
                            query = query.Where(k => k.DeletedAt == null);
                        else if (filterOptions.DeletedAt == "notNull")
                            query = query.Where(k => k.DeletedAt != null);
                    }

                    if (filterOptions.CreatedFrom.HasValue)
                        query = query.Where(k => k.CreatedAt >= filterOptions.CreatedFrom.Value);

                    if (filterOptions.CreatedTo.HasValue)
                        query = query.Where(k => k.CreatedAt <= filterOptions.CreatedTo.Value);

                    if (!string.IsNullOrWhiteSpace(filterOptions.SearchText))
                        query = query.Where(k => k.Name.Contains(filterOptions.SearchText) ||
                                                 (k.Description != null && k.Description.Contains(filterOptions.SearchText)));
                }

                return await query.OrderBy(k => k.CreatedAt).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting knowledge list for userId: {UserId}", userId);
                throw;
            }
        }

        public async Task<KKnowledge> CreateAsync(KUpsertKnowledgeRequest request)
        {
            var entity = new KKnowledge(request.Name, request.UserId)
            {
                Description = request.Description,
                ImageBase64 = request.ImageBase64,
            };
            _context.KKnowledges.Add(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created knowledge {Id} for user {UserId}", entity.Id, request.UserId);
            return entity;
        }

        public async Task<KKnowledge?> UpdateAsync(int id, KUpsertKnowledgeRequest request)
        {
            var entity = await _context.KKnowledges
                .FirstOrDefaultAsync(k => k.Id == id && k.UserId == request.UserId);
            if (entity == null) return null;

            entity.Update(request.Name, request.Description, request.ImageBase64);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> SoftDeleteAsync(int id, int userId)
        {
            var entity = await _context.KKnowledges
                .FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId);
            if (entity == null) return false;

            entity.SoftDelete();
            await _context.SaveChangesAsync();
            _logger.LogInformation("Soft-deleted knowledge {Id}", id);
            return true;
        }

        /// <summary>
        /// Hard-deletes nodes (and all descendants) via [k].[sp_DeleteNodes]
        /// </summary>
        public async Task<ResultOptions> DeleteNodesAsync(int knowledgeId, List<int> nodeIds)
        {
            try
            {
                _logger.LogInformation("Deleting {Count} nodes from knowledge {KnowledgeId}", nodeIds.Count, knowledgeId);

                var nodeIdsJson = JsonSerializer.Serialize(nodeIds);

                var knowledgeIdParam = new SqlParameter("@iv_knowledge_id", knowledgeId);
                var nodeIdsParam    = new SqlParameter("@iv_node_ids", SqlDbType.NVarChar, -1) { Value = nodeIdsJson };
                var deletedCountParam = new SqlParameter("@ov_deleted_count", SqlDbType.Int) { Direction = ParameterDirection.Output };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC [k].[sp_DeleteNodes] @iv_knowledge_id, @iv_node_ids, @ov_deleted_count OUTPUT",
                    knowledgeIdParam, nodeIdsParam, deletedCountParam);

                var deletedCount = (int)deletedCountParam.Value;
                _logger.LogInformation("Deleted {Count} nodes (including descendants)", deletedCount);

                return new ResultOptions { Success = true, Message = "Nodes deleted successfully", Reference = deletedCount.ToString(), Status = 200 };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while deleting nodes from knowledge {KnowledgeId}", knowledgeId);
                return new ResultOptions { Success = false, Message = $"Database error: {ex.Message}", Status = 500 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting nodes from knowledge {KnowledgeId}", knowledgeId);
                return new ResultOptions { Success = false, Message = $"Failed to delete nodes: {ex.Message}", Status = 500 };
            }
        }
    }
}
