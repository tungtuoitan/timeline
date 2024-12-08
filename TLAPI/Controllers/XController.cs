using Microsoft.AspNetCore.Mvc;
using TLMos.Mos;
using TLDataSes.Ins;
using TLMos.DTOs;


namespace Timeline.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class XController : Controller
    {
        private readonly IXSe _XSe;
        public XController(IXSe XSe)
        {
            _XSe = XSe;
        }

        [HttpGet("GetEvents")]
        public async Task<List<Event>> GetEvents()
        {
            var Events = await _XSe.GetEvents();
            return Events;  
        }

        [HttpPost("IuEv")]
        public async Task<IActionResult> IuEv([FromForm] Event ev)
        {
            ResultOptions res = await _XSe.IuEv( ev);
            if(res.Success)
            {
                return Ok(res);
            }
            else
            {
                return BadRequest(res);
            }
        }
    }
}
