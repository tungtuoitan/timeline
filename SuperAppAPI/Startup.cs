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
            });
            services.AddLogging(config =>
            {
                config.AddConsole();
                config.AddDebug();
                // Other logging providers
            });

            // phần này là cấu hình CORS
            string[] localMachines = { "VTHKNB01", "VTHKNB02", "VTVNNB60", "VTVNNB70", "VTVNNB06" };
            var isLocal = Array.Find(localMachines, l => l == Dns.GetHostName());
            var serverUrl = (isLocal != null) ? "LocalWebUI" : "AzureWebUI";
            var webui = new string[] { Configuration.GetValue<string>(serverUrl) };
            services.AddCors(o =>
            {
                o.AddPolicy("WebAPIPolicy", builder =>
                {
                    builder.SetIsOriginAllowedToAllowWildcardSubdomains()
                    .WithOrigins("http://localhost:3000",
                                 "http://localhost:3001",
                                 "http://localhost:3003",
                                 "*.vanthiel.com")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .SetIsOriginAllowed(_ => true)
                    .AllowCredentials();
                });
            });

            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // set max body của request là 100mb
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

            // sử dụng cấu hình CORS đã khai báo ở trên
            app.UseCors("WebAPIPolicy");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SuperApp v1"));
            }

            //if (env.IsProduction())
            //{
            //    app.UseHttpsRedirection();
            //}

            //app.UseExceptionHandler(new ExceptionHandlerOptions
            //{
            //    ExceptionHandler = new JsonExceptionMiddleware().Invoke
            //});

            app.UseRouting();

            //app.UseAuthentication();
            //app.UseAuthorization();

            app.UseSession(); // thêm middleware để quản lí session
            // TEMPORARILY DISABLED: Google Token validation middleware
            // app.UseGoogleTokenValidation();

            // thêm middleware cho quản lí session
            app.UseExceptionHandler(a => a.Run(async context =>
            {
                var error = context.Features.Get<IExceptionHandlerFeature>().Error;
                var problem = new ProblemDetails { Title = "Critical Error" };
                if (error != null)
                {
                    if (env.IsDevelopment())
                    {
                        problem.Title = error.Message;
                        problem.Detail = error.StackTrace;
                    }
                    else
                        problem.Detail = error.Message;

                    // get session name
                    string name = string.Empty;
                    if (context.Session.TryGetValue("email", out byte[] nameBytes))
                    {
                        name = Encoding.UTF8.GetString(nameBytes);
                    }

                    // appsettings, timezone
                    string baseTimezone = System.OperatingSystem.IsWindows() ? Configuration.GetValue<string>("appSettings:timezone") : Configuration.GetValue<string>("appSettings:timezone2");
                }

                await context.Response.WriteAsJsonAsync(problem);

            }));





            //app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Welcome to SuperApp API");
                });
            });
            app.UseCors("AllowReactApp");
        }
    }
}
