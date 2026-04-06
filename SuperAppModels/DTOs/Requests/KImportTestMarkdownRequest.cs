using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request for structured test markdown import.
    /// POST /api/k/{knowledgeId}/import-test-markdown
    ///
    /// Markdown structure parsed on the frontend:
    ///   # keyword  — must match the selected node (validation done on frontend)
    ///   ## name    — defines a test
    ///   ### q?     — question node; answer = plain text below
    /// </summary>
    public class KImportTestMarkdownRequest
    {
        /// <summary>The node under which question nodes will be created.</summary>
        [Required]
        public int ParentNodeId { get; set; }

        /// <summary>New tests to create — each becomes a KTestEntity + KTestNodeEntity rows.</summary>
        public List<KMdTestItem> Tests { get; set; } = [];

        /// <summary>Questions with no parent test — only question nodes are created.</summary>
        public List<KMdQuestionItem> OrphanQuestions { get; set; } = [];

        /// <summary>Questions to add into existing tests (by testId).</summary>
        public List<KMdExistingTestAddition> ExistingTestAdditions { get; set; } = [];
    }

    public class KMdExistingTestAddition
    {
        public int TestId { get; set; }
        public List<KMdQuestionItem> Questions { get; set; } = [];
    }

    public class KMdTestItem
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public List<KMdQuestionItem> Questions { get; set; } = [];
    }

    public class KMdQuestionItem
    {
        [Required]
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }
}
