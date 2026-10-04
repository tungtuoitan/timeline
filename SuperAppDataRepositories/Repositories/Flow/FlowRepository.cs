using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    public class FlowRepository : IFlowRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FlowRepository> _logger;

        public FlowRepository(ApplicationDbContext context, ILogger<FlowRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ResultOptions> GetEdgesAsync(int userId)
        {
            try
            {
                var edges = await _context.FlowEdges
                    .AsNoTracking()
                    .Where(e => e.UserId == userId && e.DeletedAt == null)
                    .OrderBy(e => e.CreatedAt)
                    .ToListAsync();

                return new ResultOptions { Success = true, Data = edges.Cast<object>().ToList(), Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting flow edges for user {UserId}", userId);
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> UpsertEdgesAsync(List<UpsertFlowEdgeRequest> requests, int userId)
        {
            try
            {
                var results = new List<FlowEdge>();

                foreach (var req in requests)
                {
                    FlowEdge? existing = req.Id.HasValue && req.Id > 0
                        ? await _context.FlowEdges.FirstOrDefaultAsync(e => e.Id == req.Id && e.UserId == userId)
                        : null;

                    // Instant in a JSON body must carry 'Z' or an offset (same rule as the JSON converters).
                    DateTime? parsedDeletedAt = null;
                    if (!string.IsNullOrWhiteSpace(req.DeletedAt))
                    {
                        if (!SuperAppModels.Time.TimeParsing.TryParseInstant(req.DeletedAt, out var deletedUtc))
                            return new ResultOptions { Success = false, Message = $"deletedAt must be ISO 8601 with an offset: '{req.DeletedAt}'", Status = 400 };
                        parsedDeletedAt = deletedUtc;
                    }

                    if (existing != null)
                    {
                        existing.SourceId = req.SourceId;
                        existing.SourceType = req.SourceType;
                        existing.SourceHandle = req.SourceHandle;
                        existing.TargetId = req.TargetId;
                        existing.TargetType = req.TargetType;
                        existing.TargetHandle = req.TargetHandle;
                        existing.Note = req.Note;
                        existing.ArrowDirection = req.ArrowDirection;
                        existing.DeletedAt = parsedDeletedAt;
                        existing.UpdatedAt = DateTime.UtcNow;
                        results.Add(existing);
                    }
                    else
                    {
                        var edge = new FlowEdge
                        {
                            UserId = userId,
                            SourceId = req.SourceId,
                            SourceType = req.SourceType,
                            SourceHandle = req.SourceHandle,
                            TargetId = req.TargetId,
                            TargetType = req.TargetType,
                            TargetHandle = req.TargetHandle,
                            Note = req.Note,
                            ArrowDirection = req.ArrowDirection,
                            DeletedAt = parsedDeletedAt,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                        };
                        _context.FlowEdges.Add(edge);
                        results.Add(edge);
                    }
                }

                await _context.SaveChangesAsync();
                return new ResultOptions { Success = true, Data = results.Cast<object>().ToList(), Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting flow edges for user {UserId}", userId);
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> GetNodePositionsAsync(int userId, List<int>? nodeIds = null, string? nodeType = null)
        {
            try
            {
                var query = _context.FlowNodePositions
                    .AsNoTracking()
                    .Where(p => p.UserId == userId);

                if (nodeIds?.Count > 0)
                    query = query.Where(p => nodeIds.Contains(p.NodeId));

                if (!string.IsNullOrEmpty(nodeType))
                    query = query.Where(p => p.NodeType == nodeType);

                var positions = await query.ToListAsync();
                return new ResultOptions { Success = true, Data = positions.Cast<object>().ToList(), Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting flow node positions for user {UserId}", userId);
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> UpsertNodePositionsAsync(List<UpsertFlowNodePositionRequest> requests, int userId)
        {
            try
            {
                foreach (var req in requests)
                {
                    var existing = await _context.FlowNodePositions
                        .FirstOrDefaultAsync(p => p.UserId == userId && p.NodeId == req.NodeId && p.NodeType == req.NodeType);

                    if (existing != null)
                    {
                        existing.X = req.X;
                        existing.Y = req.Y;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _context.FlowNodePositions.Add(new FlowNodePosition
                        {
                            UserId = userId,
                            NodeId = req.NodeId,
                            NodeType = req.NodeType,
                            X = req.X,
                            Y = req.Y,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return new ResultOptions { Success = true, Message = "Positions saved", Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting flow node positions for user {UserId}", userId);
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }
    }
}
