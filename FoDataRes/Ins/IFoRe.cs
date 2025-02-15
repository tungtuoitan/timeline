using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TLMos.DTOs;
using TLMos.Mos;

namespace FoDataRes.Ins
{
    public interface IFoRe
    {
        Task<List<Fo>> GetFos();
        Task<FosResult> IuFo(Fo fo);
    }
}
