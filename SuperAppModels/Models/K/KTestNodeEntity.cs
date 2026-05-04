namespace SuperAppModels.Models
{
    /// <summary>
    /// Join table — maps a test to its question nodes.
    /// Replaces the old JSON NodeIds column on KTestEntity.
    /// </summary>
    public class KTestNodeEntity
    {
        public int  Id       { get; set; }
        public int  TestId   { get; set; }
        public int  NodeId   { get; set; }

        /// <summary>Whether this question is active in the test (false = disabled/skipped)</summary>
        public bool IsActive { get; set; } = true;

        // Navigation
        public KTestEntity Test { get; set; } = null!;
    }
}
