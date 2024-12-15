using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TLMos.Mos;

namespace TLDataRes.Ins
{
   public interface ISRsRe
    {
        Task<List<SR>> GetSRs (string type);
    }
}
