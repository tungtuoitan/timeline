using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SuperAppAPI.Middlewares;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppDataRepositories.Repositories;
using SuperAppServices.Services.Auth;
using SuperAppServices.Services.Files;
using SuperAppServices.Services.Flow;
using SuperAppServices.Services.K;
using SuperAppServices.Services.Keywords;
using SuperAppServices.Services.LifeLog;
using SuperAppServices.Services.Projects;
using SuperAppServices.Services.Registry;
using SuperAppServices.Services.Profile;
using SuperAppServices.Services.Wiki;
using SuperAppServices.Services.Workspaces;
using System.Text;
using System.Threading.RateLimiting;

namespace SuperAppAPI
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Infrastructure
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();
            services.AddSession(o =>
            {
                o.IdleTimeout = TimeSpan.FromMinutes(30);
                o.Cookie.HttpOnly = true;
                o.Cookie.IsEssential = true;
            });
            services.AddHttpContextAccessor();
            services.AddHttpClient();
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            // JWT Authentication
            var key = Encoding.UTF8.GetBytes(Configuration["Jwt:Key"] ?? "");
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

            // Controllers & Swagger
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "SuperApp", Version = "v1" });
                c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
            });
            services.AddLogging(config =>
            {
                config.AddConsole();
                config.AddDebug();
            });

            // CORS
            services.AddCors(options =>
            {
                options.AddPolicy("DevelopmentPolicy", builder =>
                {
                    builder.WithOrigins(
                            "http://localhost:3000",
                            "http://localhost:3001",
                            "http://localhost:3003",
                            "http://localhost:5000",
                            "https://unparcelled-geralyn-deutoplasmic.ngrok-free.dev",
                            "https://aeronautically-undanceable-rebecka.ngrok-free.dev",
                            "https://www.tungle.uk"
                            )
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                });

                options.AddPolicy("ProductionPolicy", builder =>
                {
                    builder.WithOrigins(
                            "http://157.66.101.51",
                            "https://157.66.101.51",
                            "https://www.tungle.uk"
                            )
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                });
            });

            // Rate limiting — protects bcrypt-heavy login from thrashing
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy("login", httpContext =>
                {
                    var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromSeconds(1),
                        QueueLimit = 0,
                    });
                });
            });

            // Form options & Security
            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 50 * 1024 * 1024;
                options.ValueLengthLimit = 1024 * 1024;
                options.KeyLengthLimit = 1024;
                options.MemoryBufferThreshold = 64 * 1024;
            });
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            // Database
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                var connectionString = Configuration.GetConnectionString("SuperAppConnection");

                if (string.IsNullOrEmpty(connectionString))
                    throw new InvalidOperationException(
                        "Connection string 'SuperAppConnection' is not configured. " +
                        "Please ensure the .env file exists and contains ConnectionStrings__SuperAppConnection.");

                Log.Information("Using connection string: {ConnectionString}",
                    connectionString.Replace(connectionString.Split("Password=")[1].Split(";")[0], "***"));

                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                    sqlOptions.CommandTimeout(120);
                });

                if (_env.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });

            services.AddScoped<IConnectionFactory, ConnectionFactory>();

            // Repositories
            services.AddScoped<INoteRepository, NoteRepository>();
            services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
            services.AddScoped<IKKnowledgeRepository, KKnowledgeRepository>();
            services.AddScoped<IKQuestionRepository, KQuestionRepository>();
            services.AddScoped<IKStatusHistoryRepository, KStatusHistoryRepository>();
            services.AddScoped<IWsRepository, WsRepository>();
            services.AddScoped<IStandardRegistryRepository, StandardRegistryRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserProfileRepository, UserProfileRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<ITargetKeywordRepository, TargetKeywordRepository>();
            services.AddScoped<ITaskCommentRepository, TaskCommentRepository>();
            services.AddScoped<IFlowRepository, FlowRepository>();
            services.AddScoped<ILifeLogRepository, LifeLogRepository>();
            services.AddScoped<IWikiRepository, WikiRepository>();

            // Services
            services.AddScoped<SuperAppServices.Interfaces.IWorkspaceService, WorkspaceService>();
            services.AddScoped<SuperAppServices.Interfaces.IWorkspaceItemService, WorkspaceItemService>();
            services.AddScoped<SuperAppServices.Interfaces.IWorkspaceItemHelperService, WorkspaceItemHelperService>();
            services.AddScoped<SuperAppServices.Interfaces.IKKnowledgeService, KKnowledgeService>();
            services.AddScoped<SuperAppServices.Interfaces.IKNodeService, KNodeService>();
            services.AddScoped<SuperAppServices.Interfaces.IKNodeHelperService, KNodeHelperService>();
            services.AddScoped<SuperAppServices.Interfaces.IKQuestionService, KQuestionService>();
            services.AddScoped<SuperAppServices.Interfaces.IWsService, WsService>();
            services.AddScoped<SuperAppServices.Interfaces.INoteService, NoteService>();
            services.AddScoped<SuperAppServices.Interfaces.IAuthService, AuthService>();
            services.AddScoped<SuperAppServices.Interfaces.IStandardRegistryService, StandardRegistryService>();
            services.AddScoped<SuperAppServices.Interfaces.IUserProfileService, UserProfileService>();
            services.AddScoped<KeywordServiceV2>();
            services.AddScoped<WorkspaceItemPathService>();
            services.AddScoped<SuperAppServices.Interfaces.IProjectService, ProjectService>();
            services.AddScoped<SuperAppServices.Interfaces.ITaskService, TaskService>();
            services.AddScoped<SuperAppServices.Interfaces.ITaskCommentService, TaskCommentService>();
            services.AddScoped<SuperAppServices.Interfaces.IFlowService, FlowService>();
            services.AddScoped<SuperAppServices.Interfaces.ILifeLogService, LifeLogService>();
            services.AddScoped<SuperAppServices.Interfaces.IWikiService, WikiService>();
            services.AddScoped<SuperAppServices.Interfaces.IGoogleDriveService, GoogleDriveService>();
            services.AddScoped<SuperAppServices.Interfaces.IFileService, FileService>();
            services.AddSingleton<SuperAppServices.Interfaces.IClaudibleService, ClaudibleService>();
            services.AddScoped<SuperAppServices.Interfaces.IKGradingService, KGradingService>();
            services.AddScoped<SuperAppServices.Interfaces.IKMarkdownImportService, KMarkdownImportService>();
        }

        public void Configure(IApplicationBuilder app)
        {
            if (_env.IsDevelopment())
            {
                app.UseCors("DevelopmentPolicy");
            }
            else
            {
                app.UseCors("ProductionPolicy");
            }

            if (!_env.IsDevelopment())
            {
                app.UseHttpsRedirection();
                app.UseHsts();
            }

            if (_env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SuperApp v1"));
            }

            app.UseRouting();
            app.UseGlobalExceptionHandler();
            app.UseSession();
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Welcome to SuperApp API");
                });
            });
        }
    }
}
