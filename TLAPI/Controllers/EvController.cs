using Microsoft.AspNetCore.Mvc;
using TLMos.Mos;
using TLDataSes.Ins;
using TLMos.DTOs;
using Serilog;


namespace TLAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EvController : Controller
    {
        private readonly IEvSe _XSe;
        private readonly ILogger<EvController> _logger;
        public EvController(IEvSe XSe, ILogger<EvController> logger)
        {
            _XSe = XSe;
            _logger = logger;
        }

        [HttpGet("GetEvs")]
        public async Task<List<Ev>> GetEvs()
        {
            var evs = await _XSe.GetEvs();
            return evs;
        }

        [HttpPost("IuEv")]
        public async Task<IActionResult> IuEv([FromForm] Ev ev)
        {
            EvsResult res = await _XSe.IuEv( ev);
            if(res.Options.Success)
            {
                return Ok(res);
            }
            else
            {
                return BadRequest(new EvsResult { Evs = new List<Ev>(), Options = res.Options });
            }
        }
    }
}
