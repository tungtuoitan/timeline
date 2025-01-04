using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TLMos.DTOs;
using TLMos.Mos;

namespace PRDataRes.Ins
{
    public interface IPrRe
    {
        Task<List<Pr>> GetPrs();
        Task<PrsResult> IuPr(Pr pr);
    }
}
