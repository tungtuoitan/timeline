using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperAppServices.Interfaces
{
    public interface IKMarkdownImportService
    {
        /// <summary>
        /// Parse markdown content with AI, create nodes under parentNodeId,
        /// all with statusCode = "draft".
        /// </summary>
        Task<List<KNodeResponse>> ImportAsync(int knowledgeId, int userId, KImportMarkdownRequest request);

        /// <summary>
        /// Create question nodes + tests from structured markdown already parsed on the frontend.
        /// Returns the number of tests created.
        /// </summary>
        Task<int> ImportTestMarkdownAsync(int knowledgeId, int userId, KImportTestMarkdownRequest request);
    }
}
