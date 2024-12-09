using Microsoft.AspNetCore.Mvc;
using TLMos.Mos;
using TLDataSes.Ins;
using TLMos.DTOs;


namespace TLAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EvController : Controller
    {
        private readonly IEvSe _XSe;
        public EvController(IEvSe XSe)
        {
            _XSe = XSe;
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
