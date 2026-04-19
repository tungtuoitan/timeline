using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    public class WikiCreateKeywordRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public List<string> Synonyms { get; set; } = new();

        [JsonIgnore]
        public int UserId { get; set; }
    }
}
