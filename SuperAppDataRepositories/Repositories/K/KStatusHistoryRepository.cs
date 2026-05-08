using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.Models;
using SuperAppModels.Utils;

namespace SuperAppDataRepositories.Repositories
{
    public class KStatusHistoryRepository : IKStatusHistoryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<KStatusHistoryRepository> _logger;

        public KStatusHistoryRepository(ApplicationDbContext context, ILogger<KStatusHistoryRepository> logger)
        {
            _context = context;
            _logger  = logger;
        }

        public async Task AddQuestionStatusAsync(int questionId, string statusCode, int? userId)
        {
            _context.KQuestionStatusHistory.Add(new KQuestionStatusHistoryEntity
            {
                QuestionId = questionId,
                StatusCode = statusCode,
                ChangedAt  = VietnamDateTime.Now(),
                UserId     = userId,
            });
            await _context.SaveChangesAsync();
        }

        public async Task AddQuestionStatusBulkAsync(IEnumerable<(int QuestionId, string StatusCode)> rows, int? userId)
        {
            var now = VietnamDateTime.Now();
            var entities = rows.Select(r => new KQuestionStatusHistoryEntity
            {
                QuestionId = r.QuestionId,
                StatusCode = r.StatusCode,
                ChangedAt  = now,
                UserId     = userId,
            }).ToList();
            if (entities.Count == 0) return;
            _context.KQuestionStatusHistory.AddRange(entities);
            await _context.SaveChangesAsync();
        }

        public async Task AddNodeStatusAsync(int nodeId, string statusCode, int? userId)
        {
            _context.KNodeStatusHistory.Add(new KNodeStatusHistoryEntity
            {
                NodeId     = nodeId,
                StatusCode = statusCode,
                ChangedAt  = VietnamDateTime.Now(),
                UserId     = userId,
            });
            await _context.SaveChangesAsync();
        }

        public async Task<List<KQuestionStatusHistoryEntity>> GetQuestionStatusHistoryByKnowledgeAsync(int knowledgeId)
        {
            return await _context.KQuestionStatusHistory
                .Where(h => _context.KQuestions
                    .Where(q => q.Node != null && q.Node.KnowledgeId == knowledgeId)
                    .Select(q => q.Id)
                    .Contains(h.QuestionId))
                .OrderBy(h => h.ChangedAt)
                .ToListAsync();
        }

        public async Task<List<KNodeStatusHistoryEntity>> GetNodeStatusHistoryByKnowledgeAsync(int knowledgeId)
        {
            return await _context.KNodeStatusHistory
                .Where(h => _context.KNodes
                    .Where(n => n.KnowledgeId == knowledgeId)
                    .Select(n => n.Id)
                    .Contains(h.NodeId))
                .OrderBy(h => h.ChangedAt)
                .ToListAsync();
        }
    }
}
