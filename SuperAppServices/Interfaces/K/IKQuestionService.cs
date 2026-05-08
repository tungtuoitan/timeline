using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperAppServices.Interfaces
{
    public interface IKQuestionService
    {
        // ── CRUD ───────────────────────────────────────────────────────────────

        /// <summary>Get all questions for a specific node (WHERE NodeId == nodeId).</summary>
        Task<ResultOptions> GetNodeQuestionsAsync(int nodeId, int userId);

        /// <summary>Get all questions across all nodes in a knowledge (WHERE Node.KnowledgeId == knowledgeId).</summary>
        Task<ResultOptions> GetKnowledgeQuestionsAsync(int knowledgeId, int userId);

        /// <summary>Get all orphan questions (node_id IS NULL).</summary>
        Task<ResultOptions> GetOrphanQuestionsAsync(int userId);

        /// <summary>Batch add/update/delete/toggle orphan questions (node_id = null).</summary>
        Task<ResultOptions> UpdateOrphanQuestionsAsync(int userId, KUpdateQuestionsRequest request);

        /// <summary>Batch add/update/delete/toggle questions for a node.</summary>
        Task<ResultOptions> UpdateQuestionsAsync(int nodeId, int userId, KUpdateQuestionsRequest request);

        /// <summary>Move a question to a different node (or make it orphan when targetNodeId is null).</summary>
        Task<ResultOptions> MoveQuestionAsync(int questionId, int? targetNodeId, int userId);

        /// <summary>Mark a question as draft and clear all its review history (point_history + SRS reset).</summary>
        Task<ResultOptions> MarkQuestionDraftAsync(int questionId);

        /// <summary>Submit answers with AI grading and SRS update.</summary>
        Task<ResultOptions> SubmitAnswersAsync(int nodeId, int userId, KSubmitAnswersRequest request);

        /// <summary>Latest score per question (used by flow canvas).</summary>
        Task<Dictionary<int, int>> GetQuestionScoresAsync(int nodeId, int userId);

        // ── SRS / Daily Review ─────────────────────────────────────────────────

        /// <summary>Get daily review queue for a node.</summary>
        Task<ResultOptions> GetDailyQueueAsync(int nodeId, int userId);

        /// <summary>Get global daily queue across all user's knowledges.</summary>
        Task<ResultOptions> GetGlobalDailyQueueAsync(int userId);

        /// <summary>Get questions for a daily review session of a node.</summary>
        Task<ResultOptions> GetDailySessionAsync(int nodeId, int userId, int dailyLimit);

        /// <summary>Get questions for a knowledge-scoped daily review session.</summary>
        Task<ResultOptions> GetKnowledgeDailySessionAsync(int nodeId, int userId, int dailyLimit);

        /// <summary>Submit daily review answers, update SRS.</summary>
        Task<ResultOptions> SubmitDailyAnswersAsync(int nodeId, int userId, KDailySubmitRequest request);

        // ── Retention ──────────────────────────────────────────────────────────

        /// <summary>Get overall retention summary for a node.</summary>
        Task<KRetentionSummaryResponse> GetRetentionSummaryAsync(int nodeId);

        /// <summary>Get per-question retention graph over the last N days.</summary>
        Task<KRetentionGraphResponse> GetRetentionGraphAsync(int nodeId, int days);

        // ── Question status timeline (Progress dashboard) ──────────────────────

        /// <summary>Get classification timeline of every question of a knowledge into master / learning / draft / deleted, day by day.</summary>
        Task<ResultOptions> GetQuestionStatusTimelineAsync(int knowledgeId, int userId);
    }
}
