using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TLMos.DTOs;
using TLMos.Mos;

namespace TLDataSes.Ins
{
    public interface IXSe
    {
        Task<List<Event>> GetEvents();
        Task<ResultOptions> IuEv(Event ev);

    }
}
