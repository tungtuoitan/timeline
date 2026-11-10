using SuperAppDataRepositories.Ins.Reflect;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests.Reflect;
using SuperAppModels.Models.Reflect;
using SuperAppServices.Interfaces.Reflect;

namespace SuperAppServices.Services.Reflect
{
    public class ReflectService : IReflectService
    {
        private readonly IReflectRepository _repository;

        public ReflectService(IReflectRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public Task<ResultOptions> GetEntriesAsync(int userId) => _repository.GetEntriesAsync(userId);

        public Task<ResultOptions> UpsertEntryAsync(UpsertReflectEntryRequest request)
        {
            var entry = new ReflectEntry
            {
                Id = request.Id.Trim(),
                UserId = request.UserId,
                Type = (request.Type ?? "").Trim(),
                Think = request.Think ?? "",
                Real = request.Real ?? ""
            };
            return _repository.UpsertEntryAsync(entry);
        }

        public Task<ResultOptions> DeleteEntryAsync(int userId, string id) => _repository.DeleteEntryAsync(userId, id);
    }
}
