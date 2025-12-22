using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Text;
using Serilog;
using SuperAppAPI.Middlewares;

namespace SuperAppAPI
{
    public class Startup // file startup là file quan trọng, chịu trách nhiệm cấu hình service và request.pipline
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddAutoMapper(typeof(DocumentServiceAutoMapperConfiguration));

            //services.AddDbContext<PLMDBContext>(
            //options => options.UseSqlServer(ApplicationSettings.SuperAppConnectionString));

            services.AddMemoryCache();

            services.AddControllers();
            services.AddHttpClient();
            //services.AddCors(options =>
            //{
            //    options.AddPolicy("AllowReactApp", policy =>
            //    {
            //        policy.WithOrigins("http://localhost:3000")
            //              .AllowAnyHeader()
            //              .AllowAnyMethod();
            //    });
            //});
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "SuperApp", Version = "v1" });
                
                // TEMPORARY: JWT Authentication to Swagger DISABLED for development
                /*
                // Add JWT Authentication to Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
                */
            });
            services.AddLogging(config =>
            {
                config.AddConsole();
                config.AddDebug();
                // Other logging providers
            });

            // CORS Configuration - Environment-specific for security
            services.AddCors(options =>
            {
                // Development policy - more permissive for local development
                options.AddPolicy("DevelopmentPolicy", builder =>
                {
                    builder.WithOrigins(
                            "http://localhost:3000",
                            "http://localhost:3001", 
                            "http://localhost:3003",
                            "http://localhost:5000")
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                });

                // Production policy - restrictive for security
                options.AddPolicy("ProductionPolicy", builder =>
                {
                    builder.WithOrigins(Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                           .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                           .WithHeaders("Content-Type", "Authorization", "X-Requested-With")
                           .AllowCredentials()
                           .SetIsOriginAllowedToAllowWildcardSubdomains();
                });
            });

            // Configure form options with security limits
            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50MB limit (reduced from 100MB for security)
                options.ValueLengthLimit = 1024 * 1024; // 1MB per form value
                options.KeyLengthLimit = 1024; // 1KB per form key
                options.MemoryBufferThreshold = 64 * 1024; // 64KB buffer threshold
            });

            // Add security headers configuration
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            services.AddHttpContextAccessor(); // cho phép dùng httpContext trong service

            //services.AddTransient<IBlobAppend, BlobAppend>();
            //services.AddTransient<ILoggerService, LoggerService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env
            //, ILoggerService loggerService
            )
        {
            //ILoggerService _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));

            // TEMPORARY: Security headers DISABLED for development debugging
            // app.UseSecurityHeaders();

            // Environment-specific CORS policy
            if (env.IsDevelopment())
            {
                app.UseCors("DevelopmentPolicy");
            }
            else
            {
                app.UseCors("ProductionPolicy");
            }

            // HTTPS Redirection and Security - production only
            if (!env.IsDevelopment())
            {
                app.UseHttpsRedirection();
                app.UseHsts(); // HTTP Strict Transport Security
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SuperApp v1"));
            }

            //app.UseExceptionHandler(new ExceptionHandlerOptions
            //{
            //    ExceptionHandler = new JsonExceptionMiddleware().Invoke
            //});

            app.UseRouting();

            // Global Exception Handler - Must be early in the pipeline
            app.UseGlobalExceptionHandler();

            app.UseSession(); // Session management middleware

            // Authentication and Authorization - MUST be in this order and after routing
            app.UseAuthentication();  // Must come before UseAuthorization
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Welcome to SuperApp API");
                });
            });
            // CORS is already configured above - this line was duplicate and incorrect
        }
    }
}
