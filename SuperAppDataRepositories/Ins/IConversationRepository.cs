using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface IConversationRepository
    {
        Task<ResultOptions> GetTopicsAsync(TopicFilterOptions filter);
        Task<ResultOptions> UpsertTopicAsync(ConTopic topic);
        Task<ResultOptions> GetMessagesAsync(MessageFilterOptions filter);
        Task<ResultOptions> UpsertMessageAsync(ConMessage message);
        Task<ResultOptions> PromoteMessageToTopicAsync(int messageId, int userId, string topicName);
    }
}
