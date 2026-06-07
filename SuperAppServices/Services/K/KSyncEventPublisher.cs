using System.Threading.Channels;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services.K
{
    /// <summary>
    /// Singleton in-process event bus backed by an unbounded <see cref="Channel{T}"/>.
    /// Controllers call <see cref="NotifyChanged"/> after a successful write;
    /// the daemon reads from <see cref="Reader"/> and debounces pushes.
    /// </summary>
    public sealed class KSyncEventPublisher : IKSyncEventPublisher
    {
        private readonly Channel<int> _channel = Channel.CreateUnbounded<int>(
            new UnboundedChannelOptions { SingleReader = true });

        public ChannelReader<int> Reader => _channel.Reader;

        public void NotifyChanged(int userId)
        {
            // Fire-and-forget; TryWrite on unbounded channel always returns true.
            _channel.Writer.TryWrite(userId);
        }
    }
}
