using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperAppServices.Interfaces
{
    public interface IKQuestionService
    {
        // ── CRUD ───────────────────────────────────────────────────────────────

        /// <summary>Get all questions for a node.</summary>
        Task<ResultOptions> GetQuestionsAsync(int knowledgeId, int userId);

        /// <summary>Get all orphan questions (node_id IS NULL).</summary>
        Task<ResultOptions> GetOrphanQuestionsAsync(int userId);

        /// <summary>Batch add/update/delete/toggle orphan questions (node_id = null).</summary>
        Task<ResultOptions> UpdateOrphanQuestionsAsync(int userId, KUpdateQuestionsRequest request);

        /// <summary>Batch add/update/delete/toggle questions.</summary>
        Task<ResultOptions> UpdateQuestionsAsync(int knowledgeId, int userId, KUpdateQuestionsRequest request);

        /// <summary>Move a question to a different node (or make it orphan when targetNodeId is null).</summary>
        Task<ResultOptions> MoveQuestionAsync(int questionId, int? targetNodeId, int userId);

        /// <summary>Submit answers with AI grading and SRS update.</summary>
        Task<ResultOptions> SubmitAnswersAsync(int knowledgeId, int userId, KSubmitAnswersRequest request);

        /// <summary>Latest score per question (used by flow canvas).</summary>
        Task<Dictionary<int, int>> GetQuestionScoresAsync(int knowledgeId, int userId);

        // ── SRS / Daily Review ─────────────────────────────────────────────────

        /// <summary>Get daily review queue for a single knowledge.</summary>
        Task<ResultOptions> GetDailyQueueAsync(int knowledgeId, int userId);

        /// <summary>Get global daily queue across all user's knowledges.</summary>
        Task<ResultOptions> GetGlobalDailyQueueAsync(int userId);

        /// <summary>Get questions for a daily review session.</summary>
        Task<ResultOptions> GetDailySessionAsync(int knowledgeId, int userId, int dailyLimit);

        /// <summary>Submit daily review answers, update SRS.</summary>
        Task<ResultOptions> SubmitDailyAnswersAsync(int knowledgeId, int userId, KDailySubmitRequest request);

        // ── Retention ──────────────────────────────────────────────────────────

        /// <summary>Get overall retention summary for a knowledge.</summary>
        Task<KRetentionSummaryResponse> GetRetentionSummaryAsync(int knowledgeId);

        /// <summary>Get per-question retention graph over the last N days.</summary>
        Task<KRetentionGraphResponse> GetRetentionGraphAsync(int knowledgeId, int days);
    }
}
