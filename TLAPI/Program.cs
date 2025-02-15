
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
            //1. tạo ra 1 host Builder với cấu hình cho trước
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                // 2. cấu hình web host với "default settings"
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                    .UseStartup<Startup>() // 3. dùng file Startup để cấu hình
                    //.UseUrls("https://0.0.0.0:5000")
                    .ConfigureKestrel(options =>
                     {
                         options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 3.1 set max body size của request là 100MB
                     });
                })
                .ConfigureServices((_, services) =>
                {
                    services.AddDistributedMemoryCache(); // 3.2
                    services.AddSession(o => // 3.3 cấu hình Session: 30'
                    {
                        o.IdleTimeout = TimeSpan.FromMinutes(30);
                        o.Cookie.HttpOnly = true;
                        o.Cookie.IsEssential = true;
                    });

                    services.AddHttpContextAccessor(); // 3.4 cấu hình này cho phép ta truy cập vào HttpContext

                    services.Add(ServiceDescriptor.Scoped<IEvRe, EvRe>());
                    services.Add(ServiceDescriptor.Scoped<IEvSe, EvSe>());
                    services.Add(ServiceDescriptor.Scoped<ISRsSe, SRsSe>());
                    services.Add(ServiceDescriptor.Scoped<ISRsRe, SRsRe>());
                    services.Add(ServiceDescriptor.Scoped<IPrRe, PrRe>());
                    services.Add(ServiceDescriptor.Scoped<IPRSe, PrSe>());
                    services.Add(ServiceDescriptor.Scoped<IPrFilterRe, PrFilterRe>());
                    services.Add(ServiceDescriptor.Scoped<IPrFilterSe, PrFilterSe>());
                    services.Add(ServiceDescriptor.Scoped<IUserProfileRe, UserProfileRe>());
                    services.Add(ServiceDescriptor.Scoped<IUserProfileSe, UserProfileSe>());

                    services.Add(ServiceDescriptor.Scoped<IFoRe, FoRe>());
                    services.Add(ServiceDescriptor.Scoped<IFoSe, FoSe>());

                });
    }
}
