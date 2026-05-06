using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface IKQuestionRepository
    {
        // ── Questions CRUD ─────────────────────────────────────────────────────

        Task<List<KQuestionEntity>> GetQuestionsByNodeAsync(int nodeId);
        Task<List<KQuestionEntity>> GetAllQuestionsByKnowledgeAsync(int knowledgeId);
        Task<List<KQuestionEntity>> GetOrphanQuestionsAsync();
        Task<List<KQuestionEntity>> GetQuestionsByIdsAsync(List<int> questionIds);
        Task AddQuestionsAsync(int? nodeId, List<KNewQuestionItem> questions);
        Task UpdateQuestionsDataAsync(List<KUpdateQuestionItem> updates);
        Task DeleteQuestionsAsync(List<int> questionIds);
        Task RestoreQuestionsAsync(List<int> questionIds);
        Task ResetQuestionsSrsAsync(List<int> questionIds);
        Task ToggleQuestionsDraftAsync(List<int> questionIds);
        Task MarkQuestionDraftAsync(int questionId);
        Task MoveQuestionAsync(int questionId, int? targetNodeId);

        // ── Point history ──────────────────────────────────────────────────────

        Task SaveSubmissionAsync(int? nodeId, int userId, List<(int QuestionId, string? AnswerText, int Point)> results);
        Task SaveDailySubmissionAsync(int? knowledgeId, int userId, List<(int QuestionId, string? AnswerText, int Point, int? ResponseTimeMs)> results);
        Task<List<KPointHistoryEntity>> GetHistoryForQuestionsAsync(int userId, List<int> questionIds);
        Task<List<KPointHistoryEntity>> GetAllHistoryForQuestionsAsync(List<int> questionIds);
        Task<Dictionary<int, int>> GetQuestionScoresAsync(int nodeId, int userId);

        // ── SRS ────────────────────────────────────────────────────────────────

        Task UpdateQuestionsStatusAsync(List<int> questionIds, string statusCode);
        Task UpdateQuestionSrsAsync(int questionId, int interval, double easeFactor, int repetitions, DateTime? nextReviewAt);
        Task<List<KSubmissionGroup>> GetRecentSubmissionGroupsAsync(int knowledgeId, int userId, int count);

        // ── Daily review ───────────────────────────────────────────────────────

        Task<List<KDailyQueueItem>> GetDailyQueueAsync(int knowledgeId, int userId);
        Task<List<KDailyQueueItem>> GetGlobalDailyQueueAsync(int userId);
        Task<List<KQuestionEntity>> GetDailySessionQuestionsAsync(int knowledgeId, int dailyLimit, double newRatio);

        // ── Retention ──────────────────────────────────────────────────────────

        Task<List<KQuestionEntity>> GetActiveQuestionsAsync(int knowledgeId);
        Task<string?> GetNodeNameAsync(int nodeId);
    }

    /// <summary>Daily queue summary per knowledge.</summary>
    public class KDailyQueueItem
    {
        public int    KnowledgeId   { get; set; }
        public string KnowledgeName { get; set; } = string.Empty;
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
