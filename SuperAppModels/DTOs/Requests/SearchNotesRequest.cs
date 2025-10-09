using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request model for searching and filtering notes with pagination support
    /// </summary>
    public class SearchNotesRequest
    {
        [StringLength(200, ErrorMessage = "Search text cannot exceed 200 characters")]
        public string? SearchText { get; set; }

        [StringLength(100, ErrorMessage = "Type filter cannot exceed 100 characters")]
        public string? Type { get; set; }

        [StringLength(500, ErrorMessage = "Tags filter cannot exceed 500 characters")]
        public string? Tags { get; set; }

        public bool IncludeArchived { get; set; } = false;

        [Range(1, int.MaxValue, ErrorMessage = "Page number must be at least 1")]
        public int PageNumber { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
        public int PageSize { get; set; } = 20;

        [StringLength(50, ErrorMessage = "Sort by field cannot exceed 50 characters")]
        public string SortBy { get; set; } = "CreatedAt";

        [StringLength(10, ErrorMessage = "Sort direction must be 'asc' or 'desc'")]
        public string SortDirection { get; set; } = "desc";

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}