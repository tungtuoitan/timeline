
using TLDataSes.Ins;
using TLDataSes.Ses;
using TLDataRes.Ins;
using TLDataRes.Res;
using Serilog;
using PRDataRes.Res;
using PRDataRes.Ins;
using PRDataSes.Ins;
using PRDataSes.Ses;
using UserProfileDataRes.Ins;
using UserProfileDataSes.Ins;
using UserProfileDataRes.Res;
using UserProfileDataSes.Ses;
using FoDataRes.Ins;
using FoDataRes.Res;
using FoDataSes.Ins;
using FoDataSes.Ses;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;


namespace TimelineAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug() 
                .WriteTo.Console()  
                .WriteTo.File("Logs/timeline.log", rollingInterval: RollingInterval.Day) // Log ra file theo ngày
                .CreateLogger();

            try{
                Log.Information(">>  >>  >>  Starting up the application...");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex){
                Log.Fatal(ex, "Application start-up failed");
            }
            finally{
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                    .UseStartup<Startup>()
                    .ConfigureKestrel(options =>
                    {
                        options.Limits.MaxRequestBodySize = 100 * 1024 * 1024;
                    });
                })
                .ConfigureServices((hostContext, services) =>
                {
                    var builder = hostContext.Configuration;
                    var jwtSettings = hostContext.Configuration.GetSection("Jwt");
                    var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

                    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options =>
                        {
                            options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuerSigningKey = true,
                                IssuerSigningKey = new SymmetricSecurityKey(key),
                                ValidateIssuer = true,
                                ValidIssuer = jwtSettings["Issuer"],
                                ValidateAudience = true,
                                ValidAudience = jwtSettings["Audience"],
                                ValidateLifetime = true,
                                ClockSkew = TimeSpan.Zero // Không cho phép thời gian trễ
                            };
                        });

                    services.AddDistributedMemoryCache();
                    services.AddSession(o =>
                    {
                        o.IdleTimeout = TimeSpan.FromMinutes(30);
                        o.Cookie.HttpOnly = true;
                        o.Cookie.IsEssential = true;
                    });

                    services.AddHttpContextAccessor();

                    services.AddScoped<IEvRe, EvRe>();
                    services.AddScoped<IEvSe, EvSe>();
                    services.AddScoped<ISRsSe, SRsSe>();
                    services.AddScoped<ISRsRe, SRsRe>();
                    services.AddScoped<IPrRe, PrRe>();
                    services.AddScoped<IPRSe, PrSe>();
                    services.AddScoped<IPrFilterRe, PrFilterRe>();
                    services.AddScoped<IPrFilterSe, PrFilterSe>();
                    services.AddScoped<IUserProfileRe, UserProfileRe>();
                    services.AddScoped<IUserProfileSe, UserProfileSe>();
                    services.AddScoped<IFoRe, FoRe>();
                    services.AddScoped<IFoSe, FoSe>();
                    services.AddScoped<IAuthSe, AuthSe>();
                    services.AddScoped<IAuthRe, AuthRe>();
                });

    }
}
