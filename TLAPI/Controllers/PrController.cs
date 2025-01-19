using Microsoft.AspNetCore.Mvc;
using TLMos.Mos;
using TLMos.DTOs;
using PRDataSes.Ins;


namespace TLAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PrController : Controller
    {
        private readonly IPRSe _XSe;
        private readonly ILogger<PrController> _logger;
        public PrController(IPRSe XSe, ILogger<PrController> logger)
        {
            _XSe = XSe;
            _logger = logger;
        }

        [HttpGet("GetPrs")]
        public async Task<List<Pr>> GetPrs(string? searchText)
        {
            var prs = await _XSe.GetPrs(searchText);
            return prs;
        }

        [HttpPost("IuPr")]
        public async Task<IActionResult> IuPr([FromForm] Pr pr)
        {
            PrsResult res = await _XSe.IuPr( pr);
            if(res.Options.Success)
            {
                return Ok(res);
            }
            else
            {
                return BadRequest(new PrsResult { Prs = new List<Pr>(), Options = res.Options });
            }
        }
    }
}
