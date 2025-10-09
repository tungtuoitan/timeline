using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request model for searching/filtering notes
    /// </summary>
    public class SearchNotesRequest
    {
        /// <summary>
        /// Search text to filter notes by name or description
        /// </summary>
        [StringLength(200, ErrorMessage = "Search text cannot exceed 200 characters")]
        public string? SearchText { get; set; }

        /// <summary>
        /// Filter by note type
        /// </summary>
        [StringLength(100, ErrorMessage = "Type filter cannot exceed 100 characters")]
        public string? Type { get; set; }

        /// <summary>
        /// Filter by tags (comma-separated)
        /// </summary>
        [StringLength(500, ErrorMessage = "Tags filter cannot exceed 500 characters")]
        public string? Tags { get; set; }

        /// <summary>
        /// Include archived notes in search
        /// </summary>
        public bool IncludeArchived { get; set; } = false;

        /// <summary>
        /// Page number for pagination (1-based)
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Page number must be at least 1")]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Page size for pagination
        /// </summary>
        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Sort field (Name, CreatedAt, UpdatedAt)
        /// </summary>
        [StringLength(50, ErrorMessage = "Sort by field cannot exceed 50 characters")]
        public string SortBy { get; set; } = "CreatedAt";

        /// <summary>
        /// Sort direction (asc, desc)
        /// </summary>
        [StringLength(10, ErrorMessage = "Sort direction must be 'asc' or 'desc'")]
        public string SortDirection { get; set; } = "desc";

        /// <summary>
        /// Date range filter - from date
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Date range filter - to date
        /// </summary>
        public DateTime? ToDate { get; set; }
    }
}