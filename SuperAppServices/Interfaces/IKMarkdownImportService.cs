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
    }
}
