using SuperAppDataServices.Ins;
using SuperAppDataServices.Services;
using SuperAppDataRepositories.Ins;
using SuperAppDataRepositories.Repositories;
using SuperAppDataRepositories.Data;
using SuperApp.Application.Common.Mappings;
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
using System.Reflection;
using AutoMapper;


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

                    // Configure JWT Authentication
                    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options =>
                        {
                            options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuerSigningKey = true,
                                IssuerSigningKey = new SymmetricSecurityKey(key),
                                ValidateIssuer = false,
                                ValidateAudience = false,
                                ValidateLifetime = true,
                                ClockSkew = TimeSpan.Zero
                            };
                        });

                    services.AddAuthorization();

                    // Register MediatR
                    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SuperApp.Application.Features.Notes.Queries.GetNotes.GetNotesQuery).Assembly));

                    // Register AutoMapper
                    var mapperConfig = new AutoMapper.MapperConfiguration(mc =>
                    {
                        mc.AddProfile(new MappingProfile());
                    });
                    IMapper mapper = mapperConfig.CreateMapper();
                    services.AddSingleton(mapper);

                    // Register Connection Factory
                    services.AddScoped<IConnectionFactory, ConnectionFactory>();

                    // Register Repositories (keeping these for now as they're used by CQRS handlers)
                    services.AddScoped<IUserProfileRepositoy, UserProfileRepository>();
                    services.AddScoped<IAuthRepository, AuthRepository>();
                    services.AddScoped<INoteRepository, NoteRepository>();
                    services.AddScoped<IStandardRegistryRepository, StandardRegistryRepository>();

                    // Register Services (only keeping what's still needed - gradually phase these out)
                    services.AddScoped<IUserProfileSe, UserProfileSe>();
                    services.AddScoped<IAuthService, AuthService>();
                    services.AddScoped<IStandardRegistryService, StandardRegistryService>();

                    // Note: Removed INoteSe registration as we're using CQRS for Notes now
                });

    }
}
