
using TLMos.Mos;
using TLMos.DTOs;
using Microsoft.AspNetCore.Http;
using PRDataRes.Ins;
using UserProfileDataSes.Ins;
using UserProfileDataRes.Ins;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
namespace UserProfileDataSes.Ses
{
    public class AuthSe: IAuthSe
    {
        private readonly IUserProfileRe _XRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _config;
        public AuthSe(IUserProfileRe XRepo, IHttpContextAccessor httpContextAccessor, IConfiguration config)
        {
            _XRepo = XRepo;
            _httpContextAccessor = httpContextAccessor;
            _config = config;
        }
        public async Task<UserModel> GenerateJwtToken(UserModel model)
        {
            try
            {
                if(model.Email.Length > 0)
                {
                    var jwtSettings = _config.GetSection("Jwt");
                    var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

                    var claims = new[]
                    {
                        new Claim(JwtRegisteredClaimNames.Sub, model.Email),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                    };

                    var token = new JwtSecurityToken(
                        issuer: jwtSettings["Issuer"],
                        audience: jwtSettings["Audience"],
                        claims: claims,
                        expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpireMinutes"])),
                        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
                    );

                    return new UserModel
                    {
                        Email = model.Email,
                        Token = new JwtSecurityTokenHandler().WriteToken(token),
                        //expire = DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpireMinutes"])).ToString()
                    };
                }
                else
                {
                    throw new Exception("Invalid email!"); 
                }
            }
            catch
            {
                throw;
            }

        }
      
    }
}
