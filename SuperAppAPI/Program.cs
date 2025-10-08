using SuperAppDataServices.Ins;
using SuperAppDataServices.Services;
using SuperAppDataRepositories.Ins;
using SuperAppDataRepositories.Repositories;
using Serilog;
using UserProfileDataRepositories.Ins;
using UserProfileDataServices.Ins;
using UserProfileDataRepositories.Repositories;
using UserProfileDataServices.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;


namespace SuperAppAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug() 
                .WriteTo.Console()  
                .WriteTo.File("Logs/superapp.log", rollingInterval: RollingInterval.Day) // Log ra file theo ngày
                .CreateLogger();

            try{
                Log.Information(">>  >>  >>  Starting up the SuperApp application...");
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

                    services.AddDistributedMemoryCache();
                    services.AddSession(o =>
                    {
                        o.IdleTimeout = TimeSpan.FromMinutes(30);
                        o.Cookie.HttpOnly = true;
                        o.Cookie.IsEssential = true;
                    });

                    services.AddHttpContextAccessor();

                    services.AddScoped<IUserProfileRe, UserProfileRe>();
                    services.AddScoped<IUserProfileSe, UserProfileSe>();
                    services.AddScoped<IAuthSe, AuthSe>();
                    services.AddScoped<IAuthRe, AuthRe>();
                    services.AddScoped<INoteRe, NoteRe>();
                    services.AddScoped<INoteSe, NoteSe>();
                    services.AddScoped<IStandardRegistryService, StandardRegistryService>();
                    services.AddScoped<IStandardRegistryRepository, StandardRegistryRepository>();
                });

    }
}
