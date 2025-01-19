
using TLMos.DTOs;
using TLMos.Mos;

namespace PRDataSes.Ins
{
    public interface IPrFilterSe
    {
        Task<List<int>> GetPrParentIds();

    }
}