using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface IKStatusHistoryRepository
    {
        Task AddQuestionStatusAsync(int questionId, string statusCode, int? userId);
        Task AddQuestionStatusBulkAsync(IEnumerable<(int QuestionId, string StatusCode)> rows, int? userId);
        Task AddNodeStatusAsync(int nodeId, string statusCode, int? userId);

        /// <summary>All question status rows for questions belonging to a knowledge, ordered by ChangedAt ASC.</summary>
        Task<List<KQuestionStatusHistoryEntity>> GetQuestionStatusHistoryByKnowledgeAsync(int knowledgeId);

        /// <summary>All node status rows for nodes belonging to a knowledge, ordered by ChangedAt ASC.</summary>
        Task<List<KNodeStatusHistoryEntity>> GetNodeStatusHistoryByKnowledgeAsync(int knowledgeId);
    }
}
