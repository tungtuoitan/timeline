namespace SuperAppModels.Models
{
    /// <summary>
    /// Tracks every status_code change of a k.node row.
    /// One row inserted on creation and on each subsequent update.
    /// </summary>
    public class KNodeStatusHistoryEntity
    {
        public int      Id         { get; set; }
        public int      NodeId     { get; set; }
        /// <summary>"learning" | "draft"</summary>
        public string   StatusCode { get; set; } = "learning";
        public DateTime ChangedAt  { get; set; }
        /// <summary>NULL when inserted by backfill.</summary>
        public int?     UserId     { get; set; }
    }
}
