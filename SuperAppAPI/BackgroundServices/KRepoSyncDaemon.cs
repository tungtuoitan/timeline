using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.BackgroundServices
{
    /// <summary>
    /// Real-time sync daemon that watches for both DB mutations and remote git changes,
    /// then synchronises them automatically. Conflict strategy: DB always wins.
    ///
    /// <list type="bullet">
    ///   <item><b>DB change</b> (via <see cref="IKSyncEventPublisher"/> Channel):
    ///         debounce 5 s, then push DB -> repo.</item>
    ///   <item><b>Remote change</b> (git fetch every 2 min):
    ///         if remote ahead, push DB first (DB wins) then pull remote -> DB.</item>
    /// </list>
    ///
    /// Per-user <see cref="SemaphoreSlim"/> prevents overlapping syncs for the same user.
    /// </summary>
    public sealed class KRepoSyncDaemon : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IKSyncEventPublisher _publisher;
        private readonly ILogger<KRepoSyncDaemon> _logger;

        private readonly ConcurrentDictionary<int, SemaphoreSlim> _userLocks = new();
        private readonly ConcurrentDictionary<int, CancellationTokenSource> _debounceTimers = new();

        /// <summary>How long to wait after the last DB change before pushing.</summary>
        private static readonly TimeSpan DebounceDelay = TimeSpan.FromSeconds(5);

        /// <summary>How often to poll remote repos for new commits.</summary>
        private static readonly TimeSpan RemotePollInterval = TimeSpan.FromMinutes(2);

        public KRepoSyncDaemon(
            IServiceScopeFactory scopeFactory,
            IKSyncEventPublisher publisher,
            ILogger<KRepoSyncDaemon> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _publisher    = publisher    ?? throw new ArgumentNullException(nameof(publisher));
            _logger       = logger       ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KRepoSyncDaemon started (debounce={Debounce}s, poll={Poll}s)",
                DebounceDelay.TotalSeconds, RemotePollInterval.TotalSeconds);

            var dbTask     = ListenDbChangesAsync(stoppingToken);
            var remoteTask = PollRemoteAsync(stoppingToken);

            await Task.WhenAll(dbTask, remoteTask);

            _logger.LogInformation("KRepoSyncDaemon stopped");
        }

        // ── DB change listener ──────────────────────────────────────────────

        private async Task ListenDbChangesAsync(CancellationToken ct)
        {
            try
            {
                await foreach (var userId in _publisher.Reader.ReadAllAsync(ct))
                {
                    DebouncePush(userId, ct);
                }
            }
            catch (OperationCanceledException) { /* shutdown */ }
        }

        /// <summary>
        /// (Re-)schedules a debounced push for <paramref name="userId"/>.
        /// If a previous timer is pending it is cancelled so rapid edits
        /// are batched into a single push.
        /// </summary>
        private void DebouncePush(int userId, CancellationToken ct)
        {
            // Cancel any pending debounce for this user
            if (_debounceTimers.TryRemove(userId, out var existing))
            {
                existing.Cancel();
                existing.Dispose();
            }

            var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _debounceTimers[userId] = cts;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(DebounceDelay, cts.Token);
                    _debounceTimers.TryRemove(userId, out _);

                    _logger.LogInformation("KRepoSyncDaemon: pushing DB->repo for user {UserId} (debounced)", userId);

                    await ExecuteWithLock(userId, async sp =>
                    {
                        var syncService = sp.GetRequiredService<IKRepoSyncService>();
                        await syncService.PushToRepoAsync(userId);
                    }, ct);
                }
                catch (OperationCanceledException) { /* debounce reset or shutdown */ }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "KRepoSyncDaemon: debounced push failed for user {UserId}", userId);
                }
            }, ct);
        }

        // ── Remote poller ───────────────────────────────────────────────────

        private async Task PollRemoteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                // Wait first — give the app time to start up
                try { await Task.Delay(RemotePollInterval, ct); }
                catch (OperationCanceledException) { break; }

                try
                {
                    // Discover all users with a configured repo
                    List<int> userIds;
                    using (var listScope = _scopeFactory.CreateScope())
                    {
                        var db = listScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        userIds = await db.UserProfiles
                            .Where(p => p.KRepoUrl != null && p.KRepoUrl != ""
                                     && p.KRepoStatusCode != "conflict")
                            .Select(p => p.UserId)
                            .ToListAsync(ct);
                    }

                    foreach (var userId in userIds)
                    {
                        if (ct.IsCancellationRequested) break;

                        try
                        {
                            await ExecuteWithLock(userId, async sp =>
                            {
                                var syncService = sp.GetRequiredService<IKRepoSyncService>();

                                // Step 1: fetch remote HEAD and update status
                                await syncService.CheckAndUpdateStatusAsync(userId);

                                // Step 2: re-read profile to see if remote is ahead
                                var profileRepo = sp.GetRequiredService<IUserProfileRepository>();
                                var profile = await profileRepo.GetByUserIdAsync(userId);

                                if (profile?.KRepoStatusCode == "behind")
                                {
                                    _logger.LogInformation(
                                        "KRepoSyncDaemon: remote ahead — auto-pulling for user {UserId}", userId);
                                    // PullFromRepoAsync pushes DB first (DB wins) then applies remote
                                    await syncService.PullFromRepoAsync(userId);
                                }
                            }, ct);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "KRepoSyncDaemon: poll error for user {UserId}", userId);
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "KRepoSyncDaemon: remote poll cycle error");
                }
            }
        }

        // ── Lock helper ─────────────────────────────────────────────────────

        /// <summary>
        /// Acquires a per-user lock (non-blocking) and runs <paramref name="action"/>
        /// inside a fresh DI scope. If the lock is already held (another sync for this
        /// user is in progress), the call is silently skipped.
        /// </summary>
        private async Task ExecuteWithLock(int userId, Func<IServiceProvider, Task> action, CancellationToken ct)
        {
            var sem = _userLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));

            if (!await sem.WaitAsync(TimeSpan.Zero, ct))
            {
                _logger.LogDebug("KRepoSyncDaemon: skipping user {UserId} — sync already in progress", userId);
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                await action(scope.ServiceProvider);
            }
            finally
            {
                sem.Release();
            }
        }

        // ── Cleanup ─────────────────────────────────────────────────────────

        public override void Dispose()
        {
            foreach (var cts in _debounceTimers.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _debounceTimers.Clear();

            foreach (var sem in _userLocks.Values)
                sem.Dispose();
            _userLocks.Clear();

            base.Dispose();
        }
    }
}
