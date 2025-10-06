using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PLMModels.DTOs;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
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
        private readonly HttpClient _httpClient;

        public AuthController(IConfiguration config, IAuthSe authSe, HttpClient httpClient)
        {
            _config = config;
            _authService = authSe;
            _httpClient = httpClient;
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

        [HttpPost("loginSignup2")]
        public async Task<IActionResult> ExchangeToken([FromBody] CodeRequest request)
        {
            var clientId = _config["OAuth:ClientId"];
            var clientSecret = _config["OAuth:ClientSecret"];
            var redirectUri = _config["OAuth:RedirectUri"];
            var tokenUrl = "https://oauth2.googleapis.com/token";

            var formData = new Dictionary<string, string>
            {
                { "code", request.Code },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" }
            };

            var content = new FormUrlEncodedContent(formData);

            try
            {
                var response = await _httpClient.PostAsync(tokenUrl, content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var tokens = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                return Ok(tokens);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }






}


public class CodeRequest
{
    public string Code { get; set; }
}
