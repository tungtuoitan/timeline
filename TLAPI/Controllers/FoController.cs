using Microsoft.AspNetCore.Mvc;
using TLMos.Mos;
using TLMos.DTOs;
using FoDataSes.Ins;


namespace TLAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FoController : Controller
    {
        private readonly IFoSe _XSe;
        private readonly ILogger<FoController> _logger;
        public FoController(IFoSe XSe, ILogger<FoController> logger)
        {
            _XSe = XSe;
            _logger = logger;
        }

        [HttpGet("GetFos")]
        public async Task<List<Fo>> GetFos()
        {
            var fos = await _XSe.GetFos();
            return fos;
        }

        [HttpPost("IuFos")]
        public async Task<IActionResult> IuFo([FromForm] Fo fo)
        {
            FosResult res = await _XSe.IuFo(fo);
            if(res.Options.Success)
            {
                return Ok(res);
            }
            else
            {
                return BadRequest(new FosResult { Fos = new List<Fo>(), Options = res.Options });
            }
        }
    }
}
