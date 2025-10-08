using Microsoft.AspNetCore.Mvc;
using SuperAppModels.Mos;
using SuperAppDataServices.Ins;


namespace SuperAppAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StandardRegistryController : Controller
    {
        private readonly IStandardRegistryService _StandardRegistryService;
        
        public StandardRegistryController(IStandardRegistryService XSe)
        {
            _StandardRegistryService = XSe;
        }

        [HttpGet("GetStandardRegistries")]
        public async Task<List<StandardRegistry>> GetStandardRegistries(string? type)
        {
            var sr = await _StandardRegistryService.GetStandardRegistries(type);
            return sr;
        }
    }
}
