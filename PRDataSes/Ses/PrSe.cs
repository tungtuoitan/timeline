
using TLMos.Mos;
using TLMos.DTOs;
using Microsoft.AspNetCore.Http;
using PRDataSes.Ins;
using PRDataRes.Ins;

namespace PRDataSes.Ses
{
    public class PrSe: IPRSe
    {
        private readonly IPrRe _XRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public PrSe(IPrRe XRepo, IHttpContextAccessor httpContextAccessor)
        {
            _XRepo = XRepo;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<List<Pr>> GetPrs(string? searchText)
        {
            List<Pr> prs = await _XRepo.GetPrs(searchText);
            return prs;
        }
        public async Task<PrsResult> IuPr(Pr pr)
        {
            return await _XRepo.IuPr(pr);
        }
    }
}
