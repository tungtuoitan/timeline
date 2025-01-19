using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TLMos.DTOs;
using TLMos.Mos;

namespace PRDataRes.Ins
{
    public interface IPrFilterRe
    {
        Task<List<int>> GetPrParentIds();
    }
}
