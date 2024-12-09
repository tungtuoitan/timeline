using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TLMos.DTOs;
using TLMos.Mos;

namespace TLDataSes.Ins
{
    public interface IEvSe
    {
        Task<List<Ev>> GetEvs();
        Task<EvsResult> IuEv(Ev ev);

        DateTime ConvertUTCToUserTimeZone(DateTime utcDateTime);

    }
}
