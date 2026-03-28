using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request for AI-powered markdown → nodes import.
    /// POST /api/k/{knowledgeId}/nodes/import-markdown
    /// </summary>
    public class KImportMarkdownRequest
    {
        /// <summary>Free-form markdown content. Headings → node hierarchy.</summary>
        [Required]
        public string Markdown { get; set; } = string.Empty;

        /// <summary>Parent node ID (null = root of knowledge)</summary>
        public int? ParentNodeId { get; set; }
    }
}
