using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Hubs
{
    public class KSyncHub : Hub
    {
        private readonly IKViewerTracker _viewerTracker;
        private readonly ILogger<KSyncHub> _logger;

        public KSyncHub(IKViewerTracker viewerTracker, ILogger<KSyncHub> logger)
        {
            _viewerTracker = viewerTracker ?? throw new ArgumentNullException(nameof(viewerTracker));
            _logger        = logger        ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _viewerTracker.OnDisconnect(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        public Task StartViewing()
        {
            if (int.TryParse(Context.UserIdentifier, out var userId))
            {
                _viewerTracker.StartViewing(userId, Context.ConnectionId);
                _logger.LogInformation("KSyncHub: user {UserId} StartViewing (conn {Conn})", userId, Context.ConnectionId);
            }
            return Task.CompletedTask;
        }

        public Task StopViewing()
        {
            if (int.TryParse(Context.UserIdentifier, out var userId))
            {
                _viewerTracker.StopViewing(userId, Context.ConnectionId);
                _logger.LogInformation("KSyncHub: user {UserId} StopViewing (conn {Conn})", userId, Context.ConnectionId);
            }
            return Task.CompletedTask;
        }
    }

    public record KSyncStatusMessage(string Status, string? Message, string Direction);

    /// <summary>
    /// SignalR implementation of IKSyncNotifier — sends real-time status updates to the user's hub group.
    /// Registered as a scoped service in Startup so it can access IHubContext.
    /// </summary>
    public class SignalRKSyncNotifier : IKSyncNotifier
    {
        private readonly IHubContext<KSyncHub> _hub;

        public SignalRKSyncNotifier(IHubContext<KSyncHub> hub)
        {
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));
        }

        public async Task NotifyAsync(int userId, string status, string? message, string direction)
        {
            await _hub.Clients
                .Group($"user-{userId}")
                .SendAsync("UpdateSyncStatus", new KSyncStatusMessage(status, message, direction));
        }
    }
}
