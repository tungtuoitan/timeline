using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;

namespace SuperAppServices.Interfaces
{
    public interface IConversationService
    {
        Task<ResultOptions> GetTopicsAsync(TopicFilterOptions filter);
        Task<ResultOptions> UpsertTopicAsync(UpsertTopicRequest request);
        Task<ResultOptions> GetMessagesAsync(MessageFilterOptions filter);
        Task<ResultOptions> UpsertMessageAsync(UpsertMessageRequest request);
        Task<ResultOptions> PromoteMessageToTopicAsync(int messageId, int userId, string topicName);
    }
}
