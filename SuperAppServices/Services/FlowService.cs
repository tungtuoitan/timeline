using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
{
    public class FlowService : IFlowService
    {
        private readonly IFlowRepository _flowRepository;
        private readonly ILogger<FlowService> _logger;

        public FlowService(IFlowRepository flowRepository, ILogger<FlowService> logger)
        {
            _flowRepository = flowRepository;
            _logger = logger;
        }

        public async Task<ResultOptions> GetEdgesAsync(int userId)
        {
            try { return await _flowRepository.GetEdgesAsync(userId); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FlowService.GetEdgesAsync");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> UpsertEdgesAsync(List<UpsertFlowEdgeRequest> requests, int userId)
        {
            try { return await _flowRepository.UpsertEdgesAsync(requests, userId); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FlowService.UpsertEdgesAsync");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> GetNodePositionsAsync(int userId, List<int>? nodeIds, string? nodeType)
        {
            try { return await _flowRepository.GetNodePositionsAsync(userId, nodeIds, nodeType); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FlowService.GetNodePositionsAsync");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> UpsertNodePositionsAsync(List<UpsertFlowNodePositionRequest> requests, int userId)
        {
            try { return await _flowRepository.UpsertNodePositionsAsync(requests, userId); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FlowService.UpsertNodePositionsAsync");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }
    }
}
