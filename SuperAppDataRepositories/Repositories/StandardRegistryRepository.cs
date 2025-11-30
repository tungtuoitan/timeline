using SuperAppDataRepositories.Ins;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    public class StandardRegistryRepository : IStandardRegistryRepository
    {
        public Task<List<StandardRegistry>> GetAllAsync(string type)
        {
            throw new NotImplementedException("StandardRegistryRepository chưa được implement - cần migrate từ code cũ");
        }

        public Task<StandardRegistry?> GetByIdAsync(int id)
        {
            throw new NotImplementedException("StandardRegistryRepository chưa được implement - cần migrate từ code cũ");
        }
    }
}
