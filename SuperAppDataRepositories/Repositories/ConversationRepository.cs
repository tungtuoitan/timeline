using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ConversationRepository> _logger;

        public ConversationRepository(ApplicationDbContext context, ILogger<ConversationRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ResultOptions> GetTopicsAsync(TopicFilterOptions filter)
        {
            try
            {
                var query = _context.ConTopics.AsNoTracking()
                    .Where(t => t.UserId == filter.UserId);

                if (filter.EntityType != null)
                    query = query.Where(t => t.EntityType == filter.EntityType);

                if (filter.EntityId.HasValue)
                    query = query.Where(t => t.EntityId == filter.EntityId);

                if (filter.DeletedAt == "null")
                    query = query.Where(t => t.DeletedAt == null);
                else if (filter.DeletedAt == "notNull")
                    query = query.Where(t => t.DeletedAt != null);

                var topics = await query.OrderBy(t => t.CreatedAt).ToListAsync();
                return new ResultOptions { Success = true, Data = topics.Cast<object>().ToList(), Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting topics");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> UpsertTopicAsync(ConTopic topic)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (topic.Id > 0)
                    {
                        var existing = await _context.ConTopics.FindAsync(topic.Id);
                        if (existing == null || existing.UserId != topic.UserId)
                            return new ResultOptions { Success = false, Message = "Topic not found", Status = 404 };

                        existing.Name = topic.Name;
                        existing.Description = topic.Description;
                        existing.DeletedAt = topic.DeletedAt;
                        existing.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        await tx.CommitAsync();
                        return new ResultOptions { Success = true, Object = existing, Status = 200 };
                    }
                    else
                    {
                        topic.CreatedAt = DateTime.UtcNow;
                        topic.UpdatedAt = DateTime.UtcNow;
                        _context.ConTopics.Add(topic);
                        await _context.SaveChangesAsync();
                        await tx.CommitAsync();
                        return new ResultOptions { Success = true, Object = topic, Status = 200 };
                    }
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    _logger.LogError(ex, "Error upserting topic");
                    return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
                }
            });
        }

        public async Task<ResultOptions> GetMessagesAsync(MessageFilterOptions filter)
        {
            try
            {
                var query = _context.ConMessages.AsNoTracking()
                    .Where(m => m.UserId == filter.UserId && m.ParentId == null); // top-level only

                if (filter.EntityType != null)
                    query = query.Where(m => m.EntityType == filter.EntityType);

                if (filter.EntityId.HasValue)
                    query = query.Where(m => m.EntityId == filter.EntityId);

                if (filter.EntityLevelOnly)
                    query = query.Where(m => m.TopicId == null);
                else if (filter.TopicId.HasValue)
                    query = query.Where(m => m.TopicId == filter.TopicId);

                if (filter.DeletedAt == "null")
                    query = query.Where(m => m.DeletedAt == null);
                else if (filter.DeletedAt == "notNull")
                    query = query.Where(m => m.DeletedAt != null);

                var messages = await query.OrderBy(m => m.CreatedAt).ToListAsync();

                // Load replies for each top-level message
                var messageIds = messages.Select(m => m.Id).ToList();
                var replies = await _context.ConMessages.AsNoTracking()
                    .Where(m => m.ParentId != null && messageIds.Contains(m.ParentId.Value) && m.DeletedAt == null)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();

                var repliesByParent = replies.GroupBy(r => r.ParentId!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var msg in messages)
                    msg.Replies = repliesByParent.TryGetValue(msg.Id, out var r) ? r : new List<ConMessage>();

                return new ResultOptions { Success = true, Data = messages.Cast<object>().ToList(), Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> UpsertMessageAsync(ConMessage message)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (message.Id > 0)
                    {
                        var existing = await _context.ConMessages.FindAsync(message.Id);
                        if (existing == null || existing.UserId != message.UserId)
                            return new ResultOptions { Success = false, Message = "Message not found", Status = 404 };

                        existing.Content = message.Content;
                        existing.Title = message.Title;
                        existing.Type = message.Type;
                        existing.Location = message.Location;
                        existing.OccurAt = message.OccurAt;
                        existing.IsSensitive = message.IsSensitive;
                        existing.DeletedAt = message.DeletedAt;
                        existing.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        await tx.CommitAsync();
                        return new ResultOptions { Success = true, Object = existing, Status = 200 };
                    }
                    else
                    {
                        message.CreatedAt = DateTime.UtcNow;
                        message.UpdatedAt = DateTime.UtcNow;
                        _context.ConMessages.Add(message);
                        await _context.SaveChangesAsync();
                        await tx.CommitAsync();
                        return new ResultOptions { Success = true, Object = message, Status = 200 };
                    }
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    _logger.LogError(ex, "Error upserting message");
                    return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
                }
            });
        }

        public async Task<ResultOptions> PromoteMessageToTopicAsync(int messageId, int userId, string topicName)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    var message = await _context.ConMessages.FindAsync(messageId);
                    if (message == null || message.UserId != userId)
                        return new ResultOptions { Success = false, Message = "Message not found", Status = 404 };

                    var topic = new ConTopic
                    {
                        UserId = userId,
                        EntityType = message.EntityType,
                        EntityId = message.EntityId,
                        Name = topicName,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };
                    _context.ConTopics.Add(topic);
                    await _context.SaveChangesAsync(); // flush to get topic.Id

                    message.TopicId = topic.Id;
                    message.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    return new ResultOptions { Success = true, Object = topic, Status = 200 };
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    _logger.LogError(ex, "Error promoting message to topic");
                    return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
                }
            });
        }
    }
}
