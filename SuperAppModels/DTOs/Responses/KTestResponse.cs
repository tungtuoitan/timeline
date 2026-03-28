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
        public int NodeCount { get; set; }
        /// <summary>Number of nodes with IsActive = true</summary>
        public int ActiveCount { get; set; }
        public int? LastTotalPoints { get; set; }
        public int? LastMaxPoints { get; set; }
        public int? LastPct { get; set; }
        public DateTime? LastSubmittedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
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
        /// <summary>k.test_node.id — used to toggle isActive</summary>
        public int    TestNodeId { get; set; }
        public int    NodeId     { get; set; }
        /// <summary>k.node.name — the question text</summary>
        public string Question   { get; set; } = string.Empty;
        /// <summary>k.node.description — the expected answer</summary>
        public string? Answer    { get; set; }
        /// <summary>Whether this question is active (false = disabled)</summary>
        public bool   IsActive   { get; set; } = true;
        /// <summary>Last ≤10 points (0–5) for this node, oldest→newest</summary>
        public List<int> ScoreHistory { get; set; } = [];
    }

    public class KSubmitAnswersResultResponse
    {
        public int TotalPoints { get; set; }
        public int MaxPoints { get; set; }
        public int Pct { get; set; }
        public List<KNodeGradeResponse> Grades { get; set; } = [];
    }

    public class KNodeGradeResponse
    {
        public int NodeId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string? AnswerText { get; set; }
        public string? ExpectedAnswer { get; set; }
        public int Point { get; set; }
        /// <summary>Short AI comment, max ~15 words</summary>
        public string? Comment { get; set; }
    }
}
