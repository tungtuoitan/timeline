using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request model for updating user filter preferences
    /// </summary>
    public class UpdateFiltersRequest
    {
        /// <summary>
        /// User filter preferences (JSON string)
        /// Example: {"noteGrid":{"statusCode":"active","deletedAt":"null"},"wsGrid":{"statusCode":"active","deletedAt":"null"}}
        /// </summary>
        [Required(ErrorMessage = "Filters are required")]
        public string Filters { get; set; } = string.Empty;
    }
}
