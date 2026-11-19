using System.Globalization;
using System.Text.Json;
using SuperAppDataRepositories.Ins.Reflect;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests.Reflect;
using SuperAppModels.Models.Reflect;
using SuperAppServices.Interfaces.Reflect;

namespace SuperAppServices.Services.Reflect
{
    public class ReflectPlanService : IReflectPlanService
    {
        private readonly IReflectPlanRepository _repository;

        public ReflectPlanService(IReflectPlanRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public Task<ResultOptions> GetItemsAsync(int userId, DateTime? from, DateTime? to) => _repository.GetItemsAsync(userId, from, to);

        public Task<ResultOptions> UpsertItemAsync(UpsertReflectPlanItemRequest request)
        {
            if (!DateTime.TryParseExact(request.PlanDate?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var planDate))
                return Task.FromResult(new ResultOptions { Success = false, Message = "planDate must be yyyy-MM-dd", Status = 400 });

            var extra = string.IsNullOrWhiteSpace(request.Extra) ? "{}" : request.Extra;
            try { JsonDocument.Parse(extra).Dispose(); }
            catch (JsonException)
            {
                return Task.FromResult(new ResultOptions { Success = false, Message = "extra must be valid JSON", Status = 400 });
            }

            var item = new ReflectPlanItem
            {
                Id = request.Id.Trim(),
                UserId = request.UserId,
                PlanDate = planDate.Date,
                Title = (request.Title ?? "").Trim(),
                Status = string.IsNullOrWhiteSpace(request.Status) ? "planned" : request.Status.Trim(),
                SortOrder = request.SortOrder,
                Note = request.Note ?? "",
                Extra = extra
            };
            return _repository.UpsertItemAsync(item);
        }

        public Task<ResultOptions> DeleteItemAsync(int userId, string id) => _repository.DeleteItemAsync(userId, id);
    }
}
