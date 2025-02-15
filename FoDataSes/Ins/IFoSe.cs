
using TLMos.DTOs;
using TLMos.Mos;

namespace FoDataSes.Ins
{
    public interface IFoSe
    {
        Task<List<Fo>> GetFos();
        Task<FosResult> IuFo(Fo fo);

    }
}