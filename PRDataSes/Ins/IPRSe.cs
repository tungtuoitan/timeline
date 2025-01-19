
using TLMos.DTOs;
using TLMos.Mos;

namespace PRDataSes.Ins
{
    public interface IPRSe
    {
        Task<List<Pr>> GetPrs(string? searchText);
        Task<PrsResult> IuPr(Pr pr);

    }
}