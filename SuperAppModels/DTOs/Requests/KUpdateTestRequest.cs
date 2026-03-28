using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>Update test title</summary>
    public class KUpdateTestRequest
    {
        [Required]
        [StringLength(500, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;
    }
}
