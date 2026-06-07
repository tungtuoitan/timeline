using System.Threading.Channels;

namespace SuperAppServices.Interfaces
{
    /// <summary>
    /// Publishes K-entity change events (create/update/delete of Knowledge, Node, Question)
    /// to a background sync daemon. Registered as a singleton so the same Channel is shared
    /// between API controllers (writers) and the daemon (reader).
    /// </summary>
    public interface IKSyncEventPublisher
    {
        /// <summary>
        /// Signal that user <paramref name="userId"/> has mutated K data in the DB.
        /// The daemon will debounce and push DB -> repo.
        /// </summary>
        void NotifyChanged(int userId);

        /// <summary>Reader end of the channel — consumed exclusively by <c>KRepoSyncDaemon</c>.</summary>
        ChannelReader<int> Reader { get; }
    }
}
