using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    public class KCreateEmptyTestRequest
    {
        [Required]
        public string Title  { get; set; } = string.Empty;
        public int?   NodeId { get; set; }
    }
}
