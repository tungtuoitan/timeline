using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.BackgroundServices
{
    public class KRepoSyncBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IKViewerTracker _viewerTracker;
        private readonly ILogger<KRepoSyncBackgroundService> _logger;

        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan PushInterval  = TimeSpan.FromHours(1);

        private DateTime _lastPush = DateTime.MinValue;

        public KRepoSyncBackgroundService(
            IServiceScopeFactory scopeFactory,
            IKViewerTracker viewerTracker,
            ILogger<KRepoSyncBackgroundService> logger)
        {
            _scopeFactory  = scopeFactory  ?? throw new ArgumentNullException(nameof(scopeFactory));
            _viewerTracker = viewerTracker ?? throw new ArgumentNullException(nameof(viewerTracker));
            _logger        = logger        ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KRepoSyncBackgroundService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(CheckInterval, stoppingToken);

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<IKRepoSyncService>();

                    // Per-user CheckAllUsers / PushAllUsers iterate users themselves —
                    // we filter at the call site by skipping users currently viewing
                    // the diff popup so their compare/push state stays stable.
                    await syncService.CheckAllUsersAsync(_viewerTracker);

                    if (DateTime.UtcNow - _lastPush >= PushInterval)
                    {
                        _lastPush = DateTime.UtcNow;
                        await syncService.PushAllUsersAsync(_viewerTracker);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "KRepoSyncBackgroundService unhandled error");
                }
            }

            _logger.LogInformation("KRepoSyncBackgroundService stopped");
        }
    }
}
