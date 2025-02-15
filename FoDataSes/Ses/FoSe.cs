
using TLMos.Mos;
using TLMos.DTOs;
using Microsoft.AspNetCore.Http;
using FoDataSes.Ins;
using FoDataRes.Ins;

namespace FoDataSes.Ses
{
    public class FoSe: IFoSe
    {
        private readonly IFoRe _XRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public FoSe(IFoRe XRepo, IHttpContextAccessor httpContextAccessor)
        {
            _XRepo = XRepo;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<List<Fo>> GetFos()
        {
            List<Fo> fos = await _XRepo.GetFos();
            return fos;
        }
        public async Task<FosResult> IuFo(Fo fo)
        {
            return await _XRepo.IuFo(fo);
        }
    }
}
