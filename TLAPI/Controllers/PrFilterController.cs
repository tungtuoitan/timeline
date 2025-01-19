using Microsoft.AspNetCore.Mvc;
using TLMos.Mos;
using TLMos.DTOs;
using PRDataSes.Ins;


namespace TLAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PrFilterController : Controller
    {
        private readonly IPrFilterSe _XSe;
        private readonly ILogger<PrController> _logger;
        public PrFilterController(IPrFilterSe XSe, ILogger<PrController> logger)
        {
            _XSe = XSe;
            _logger = logger;
        }

        [HttpGet("GetPrParentIds")]
        public async Task<List<int>> GetPrParentIds()
        {
            var parents = await _XSe.GetPrParentIds();
            return parents;
        }
    }
}
