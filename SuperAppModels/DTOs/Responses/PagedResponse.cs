namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// Paginated response wrapper for list endpoints
    /// </summary>
    /// <typeparam name="T">The type of items in the list</typeparam>
    public class PagedResponse<T>
    {
        /// <summary>
        /// List of items for the current page
        /// </summary>
        public IList<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        /// <summary>
        /// Indicates if there are more pages after the current page
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>
        /// Indicates if there are previous pages before the current page
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>
        /// Number of items on the current page
        /// </summary>
        public int CurrentPageCount => Items.Count;

        /// <summary>
        /// Creates a paginated response
        /// </summary>
        public static PagedResponse<T> Create(
            IList<T> items, 
            int pageNumber, 
            int pageSize, 
            int totalCount)
        {
            return new PagedResponse<T>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        /// <summary>
        /// Creates an empty paginated response
        /// </summary>
        public static PagedResponse<T> Empty(int pageNumber = 1, int pageSize = 20)
        {
            return new PagedResponse<T>
            {
                Items = new List<T>(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = 0
            };
        }
    }
}