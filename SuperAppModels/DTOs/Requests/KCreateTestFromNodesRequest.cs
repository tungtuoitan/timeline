using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Create a test by pulling question nodes (nodeType="question") under the selected entity nodes.
    /// The test is persisted; a first attempt is started immediately.
    /// </summary>
    public class KCreateTestFromNodesRequest
    {
        /// <summary>Test title — defaults to today's date if empty</summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>Difficulty level: 1 = Easy, 2 = Medium, 3 = Hard</summary>
        [Range(1, 3)]
        [JsonPropertyName("level")]
        public int Level { get; set; } = 1;

        /// <summary>Entity node IDs to pull question nodes from</summary>
        [Required]
        [JsonPropertyName("nodeIds")]
        public List<int> NodeIds { get; set; } = [];

        /// <summary>When true, include question nodes from all descendants; when false, direct children only</summary>
        [JsonPropertyName("includeDescendants")]
        public bool IncludeDescendants { get; set; } = true;

        /// <summary>Max number of question nodes to include (shuffle + sample). Use int.MaxValue to include all.</summary>
        [Range(1, int.MaxValue)]
        [JsonPropertyName("count")]
        public int Count { get; set; } = int.MaxValue;
    }
}
