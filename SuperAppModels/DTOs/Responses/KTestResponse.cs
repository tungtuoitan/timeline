namespace SuperAppModels.DTOs.Responses
{
    public class KTestSummaryResponse
    {
        public int Id { get; set; }
        public int KnowledgeId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Level { get; set; }
        public string? Mode { get; set; }
        public string? Status { get; set; }
        public int NodeCount { get; set; }
        /// <summary>Number of questions with IsActive = true</summary>
        public int ActiveCount { get; set; }
        public int? LastTotalPoints { get; set; }
        public int? LastMaxPoints { get; set; }
        public int? LastPct { get; set; }
        public DateTime? LastSubmittedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int SortOrder { get; set; }
        /// <summary>Last 10 submission percentages (oldest→newest) for sparkline chart</summary>
        public List<int> ScoreHistory { get; set; } = [];
    }

    public class KTestDetailResponse
    {
        public int Id { get; set; }
        public int KnowledgeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Level { get; set; }
        public string? Mode { get; set; }
        public List<KTestQuestionResponse> Questions { get; set; } = [];
        public DateTime? CreatedAt { get; set; }
    }

    public class KTestQuestionResponse
    {
        /// <summary>k.question.id</summary>
        public int    Id         { get; set; }
        /// <summary>k.question.name — the question text</summary>
        public string Question   { get; set; } = string.Empty;
        /// <summary>k.question.description — the expected answer</summary>
        public string? Answer    { get; set; }
        /// <summary>Whether this question is active (false = disabled)</summary>
        public bool   IsActive   { get; set; } = true;
        public int    SortOrder  { get; set; }
        /// <summary>Last ≤10 points (0–5) for this question, oldest→newest</summary>
        public List<int> ScoreHistory { get; set; } = [];
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
