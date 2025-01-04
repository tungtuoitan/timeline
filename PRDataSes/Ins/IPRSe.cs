
using TLMos.DTOs;
using TLMos.Mos;

namespace PRDataSes.Ins
{
    public interface IPRSe
    {
        Task<List<Pr>> GetPrs();
        Task<PrsResult> IuPr(Pr pr);

    }
}