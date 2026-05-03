namespace SuperAppModels.DTOs.Responses
{
    public class KQuestionsListResponse
    {
        public int? KnowledgeId { get; set; }
        public List<KQuestionResponse> Questions { get; set; } = [];
    }

    public class KQuestionResponse
    {
        /// <summary>k.question.id</summary>
        public int    Id         { get; set; }
        /// <summary>k.question.name — the question text</summary>
        public string Question   { get; set; } = string.Empty;
        /// <summary>k.question.description — the expected answer</summary>
        public string? Answer    { get; set; }
        /// <summary>Whether this question is active (false = disabled)</summary>
        public bool   IsActive   { get; set; } = true;
        /// <summary>True when the question is in draft state and excluded from review sessions</summary>
        public bool   IsDraft    { get; set; } = false;
        public int    SortOrder  { get; set; }
        /// <summary>Last ≤10 points (0–5) for this question, oldest→newest</summary>
        public List<int> ScoreHistory { get; set; } = [];
        /// <summary>SRS next review date (null = never reviewed)</summary>
        public DateTime? SrsNextReviewAt { get; set; }
        /// <summary>Current retention 0–100% (forgetting curve)</summary>
        public double Retention { get; set; }
        /// <summary>Non-null when question has been soft-deleted</summary>
        public DateTime? DeletedAt { get; set; }
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
