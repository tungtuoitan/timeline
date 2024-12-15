using System;




using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TLMos.DTOs;
using TLMos.Mos;

namespace TLDataSes.Ins
{
    public interface ISRsSe
    {
        Task<List<SR>> GetSRs(string? type);

    }
}
