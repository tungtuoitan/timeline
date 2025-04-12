using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TLMos.Mos;
using UserProfileDataSes.Ins;

namespace TimelineAPI.Controllers
{
    [Route("auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IAuthSe _authService;

        public AuthController(IConfiguration config, IAuthSe authSe)
        {
            _config = config;
            _authService = authSe;
        }

        [HttpPost("getBackendToken")]
        public async Task<IActionResult> Login([FromBody] UserModel model)
        {
            try
            {
                UserModel res = await _authService.GenerateJwtToken(model);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return Unauthorized(ex);
            }
        }
       
    }

  
}
