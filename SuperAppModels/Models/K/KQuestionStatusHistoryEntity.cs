namespace SuperAppModels.Models
{
    /// <summary>
    /// Tracks every status_code change of a k.question row.
    /// One row inserted on creation and on each subsequent toggle / bulk update.
    /// </summary>
    public class KQuestionStatusHistoryEntity
    {
        public int      Id         { get; set; }
        public int      QuestionId { get; set; }
        /// <summary>"learning" | "draft"</summary>
        public string   StatusCode { get; set; } = "learning";
        public DateTime ChangedAt  { get; set; }
        /// <summary>NULL when inserted by backfill.</summary>
        public int?     UserId     { get; set; }
    }
}
