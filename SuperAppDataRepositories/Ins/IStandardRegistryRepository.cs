using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface IStandardRegistryRepository
    {
        Task<List<StandardRegistry>> GetAllAsync(string type);
        Task<StandardRegistry?> GetByIdAsync(int id);
    }
}
