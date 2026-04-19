using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    public class WikiUpsertInfoRequest
    {
        public int? Id { get; set; }

        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        [JsonIgnore]
        public int UserId { get; set; }
    }
}
