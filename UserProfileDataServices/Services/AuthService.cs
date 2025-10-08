
using SuperAppModels.Mos;
using SuperAppModels.DTOs;
using Microsoft.AspNetCore.Http;
using UserProfileDataServices.Ins;
using UserProfileDataRepositories.Ins;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using PLMModels.DTOs;
using UserProfileDataRepositories;
using Newtonsoft.Json.Linq;
namespace UserProfileDataServices.Services
{
    public class AuthService: IAuthService
    {
        private readonly IAuthRepository _XRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _config;
        public AuthService(IAuthRepository XRepository, IHttpContextAccessor httpContextAccessor, IConfiguration config)
        {
            _XRepository = XRepository;
            _httpContextAccessor = httpContextAccessor;
            _config = config;
        }
        public async Task<ResultOptions2<UserModel>> IuUser(UserModel model)
        {
            try
            {
                if (model.Email?.Length > 0 || model.Phone?.Length > 0)
                {
                    if (model.Type == UserProfileDataConstants.signupDefault)
                    {
                        // check email/phone is duplicated?
                        List<UserModel> list = (await GetUsers()).Data;
                        UserModel? existUser = list.FirstOrDefault(u => u.Email == model.Email && model.Email != null || u.Phone == model.Phone && model.Phone != null);
                        if (existUser != null)
                        {
                            return new ResultOptions2<UserModel>
                            {
                                Success = false,
                                Message = model.Email != null && model.Email == existUser?.Email ? "Email is already registered" : "Phone number is already registered",
                                Data = new UserModel()
                            };
                        }

                        ResultOptions2<UserModel> res = await _XRepository.IuUser(model);
                        string token = GenerateJwtToken(model);
                        res.Data.Token = token;
                        return res;
                    }
                    else if (model.Type == UserProfileDataConstants.loginByGoogle)
                    {
                        List<UserModel> list = (await GetUsers()).Data;
                        UserModel? existUser = list.FirstOrDefault(u => u.Email == model.Email && model.Email != null || u.Phone == model.Phone && model.Phone != null);
                        if (existUser == null)
                        {
                            ResultOptions2<UserModel> res = await _XRepository.IuUser(model);
                        }

                        string token = GenerateJwtToken(model);
                        return new ResultOptions2<UserModel>
                        {
                            Success = true,
                            Message = "Login successfully",
                            Data = new UserModel
                            {
                                Email = model.Email,
                                FirstName = model.FirstName,
                                LastName = model.LastName,
                                Token = token,
                            }
                        };
                    }
                    else if (model.Type == UserProfileDataConstants.loginByDefault)
                    {
                        List<UserModel> list = (await GetUsers()).Data;
                        UserModel? existUser = list.FirstOrDefault(u => u.Email == model.Email && model.Email != null || u.Phone == model.Phone && model.Phone != null);
                        if(existUser == null)
                        {
                            return new ResultOptions2<UserModel>
                            {
                                Success = false,
                                Message = "The email/phone you entered isn't connected to an account.",
                                Data = new UserModel()
                            };
                        }
                        
                        if(Helpers.VerifyPassword(model.Password ?? "", existUser.Password))
                        {
                            string token = GenerateJwtToken(model);
                            return new ResultOptions2<UserModel>
                            {
                                Success = true,
                                Message = "Login successfully",
                                Data = new UserModel
                                {
                                    Email = model.Email,
                                    FirstName = model.FirstName,
                                    LastName = model.LastName,
                                    Token = token,
                                }
                            };
                        }
                        else
                        {
                            return new ResultOptions2<UserModel>
                            {
                                Success = false,
                                Message = "Password isn't correct!",
                                Data = new UserModel()
                            };
                        }
                    }
                    else throw new Exception("Wrong type");
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

        public string GenerateJwtToken(UserModel model)
        {
            try
            {
                if (model.Email?.Length > 0 || model.Phone?.Length > 0)
                {
                    var jwtSettings = _config.GetSection("Jwt");
                    var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

                    var claims = new[]
                    {
                        new Claim(JwtRegisteredClaimNames.Sub, model.Email != null ? model.Email : model.Phone),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                    };

                    var token = new JwtSecurityToken(
                        issuer: jwtSettings["Issuer"],
                        audience: jwtSettings["Audience"],
                        claims: claims,
                        expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpireMinutes"])),
                        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
                    );

                    string tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                    return tokenString;
                }
                else
                {
                    throw new Exception(model.Email == null ?  "Invalid email!" : "Invalid phone");
                }
            }
            catch
            {
                throw;
            }

        }
        public async Task<ResultOptions2<List<UserModel>>> GetUsers()
        {
            var res = await _XRepository.GetUsers();
            return res;
        }

    }

}

      
