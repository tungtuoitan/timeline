using DotNetEnv;
using Serilog;

namespace SuperAppAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var currentDir = Directory.GetCurrentDirectory();
            var envPath = Path.Combine(currentDir, ".env");

            if (!File.Exists(envPath))
            {
                var parentDir = Directory.GetParent(currentDir)?.FullName;
                if (parentDir != null)
                    envPath = Path.Combine(parentDir, ".env");
            }

            if (File.Exists(envPath))
            {
                Env.Load(envPath);
                Console.WriteLine($"✓ Loaded .env file from: {envPath}");
            }
            else
            {
                Console.WriteLine($"✗ Warning: .env file not found at: {envPath}");
                Console.WriteLine($"  Searched in: {currentDir}");
                if (Directory.GetParent(currentDir) != null)
                    Console.WriteLine($"  and: {Directory.GetParent(currentDir)?.FullName}");
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

            try
            {
                Log.Information(">>  >>  >>  Starting up the SuperApp application...");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
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
                .ConfigureAppConfiguration((_, config) =>
                {
                    config.AddEnvironmentVariables();
                });
    }
}
