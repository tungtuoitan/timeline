using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _repo;
        private readonly ILogger<ConversationService> _logger;

        public ConversationService(IConversationRepository repo, ILogger<ConversationService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public Task<ResultOptions> GetTopicsAsync(TopicFilterOptions filter) =>
            _repo.GetTopicsAsync(filter);

        public async Task<ResultOptions> UpsertTopicAsync(UpsertTopicRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return new ResultOptions { Success = false, Message = "Topic name is required", Status = 400 };

            var topic = new ConTopic
            {
                Id = req.Id,
                UserId = req.UserId,
                EntityType = req.EntityType,
                EntityId = req.EntityId,
                Name = req.Name.Trim(),
                Description = req.Description,
                DeletedAt = req.DeletedAt,
            };
            return await _repo.UpsertTopicAsync(topic);
        }

        public Task<ResultOptions> GetMessagesAsync(MessageFilterOptions filter) =>
            _repo.GetMessagesAsync(filter);

        public async Task<ResultOptions> UpsertMessageAsync(UpsertMessageRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Content) && string.IsNullOrWhiteSpace(req.Title))
                return new ResultOptions { Success = false, Message = "Message must have content or title", Status = 400 };

            var message = new ConMessage
            {
                Id = req.Id,
                UserId = req.UserId,
                TopicId = req.TopicId,
                EntityType = req.EntityType,
                EntityId = req.EntityId,
                ParentId = req.ParentId,
                Type = req.Type,
                Title = req.Title,
                Content = req.Content,
                TrackId = req.TrackId,
                Location = req.Location,
                OccurAt = req.OccurAt,
                IsSensitive = req.IsSensitive,
                DeletedAt = req.DeletedAt,
            };
            return await _repo.UpsertMessageAsync(message);
        }

        public async Task<ResultOptions> PromoteMessageToTopicAsync(int messageId, int userId, string topicName)
        {
            if (string.IsNullOrWhiteSpace(topicName))
                return new ResultOptions { Success = false, Message = "Topic name is required", Status = 400 };
            return await _repo.PromoteMessageToTopicAsync(messageId, userId, topicName.Trim());
        }
    }
}
