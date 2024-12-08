using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TLMos.DTOs;
using TLMos.Mos;

namespace TLDataRes.Ins
{
    public interface IEvRe
    {
        Task<List<Ev>> GetEvs();
        Task<ResultOptions> IuEv(Ev ev);
    }
}
