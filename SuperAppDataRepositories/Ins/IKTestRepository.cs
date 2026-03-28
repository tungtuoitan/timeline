using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface IKTestRepository
    {
        Task<List<KTestSummaryResponse>> GetTestSummariesAsync(int knowledgeId, int userId);
        Task<KTestEntity?> GetTestByIdAsync(int testId, int knowledgeId);
        Task<KTestEntity> CreateTestAsync(KTestEntity test, List<int> nodeIds);
        Task<List<KNodeEntity>> GetQuestionNodesAsync(int knowledgeId, List<int> entityNodeIds, bool includeDescendants);
        Task<List<KNodeEntity>> GetQuestionNodesByIdsAsync(List<int> nodeIds);
        Task SaveSubmissionAsync(int testId, int userId, List<(int NodeId, string? AnswerText, int Point)> results);
        Task<List<KPointHistoryEntity>> GetLatestResultAsync(int testId, int userId);
        Task<List<KPointHistoryEntity>> GetHistoryForNodesAsync(int testId, int userId, List<int> nodeIds);
        Task<Dictionary<int, int>> GetNodeScoresAsync(int knowledgeId, int userId);

        /// <summary>Update test title</summary>
        Task<KTestEntity?> UpdateTestTitleAsync(int testId, int knowledgeId, string title);

        /// <summary>Add k.node IDs as new active test nodes</summary>
        Task AddTestNodesAsync(int testId, List<int> nodeIds);

        /// <summary>Toggle IsActive for the given k.test_node IDs</summary>
        Task ToggleTestNodesActiveAsync(List<int> testNodeIds);

        /// <summary>Permanently delete the given k.test_node IDs from the test</summary>
        Task DeleteTestNodesAsync(List<int> testNodeIds);
    }
}
