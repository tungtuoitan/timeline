using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface IKTestRepository
    {
        Task<List<KTestSummaryResponse>> GetTestSummariesAsync(int knowledgeId, int userId, int? nodeId = null);
        Task<KTestEntity?> GetTestByIdAsync(int testId, int knowledgeId);
        Task<KTestEntity> CreateTestAsync(KTestEntity test, List<(string Name, string? Description)> questions);

        // Questions (k.question)
        Task<List<KQuestionEntity>> GetQuestionsByIdsAsync(List<int> questionIds);
        Task AddQuestionsAsync(int testId, List<KNewQuestionItem> questions);
        Task UpdateQuestionsDataAsync(List<KUpdateQuestionItem> updates);
        Task ToggleQuestionsActiveAsync(List<int> questionIds);
        Task DeleteQuestionsAsync(List<int> questionIds);
        Task RestoreQuestionsAsync(List<int> questionIds);
        Task ResetQuestionsSrsAsync(List<int> questionIds);

        // Point history
        Task SaveSubmissionAsync(int testId, int userId, List<(int QuestionId, string? AnswerText, int Point)> results);
        Task<List<KPointHistoryEntity>> GetLatestResultAsync(int testId, int userId);
        Task<List<KPointHistoryEntity>> GetHistoryForQuestionsAsync(int testId, int userId, List<int> questionIds);
        Task<Dictionary<int, int>> GetQuestionScoresAsync(int knowledgeId, int userId);

        /// <summary>Update test title</summary>
        Task<KTestEntity?> UpdateTestTitleAsync(int testId, int knowledgeId, string title);

        /// <summary>Move test to a different node. Sets SortOrder=0 and bumps siblings.</summary>
        Task<KTestEntity?> MoveTestToNodeAsync(int testId, int knowledgeId, int? nodeId);

        /// <summary>Set sort_order for tests in the given ordered list of IDs</summary>
        Task ReorderTestsAsync(int knowledgeId, List<int> orderedTestIds);

        // ── SRS / Daily Review ─────────────────────────────────────────────────

        /// <summary>Get daily queue: tests with due/new question counts for learning/mastered tests.</summary>
        Task<List<KDailyQueueItem>> GetDailyQueueAsync(int knowledgeId, int userId);

        /// <summary>Get global daily queue across ALL user's knowledges.</summary>
        Task<List<KDailyQueueItem>> GetGlobalDailyQueueAsync(int userId);

        /// <summary>Get questions for a daily session (due + new, up to limit).</summary>
        Task<List<KQuestionEntity>> GetDailySessionQuestionsAsync(int testId, int dailyLimit, double newRatio);

        /// <summary>Update SRS state on a question after grading.</summary>
        Task UpdateQuestionSrsAsync(int questionId, int interval, double easeFactor, int repetitions, DateTime? nextReviewAt);

        /// <summary>Save submission with response time.</summary>
        Task SaveDailySubmissionAsync(int testId, int userId, List<(int QuestionId, string? AnswerText, int Point, int? ResponseTimeMs)> results);

        /// <summary>Update test status (inactive/learning/mastered).</summary>
        Task UpdateTestStatusAsync(int testId, string status);

        /// <summary>Get last N submission groups for a test (for mastered check).</summary>
        Task<List<KSubmissionGroup>> GetRecentSubmissionGroupsAsync(int testId, int userId, int count);

        /// <summary>Get all active questions grouped by test for a knowledge (for retention calculation).</summary>
        Task<List<(KTestEntity Test, List<KQuestionEntity> Questions)>> GetTestsWithQuestionsAsync(int knowledgeId);

        /// <summary>Get all point history for given question IDs, ordered by CreatedAt asc.</summary>
        Task<List<KPointHistoryEntity>> GetAllHistoryForQuestionsAsync(List<int> questionIds);
    }

    /// <summary>Daily queue aggregation per test.</summary>
    public class KDailyQueueItem
    {
        public int    TestId        { get; set; }
        public int    KnowledgeId   { get; set; }
        public string KnowledgeName { get; set; } = string.Empty;
        public string Title         { get; set; } = string.Empty;
        public int    Level         { get; set; }
        public string? Status       { get; set; }
        public int    DueCount      { get; set; }
        public int    NewCount      { get; set; }
        public int    ActiveCount   { get; set; }
    }

    /// <summary>A submission group (all answers from a single session).</summary>
    public class KSubmissionGroup
    {
        public DateTime SessionTime     { get; set; }
        public double   AvgPoint        { get; set; }
        public double   AvgSpeedRatio   { get; set; }
    }
}
