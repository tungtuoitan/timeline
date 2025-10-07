using Microsoft.AspNetCore.Mvc;
using TLMos.Mos;
using TLMos.DTOs;
using UserProfileDataSes.Ins;


namespace TLAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserProfileController : Controller
    {
        private readonly IUserProfileSe _XSe;
        private readonly ILogger<UserProfileController> _logger;
        public UserProfileController(IUserProfileSe XSe, ILogger<UserProfileController> logger)
        {
            _XSe = XSe;
            _logger = logger;
        }

        [HttpGet("GetUserProfileJson")]
        public async Task<UserProfile> GetUserProfileJson(string email, string appC)
        {
            var userProfile = await _XSe.GetUserProfileJson(email, appC);
            return userProfile;
        }

        [HttpPost("IuUserProfile")]
        public async Task<ResultOptions> IuUserProfile([FromForm] string email, [FromForm] string appC, [FromForm] string userProfileJson)
        {
            return await _XSe.IuUserProfile(email, appC, userProfileJson);
        }
    }
}
