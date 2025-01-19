
using TLMos.Mos;
using TLMos.DTOs;
using Microsoft.AspNetCore.Http;
using PRDataSes.Ins;
using PRDataRes.Ins;

namespace PRDataSes.Ses
{
    public class PrFilterSe: IPrFilterSe
    {
        private readonly IPrFilterRe _XRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public PrFilterSe(IPrFilterRe XRepo, IHttpContextAccessor httpContextAccessor)
        {
            _XRepo = XRepo;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<List<int>> GetPrParentIds()
        {
            List<int> prs = await _XRepo.GetPrParentIds();
            return prs;
        }
    }
}
