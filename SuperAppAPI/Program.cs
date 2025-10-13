
using SuperAppDataRepositories.Ins;
using SuperAppDataRepositories.Repositories;
using SuperAppDataRepositories.Data;
using SuperApp.Application.Common.Mappings;
using Serilog;
using UserProfileDataRepositories.Ins;
using UserProfileDataRepositories.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using AutoMapper;
using FluentValidation;
using MediatR;


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

                    // TEMPORARY: JWT Authentication disabled for development
                    // Configure JWT Authentication
                    //services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    //    .AddJwtBearer(options =>
                    //    {
                    //        options.TokenValidationParameters = new TokenValidationParameters
                    //        {
                    //            ValidateIssuerSigningKey = true,
                    //            IssuerSigningKey = new SymmetricSecurityKey(key),
                    //            ValidateIssuer = false,
                    //            ValidateAudience = false,
                    //            ValidateLifetime = true,
                    //            ClockSkew = TimeSpan.Zero
                    //        };
                    //    });

                    //services.AddAuthorization();

                    // Register MediatR with pipeline behaviors
                    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SuperApp.Application.Features.Notes.Queries.GetNotes.GetNotesQuery).Assembly));
                    
                    // Register FluentValidation
                    services.AddValidatorsFromAssembly(typeof(SuperApp.Application.Features.Notes.Commands.CreateNote.CreateNoteValidator).Assembly);

                    // Register AutoMapper manually to avoid ambiguous calls
                    var mapperConfig = new AutoMapper.MapperConfiguration(mc =>
                    {
                        mc.AddProfile(new MappingProfile());
                    });
                    IMapper mapper = mapperConfig.CreateMapper();
                    services.AddSingleton(mapper);
                    
                    // Register MediatR pipeline behaviors
                    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(SuperApp.Application.Common.Behaviors.ValidationBehavior<,>));
                    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(SuperApp.Application.Common.Behaviors.LoggingBehavior<,>));

                    // Register Connection Factory
                    services.AddScoped<IConnectionFactory, ConnectionFactory>();

                    // Register repositories
                    services.AddScoped<INoteRepository, NoteRepository>();
                    services.AddScoped<ITagRepository, TagRepository>();
                    services.AddScoped<IStandardRegistryRepository, StandardRegistryRepository>();
                    services.AddScoped<IAuthRepository, AuthRepository>();
                    services.AddScoped<IUserProfileRepository, UserProfileRepository>();

                    // Services layer removed - using CQRS pattern instead
                    // All controllers now use MediatR commands/queries or repository pattern directly
                });

    }
}
