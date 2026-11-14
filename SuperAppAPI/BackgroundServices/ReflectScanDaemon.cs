using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperAppModels.Time;
using SuperAppServices.Interfaces.Reflect;

namespace SuperAppAPI.BackgroundServices
{
    /// <summary>
    /// Runs the Reflect scan once a day, a little after the evening check-in. Each row is read once
    /// (see <see cref="ReflectScanService"/>), so a missed day is simply caught up by the next run.
    /// </summary>
    public sealed class ReflectScanDaemon : BackgroundService
    {
        /// <summary>Local time of day (server default timezone) the scan starts</summary>
        private static readonly TimeSpan RunAt = new(23, 30, 0);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReflectScanDaemon> _logger;

        public ReflectScanDaemon(IServiceScopeFactory scopeFactory, ILogger<ReflectScanDaemon> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReflectScanDaemon started (runs daily at {Time} {Zone})", RunAt, TimeZones.Default.Id);
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(UntilNextRun(DateTime.UtcNow), stoppingToken);
                    await RunOnceAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) { /* shutdown */ }
        }

        private async Task RunOnceAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var scanned = await scope.ServiceProvider.GetRequiredService<IReflectScanService>().ScanAllAsync(ct);
                _logger.LogInformation("Reflect scan finished: {Count} rows read", scanned);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Reflect scan run failed");
            }
        }

        /// <summary>Time from <paramref name="utcNow"/> until the next RunAt in the default timezone</summary>
        private static TimeSpan UntilNextRun(DateTime utcNow)
        {
            var tz = TimeZones.Default;
            var local = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
            var next = local.Date + RunAt;
            if (next <= local) next = next.AddDays(1);
            return next - local;
        }
    }
}
