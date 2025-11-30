using SuperAppDataRepositories.Ins;
using SuperAppDataRepositories.Repositories;
using SuperAppDataRepositories.Data;
using Serilog;
using System.Text;
using Microsoft.EntityFrameworkCore;


namespace SuperAppAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("Logs/superapp.log", rollingInterval: RollingInterval.Day)
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
                    var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "");

                    services.AddDistributedMemoryCache();
                    services.AddSession(o =>
                    {
                        o.IdleTimeout = TimeSpan.FromMinutes(30);
                        o.Cookie.HttpOnly = true;
                        o.Cookie.IsEssential = true;
                    });

                    services.AddHttpContextAccessor();

                    // Register AutoMapper
                    services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

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

                
                    // Register EF Core DbContext
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        var connectionString = hostContext.Configuration.GetConnectionString("SuperAppConnection");
                        options.UseSqlServer(connectionString, sqlOptions =>
                        {
                            sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                            sqlOptions.CommandTimeout(120);
                        });

                        if (hostContext.HostingEnvironment.IsDevelopment())
                        {
                            options.EnableSensitiveDataLogging();
                            options.EnableDetailedErrors();
                        }
                    });

                    // Register Connection Factory (for stored procedures - if still needed)
                    services.AddScoped<IConnectionFactory, ConnectionFactory>();

                    // Register repositories (with EF Core)
                    services.AddScoped<INoteRepository, NoteRepository>();
                    services.AddScoped<SuperAppDataRepositories.Ins.IWorkspaceRepository, WorkspaceRepository>();
                    services.AddScoped<IStandardRegistryRepository, StandardRegistryRepository>();
                    services.AddScoped<IUserRepository, UserRepository>();
                    services.AddScoped<IUserProfileRepository, UserProfileRepository>();

                    // Register Business Services
                    services.AddScoped<SuperAppServices.Interfaces.IWorkspaceService, SuperAppServices.Services.WorkspaceService>();
                    services.AddScoped<SuperAppServices.Interfaces.INoteService, SuperAppServices.Services.NoteService>();
                });

    }
}
