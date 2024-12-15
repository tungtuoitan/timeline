using Microsoft.AspNetCore.Mvc;
using TLMos.Mos;
using TLDataSes.Ins;


namespace TLAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SRsController : Controller
    {
        private readonly ISRsSe _SRsSe;
        public SRsController(ISRsSe XSe)
        {
            _SRsSe = XSe;
        }

        [HttpGet("GetSRs")]
        public async Task<List<SR>> GetSRs(string? type)
        {
            var sr = await _SRsSe.GetSRs(type);
            return sr;
        }
    }
}
