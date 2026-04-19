using SuperAppDataRepositories.Ins;
using SuperAppDataRepositories.Repositories;
using SuperAppDataRepositories.Data;
using Serilog;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;


namespace SuperAppAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Load .env file - try current directory first, then parent (solution root)
            var currentDir = Directory.GetCurrentDirectory();
            var envPath = Path.Combine(currentDir, ".env");

            if (!File.Exists(envPath))
            {
                // Try parent directory (solution root)
                var parentDir = Directory.GetParent(currentDir)?.FullName;
                if (parentDir != null)
                {
                    envPath = Path.Combine(parentDir, ".env");
                }
            }

            if (File.Exists(envPath))
            {
                DotNetEnv.Env.Load(envPath);
                Console.WriteLine($"✓ Loaded .env file from: {envPath}");
            }
            else
            {
                Console.WriteLine($"✗ Warning: .env file not found at: {envPath}");
                Console.WriteLine($"  Searched in: {currentDir}");
                if (Directory.GetParent(currentDir) != null)
                {
                    Console.WriteLine($"  and: {Directory.GetParent(currentDir)?.FullName}");
                }
            }

            var logPath = Path.Combine(AppContext.BaseDirectory, "Logs", "superapp-.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    shared: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Console.WriteLine($"Logs directory: {Path.GetDirectoryName(logPath)}");

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
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    // Add environment variables from .env file to configuration
                    config.AddEnvironmentVariables();
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

                    // Configure Data Protection - DISABLED for now (will add later)
                    // services.AddDataProtection()
                    //     .PersistKeysToFileSystem(new DirectoryInfo("/var/www/Timeline/keys"))
                    //     .SetApplicationName("SuperApp");

                    services.AddHttpContextAccessor();

                    // Register HttpClient for OAuth
                    services.AddHttpClient();

                    // Register AutoMapper
                    services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

                    // Configure JWT Authentication
                    services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options =>
                        {
                            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                            {
                                ValidateIssuerSigningKey = true,
                                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                                ValidateIssuer = false,
                                ValidateAudience = false,
                                ValidateLifetime = true,
                                ClockSkew = TimeSpan.Zero
                            };
                        });

                    services.AddAuthorization();


                    // Register EF Core DbContext
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        var connectionString = hostContext.Configuration.GetConnectionString("SuperAppConnection");

                        // Debug logging to verify connection string
                        if (string.IsNullOrEmpty(connectionString))
                        {
                            throw new InvalidOperationException(
                                "Connection string 'SuperAppConnection' is not configured. " +
                                "Please ensure the .env file exists and contains ConnectionStrings__SuperAppConnection.");
                        }

                        Log.Information("Using connection string: {ConnectionString}",
                            connectionString.Replace(connectionString.Split("Password=")[1].Split(";")[0], "***"));

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
                    services.AddScoped<SuperAppDataRepositories.Ins.IKKnowledgeRepository, KKnowledgeRepository>();
                    services.AddScoped<SuperAppDataRepositories.Ins.IKTestRepository, KTestRepository>();
                    services.AddScoped<IWsRepository, WsRepository>();
                    services.AddScoped<IStandardRegistryRepository, StandardRegistryRepository>();
                    services.AddScoped<IUserRepository, UserRepository>();
                    services.AddScoped<IUserProfileRepository, UserProfileRepository>();
                    services.AddScoped<SuperAppDataRepositories.Ins.IRefreshTokenRepository, SuperAppDataRepositories.Repositories.RefreshTokenRepository>();
                    services.AddScoped<IProjectRepository, ProjectRepository>();
                    services.AddScoped<ITaskRepository, TaskRepository>();
                    services.AddScoped<ITargetKeywordRepository, TargetKeywordRepository>();
                    services.AddScoped<ITaskCommentRepository, TaskCommentRepository>();

                    // Register Business Services
                    services.AddScoped<SuperAppServices.Interfaces.IWorkspaceService, SuperAppServices.Services.WorkspaceService>();
                    services.AddScoped<SuperAppServices.Interfaces.IWorkspaceItemService, SuperAppServices.Services.WorkspaceItemService>();
                    services.AddScoped<SuperAppServices.Interfaces.IWorkspaceItemHelperService, SuperAppServices.Services.WorkspaceItemHelperService>();
                    services.AddScoped<SuperAppServices.Interfaces.IKKnowledgeService, SuperAppServices.Services.KKnowledgeService>();
                    services.AddScoped<SuperAppServices.Interfaces.IKNodeService, SuperAppServices.Services.KNodeService>();
                    services.AddScoped<SuperAppServices.Interfaces.IKNodeHelperService, SuperAppServices.Services.KNodeHelperService>();
                    services.AddScoped<SuperAppServices.Interfaces.IKTestService, SuperAppServices.Services.KTestService>();
                    services.AddScoped<SuperAppServices.Interfaces.IWsService, SuperAppServices.Services.WsService>();
                    services.AddScoped<SuperAppServices.Interfaces.INoteService, SuperAppServices.Services.NoteService>();
                    services.AddScoped<SuperAppServices.Interfaces.IAuthService, SuperAppServices.Services.AuthService>();
                    services.AddScoped<SuperAppServices.Interfaces.IStandardRegistryService, SuperAppServices.Services.StandardRegistryService>();
                    services.AddScoped<SuperAppServices.Interfaces.IUserProfileService, SuperAppServices.Services.UserProfileService>();
                    // TODO: Integrate KeywordServiceV2 with PathIds design
                     //services.AddScoped<SuperAppServices.Interfaces.IKeywordService, SuperAppServices.Services.KeywordService>();
                    services.AddScoped<SuperAppServices.Services.KeywordServiceV2>();
                    services.AddScoped<SuperAppServices.Services.WorkspaceItemPathService>();

                    // Personal Productivity App Services
                    services.AddScoped<SuperAppServices.Interfaces.IProjectService, SuperAppServices.Services.ProjectService>();
                    services.AddScoped<SuperAppServices.Interfaces.ITaskService, SuperAppServices.Services.TaskService>();
                    services.AddScoped<SuperAppServices.Interfaces.ITaskCommentService, SuperAppServices.Services.TaskCommentService>();
                    services.AddScoped<SuperAppDataRepositories.Ins.IFlowRepository, SuperAppDataRepositories.Repositories.FlowRepository>();
                    services.AddScoped<SuperAppServices.Interfaces.IFlowService, SuperAppServices.Services.FlowService>();

                    // LifeLog Services
                    services.AddScoped<SuperAppDataRepositories.Ins.ILifeLogRepository, SuperAppDataRepositories.Repositories.LifeLogRepository>();
                    services.AddScoped<SuperAppServices.Interfaces.ILifeLogService, SuperAppServices.Services.LifeLogService>();

                    // Wiki Services
                    services.AddScoped<SuperAppDataRepositories.Ins.IWikiRepository, SuperAppDataRepositories.Repositories.WikiRepository>();
                    services.AddScoped<SuperAppServices.Interfaces.IWikiService, SuperAppServices.Services.WikiService>();

                    // File Upload Services (Google Drive)
                    services.AddScoped<SuperAppServices.Interfaces.IGoogleDriveService, SuperAppServices.Services.GoogleDriveService>();
                    services.AddScoped<SuperAppServices.Interfaces.IFileService, SuperAppServices.Services.FileService>();

                    // AI Services
                    services.AddSingleton<SuperAppServices.Interfaces.IClaudibleService, SuperAppServices.Services.ClaudibleService>();
                    services.AddScoped<SuperAppServices.Interfaces.IKGradingService, SuperAppServices.Services.KGradingService>();
                    services.AddScoped<SuperAppServices.Interfaces.IKMarkdownImportService, SuperAppServices.Services.KMarkdownImportService>();
                });

    }
}
