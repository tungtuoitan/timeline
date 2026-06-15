using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.BackgroundServices
{
    /// <summary>
    /// Background sync daemon — DB is the source of truth, remote is a mirror.
    ///
    /// <list type="bullet">
    ///   <item>Listens to <see cref="IKSyncEventPublisher"/> channel for K mutations.</item>
    ///   <item>Per-user debounce: 5 s after the last event before pushing.</item>
    ///   <item>A new event during debounce or push <b>cancels the in-flight operation</b>;
    ///         cancellation is honoured at well-defined checkpoints inside
    ///         <c>ForceUpdateRemoteAsync</c>.</item>
    ///   <item>Per-user lock makes overlapping pushes impossible; new events queue and
    ///         the previous waiter is cancelled.</item>
    ///   <item>The daemon does not pull, fetch-and-merge, or auto-resolve conflicts —
    ///         it always force-overwrites the remote with the current DB state. The
    ///         FE polls <c>/compare</c> to surface remote-vs-DB diffs to the user.</item>
    /// </list>
    /// </summary>
    public sealed class KRepoSyncDaemon : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IKSyncEventPublisher _publisher;
        private readonly IKViewerTracker _viewerTracker;
        private readonly ILogger<KRepoSyncDaemon> _logger;

        /// <summary>One CTS per user — cancelled whenever a new event arrives so the
        /// pending debounce or in-flight push aborts and the latest event wins.</summary>
        private readonly ConcurrentDictionary<int, CancellationTokenSource> _userOpCts = new();

        /// <summary>Per-user mutex so only one push runs at a time for a given user.</summary>
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _userLocks = new();

        private static readonly TimeSpan DebounceDelay = TimeSpan.FromSeconds(5);

        public KRepoSyncDaemon(
            IServiceScopeFactory scopeFactory,
            IKSyncEventPublisher publisher,
            IKViewerTracker viewerTracker,
            ILogger<KRepoSyncDaemon> logger)
        {
            _scopeFactory  = scopeFactory  ?? throw new ArgumentNullException(nameof(scopeFactory));
            _publisher     = publisher     ?? throw new ArgumentNullException(nameof(publisher));
            _viewerTracker = viewerTracker ?? throw new ArgumentNullException(nameof(viewerTracker));
            _logger        = logger        ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KRepoSyncDaemon started (debounce={Sec}s, force-push mode)",
                DebounceDelay.TotalSeconds);

            try
            {
                await foreach (var userId in _publisher.Reader.ReadAllAsync(stoppingToken))
                    ScheduleSync(userId, stoppingToken);
            }
            catch (OperationCanceledException) { /* shutdown */ }

            _logger.LogInformation("KRepoSyncDaemon stopped");
        }

        private void ScheduleSync(int userId, CancellationToken stoppingToken)
        {
            // Replace the per-user CTS — cancelling the previous one terminates whichever
            // stage (debounce delay, semaphore wait, or in-flight push) was running.
            var newCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            if (_userOpCts.TryGetValue(userId, out var prev))
            {
                try { prev.Cancel(); } catch { /* already disposed */ }
            }
            _userOpCts[userId] = newCts;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(DebounceDelay, newCts.Token);
                    await RunSyncAsync(userId, newCts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("Daemon: cancelled for user {UserId} (newer event arrived)", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Daemon: sync failed for user {UserId}", userId);
                }
                finally
                {
                    // Only remove if this CTS is still the current one
                    _userOpCts.TryRemove(new KeyValuePair<int, CancellationTokenSource>(userId, newCts));
                    newCts.Dispose();
                }
            }, stoppingToken);
        }

        private async Task RunSyncAsync(int userId, CancellationToken ct)
        {
            // Skip while the user is reviewing diff entries on the FE — pushing
            // would invalidate what they're looking at. The next event after they
            // close the popup re-runs the sync (or the periodic check will).
            if (_viewerTracker.IsViewing(userId))
            {
                _logger.LogInformation("Daemon: user {UserId} is viewing diff — skip force-push", userId);
                return;
            }

            var sem = _userLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));

            // Wait for any previous push to release. If a newer event cancels us
            // while waiting, we exit cleanly — the newer event scheduled its own task.
            await sem.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                // Re-check after acquiring the lock — user may have opened the popup
                // while we were queued behind another push.
                if (_viewerTracker.IsViewing(userId))
                {
                    _logger.LogInformation("Daemon: user {UserId} started viewing while queued — skip force-push", userId);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<IKRepoSyncService>();

                _logger.LogInformation("Daemon: force-pushing DB → remote for user {UserId}", userId);
                var result = await syncService.ForceUpdateRemoteAsync(userId, ct);
                if (!result.Success)
                    _logger.LogWarning("Daemon: force-push for user {UserId} failed: {Msg}",
                        userId, result.Message);
            }
            finally
            {
                sem.Release();
            }
        }

        public override void Dispose()
        {
            foreach (var cts in _userOpCts.Values)
            {
                try { cts.Cancel(); cts.Dispose(); } catch { /* best effort */ }
            }
            _userOpCts.Clear();

            foreach (var sem in _userLocks.Values)
                sem.Dispose();
            _userLocks.Clear();

            base.Dispose();
        }
    }
}
