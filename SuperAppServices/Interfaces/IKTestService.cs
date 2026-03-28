using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperAppServices.Interfaces
{
    public interface IKTestService
    {
        Task<List<KTestSummaryResponse>> GetTestsAsync(int knowledgeId, int userId);
        Task<ResultOptions> GetTestDetailAsync(int testId, int knowledgeId, int userId);
        Task<ResultOptions> CreateTestFromNodesAsync(int knowledgeId, int userId, KCreateTestFromNodesRequest request);
        Task<ResultOptions> SubmitAnswersAsync(int testId, int knowledgeId, int userId, KSubmitAnswersRequest request);
        Task<Dictionary<int, int>> GetNodeScoresAsync(int knowledgeId, int userId);

        /// <summary>Update test title</summary>
        Task<ResultOptions> UpdateTestAsync(int testId, int knowledgeId, int userId, KUpdateTestRequest request);

        /// <summary>Add new question nodes and/or toggle isActive on existing test nodes</summary>
        Task<ResultOptions> UpdateTestNodesAsync(int testId, int knowledgeId, int userId, KUpdateTestNodesRequest request);
    }
}
