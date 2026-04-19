using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    public class WikiUpdateKeywordRequest
    {
        /// <summary>New name — if null/empty, name is unchanged.</summary>
        public string? Name { get; set; }
        public string? IconBase64 { get; set; }

        /// <summary>If null, synonyms are left unchanged. If provided (even empty list), synonyms are replaced.</summary>
        public List<string>? Synonyms { get; set; }

        /// <summary>Info IDs to add as links to this keyword.</summary>
        public List<int>? AddInfoIds { get; set; }

        /// <summary>Info IDs to remove from this keyword's links.</summary>
        public List<int>? RemoveInfoIds { get; set; }

        [JsonIgnore]
        public int UserId { get; set; }
    }
}
