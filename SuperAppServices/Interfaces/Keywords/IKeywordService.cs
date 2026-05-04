using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperAppServices.Interfaces
{
    /// <summary>
    /// Service interface for keyword operations
    /// Handles both internal keywords (workspaces/folders/notes/headings) and external keywords
    /// </summary>
    public interface IKeywordService
    {
        /// <summary>
        /// Get all keywords for a user (internal + external)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="workspaceIds">Optional comma-separated workspace IDs to filter (e.g., "1,2,3")</param>
        /// <returns>List of all keywords</returns>
        Task<List<KeywordDto>> GetAllKeywordsAsync(int userId, string? workspaceIds = null);

        /// <summary>
        /// Upsert external keywords (batch operation)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="requests">List of upsert requests</param>
        /// <returns>Result with success/failure details</returns>
        Task<ResultOptions> UpsertExternalKeywordsAsync(int userId, List<UpsertExternalKeywordRequest> requests);
    }
}
