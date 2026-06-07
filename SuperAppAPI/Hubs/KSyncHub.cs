using Microsoft.AspNetCore.SignalR;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Hubs
{
    public class KSyncHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            await base.OnConnectedAsync();
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
