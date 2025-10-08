using System;




using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperAppModels.DTOs;
using SuperAppModels.Mos;

namespace SuperAppDataServices.Ins
{
    public interface IStandardRegistryService
    {
        Task<List<StandardRegistry>> GetStandardRegistries(string? type);

    }
}
