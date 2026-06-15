using System.Collections.Concurrent;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Hubs
{
    /// <summary>
    /// Singleton tracker — one map per user → set of connection ids currently viewing.
    /// Concurrent because hub callbacks and the daemon read/write from different threads.
    /// </summary>
    public sealed class KViewerTracker : IKViewerTracker
    {
        // userId → set of connection ids currently viewing.
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, byte>> _byUser = new();
        // connectionId → userId (so OnDisconnect can clean up without knowing the user).
        private readonly ConcurrentDictionary<string, int> _byConnection = new();

        public void StartViewing(int userId, string connectionId)
        {
            var set = _byUser.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
            set[connectionId] = 0;
            _byConnection[connectionId] = userId;
        }

        public void StopViewing(int userId, string connectionId)
        {
            if (_byUser.TryGetValue(userId, out var set))
            {
                set.TryRemove(connectionId, out _);
                if (set.IsEmpty) _byUser.TryRemove(userId, out _);
            }
            _byConnection.TryRemove(connectionId, out _);
        }

        public void OnDisconnect(string connectionId)
        {
            if (_byConnection.TryRemove(connectionId, out var userId))
                StopViewing(userId, connectionId);
        }

        public bool IsViewing(int userId) =>
            _byUser.TryGetValue(userId, out var set) && !set.IsEmpty;
    }
}
