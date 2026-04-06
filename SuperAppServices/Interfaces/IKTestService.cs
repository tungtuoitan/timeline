using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperAppServices.Interfaces
{
    public interface IKTestService
    {
        Task<List<KTestSummaryResponse>> GetTestsAsync(int knowledgeId, int userId, int? nodeId = null);
        Task<ResultOptions> GetTestDetailAsync(int testId, int knowledgeId, int userId);
        Task<ResultOptions> CreateEmptyTestAsync(int knowledgeId, int userId, KCreateEmptyTestRequest request);
        Task<ResultOptions> SubmitAnswersAsync(int testId, int knowledgeId, int userId, KSubmitAnswersRequest request);
        Task<Dictionary<int, int>> GetQuestionScoresAsync(int knowledgeId, int userId);

        /// <summary>Update test title</summary>
        Task<ResultOptions> UpdateTestAsync(int testId, int knowledgeId, int userId, KUpdateTestRequest request);

        /// <summary>Add/toggle/delete questions in a test</summary>
        Task<ResultOptions> UpdateQuestionsAsync(int testId, int knowledgeId, int userId, KUpdateQuestionsRequest request);

        /// <summary>Persist new column order for tests</summary>
        Task<ResultOptions> ReorderTestsAsync(int knowledgeId, int userId, List<int> orderedTestIds);

        // ── SRS / Daily Review ──────────────────────────────────────────────

        /// <summary>Get the daily review queue: tests with due/new counts.</summary>
        Task<ResultOptions> GetDailyQueueAsync(int knowledgeId, int userId);

        /// <summary>Get global daily queue across all user's knowledges.</summary>
        Task<ResultOptions> GetGlobalDailyQueueAsync(int userId);

        /// <summary>Get questions for a daily review session of a specific test.</summary>
        Task<ResultOptions> GetDailySessionAsync(int testId, int knowledgeId, int userId, int dailyLimit);

        /// <summary>Submit daily review answers, grade with AI, update SRS, check mastered.</summary>
        Task<ResultOptions> SubmitDailyAnswersAsync(int testId, int knowledgeId, int userId, KDailySubmitRequest request);

        /// <summary>Update test status (inactive ↔ learning, manually).</summary>
        Task<ResultOptions> UpdateTestStatusAsync(int testId, int knowledgeId, int userId, string status);
    }
}
