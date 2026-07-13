using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins.DailyLog;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests.DailyLog;
using SuperAppModels.Models.DailyLog;
using SuperAppServices.Interfaces.DailyLog;

namespace SuperAppServices.Services.DailyLog
{
    public class DailyLogTemplateService : IDailyLogTemplateService
    {
        private readonly IDailyLogTemplateRepository _repository;
        private readonly ILogger<DailyLogTemplateService> _logger;

        public DailyLogTemplateService(IDailyLogTemplateRepository repository, ILogger<DailyLogTemplateService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ResultOptions> GetTemplateAsync(int userId)
        {
            var result = await _repository.GetTemplateAsync(userId);
            if (!result.Success) return result;

            // Auto-seed the default template on first access
            var fields = (result.Data ?? new List<object>()).Cast<DailyLogFieldTemplate>().ToList();
            if (fields.Count == 0)
            {
                _logger.LogInformation("Seeding default daily-log template for userId={UserId}", userId);
                var seed = BuildDefaultTemplate(userId);
                var seedResult = await _repository.UpsertTemplateAsync(seed);
                if (!seedResult.Success) return seedResult;
                return await _repository.GetTemplateAsync(userId);
            }
            return result;
        }

        public async Task<ResultOptions> UpsertTemplateAsync(int userId, List<UpsertDailyLogTemplateRequest> requests)
        {
            try
            {
                if (requests == null || requests.Count == 0)
                    return new ResultOptions { Success = false, Message = "No template fields provided", Status = 400 };

                var fields = requests.Select(r => new DailyLogFieldTemplate
                {
                    Id = r.Id,
                    UserId = userId,
                    Section = r.Section,
                    FieldKey = r.FieldKey,
                    Label = r.Label,
                    FieldType = r.FieldType,
                    RangeMin = r.RangeMin,
                    RangeMax = r.RangeMax,
                    SortOrder = r.SortOrder,
                    DeletedAt = r.DeletedAt
                }).ToList();

                return await _repository.UpsertTemplateAsync(fields);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting daily log template");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        private static List<DailyLogFieldTemplate> BuildDefaultTemplate(int userId) => new()
        {
            new DailyLogFieldTemplate { UserId = userId, Section = "input",  FieldKey = "general", Label = "General", FieldType = "longText", SortOrder = 0 },
            new DailyLogFieldTemplate { UserId = userId, Section = "input",  FieldKey = "note",    Label = "Note",    FieldType = "longText", SortOrder = 1 },
            new DailyLogFieldTemplate { UserId = userId, Section = "output", FieldKey = "general",      Label = "General",      FieldType = "longText", SortOrder = 0 },
            new DailyLogFieldTemplate { UserId = userId, Section = "output", FieldKey = "emotion_note", Label = "Emotion note", FieldType = "longText", SortOrder = 1 },
            new DailyLogFieldTemplate { UserId = userId, Section = "output", FieldKey = "note",         Label = "Note",         FieldType = "longText", SortOrder = 2 },
        };
    }
}
