
using TLDataSes.Ins;
using TLDataSes.Ses;
using TLDataRes.Ins;
using TLDataRes.Res;

namespace TimelineAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            //1. tạo ra 1 host Builder với cấu hình cho trước
            Host.CreateDefaultBuilder(args)
                // 2. cấu hình web host với "default settings"
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>() // 3. dùng file Startup để cấu hình
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

                });
    }
}
