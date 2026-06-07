namespace SuperAppServices.Interfaces
{
    /// <summary>
    /// Abstraction for pushing real-time sync status notifications to a specific user.
    /// Implemented in SuperAppAPI via SignalR; can be stubbed in tests.
    /// </summary>
    public interface IKSyncNotifier
    {
        Task NotifyAsync(int userId, string status, string? message, string direction);
    }
}
