using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PLMModels.DTOs;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TLMos.DTOs;
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

        [HttpPost("loginSignup")]
        public async Task<IActionResult> Login([FromBody] UserModel model)
        {
            try
            {
                ResultOptions2<UserModel> res = await _authService.IuUser(model);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return Unauthorized(ex);
            }
        }
       
    }

  
}
