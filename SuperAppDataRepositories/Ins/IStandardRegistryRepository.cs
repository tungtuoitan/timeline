using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperAppModels.Mos;

namespace SuperAppDataRepositories.Ins
{
   public interface IStandardRegistryRepository
    {
        Task<List<StandardRegistry>> GetStandardRegistries (string type);
    }
}
