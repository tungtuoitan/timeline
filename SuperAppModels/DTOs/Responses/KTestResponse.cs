namespace SuperAppModels.DTOs.Responses
{
    public class KQuestionsListResponse
    {
        public int? KnowledgeId { get; set; }
        public List<KQuestionResponse> Questions { get; set; } = [];
    }

    public class KAttachmentResponse
    {
        public int     Id       { get; set; }
        public string  Title    { get; set; } = string.Empty;
        public string  Type     { get; set; } = "code";
        public string? Language { get; set; }
        public string? Content  { get; set; }
        public int     SortOrder { get; set; }
    }

    public class KQuestionResponse
    {
        /// <summary>k.question.id</summary>
        public int    Id         { get; set; }
        /// <summary>k.node.id — null if orphan</summary>
        public int?   NodeId     { get; set; }
        /// <summary>k.node.name — empty string if orphan</summary>
        public string NodeName   { get; set; } = string.Empty;
        /// <summary>k.question.name — the question text</summary>
        public string Question   { get; set; } = string.Empty;
        /// <summary>k.question.description — the expected answer</summary>
        public string? Answer    { get; set; }
        /// <summary>Resolved context code snippet (owned or borrowed from another question).</summary>
        public string? Context   { get; set; }
        /// <summary>Raw context_question_id (null if context is owned or absent). Needed by markdown editor for round-trip.</summary>
        public int? ContextQuestionId { get; set; }
        /// <summary>"learning" = active in review; "draft" = excluded from review sessions</summary>
        public string StatusCode { get; set; } = "learning";
        public int    SortOrder  { get; set; }
        /// <summary>Last ≤10 points (0–5) for this question, oldest→newest</summary>
        public List<int> ScoreHistory { get; set; } = [];
        /// <summary>SRS next review date (null = never reviewed)</summary>
        public DateTime? SrsNextReviewAt { get; set; }
        /// <summary>Current retention 0–100% (forgetting curve)</summary>
        public double Retention { get; set; }
        /// <summary>Non-null when question has been soft-deleted</summary>
        public DateTime? DeletedAt { get; set; }
        /// <summary>Attachments linked to this question (code files from Example/ folder)</summary>
        public List<KAttachmentResponse> Attachments { get; set; } = [];
    }

    public class KQuestionStatusTimelineResponse
    {
        public List<KQuestionStatusTimelinePoint> Days { get; set; } = [];
        /// <summary>Direct COUNT(*) from k.question for today. Compare with today's Master+Learning+Draft+Deleted to detect classification bugs.</summary>
        public int DbTotalToday { get; set; }
    }

    public class KQuestionStatusTimelinePoint
    {
        /// <summary>"yyyy-MM-dd"</summary>
        public string Date     { get; set; } = string.Empty;
        public int    Master   { get; set; }
        public int    Learning { get; set; }
        public int    Draft    { get; set; }
        public int    Deleted  { get; set; }
    }

    public class KSubmitAnswersResultResponse
    {
        public int TotalPoints { get; set; }
        public int MaxPoints { get; set; }
        public int Pct { get; set; }
        public List<KQuestionGradeResponse> Grades { get; set; } = [];
    }

    public class KQuestionGradeResponse
    {
        public int QuestionId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string? AnswerText { get; set; }
        public string? ExpectedAnswer { get; set; }
        public int Point { get; set; }
        /// <summary>Short AI comment, max ~15 words</summary>
        public string? Comment { get; set; }
    }
}
