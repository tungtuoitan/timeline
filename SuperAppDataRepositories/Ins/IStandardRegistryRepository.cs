using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface IStandardRegistryRepository
    {
        Task<List<StandardRegistry>> GetAllStandardRegistry();
        Task<StandardRegistry?> GetStandardRegistryById(int id);
        Task<List<StandardRegistry>> GetStandardRegistryByType(string type);
        
        // Legacy method for backward compatibility
        Task<List<StandardRegistry>> GetStandardRegistries(string type);
    }
}
