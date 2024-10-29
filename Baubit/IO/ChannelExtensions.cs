using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Baubit.IO
{
    public static class ChannelExtensions
    {
        public static async IAsyncEnumerable<TEvent> EnumerateAsync<TEvent>(this Channel<TEvent> channel, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (await channel.Reader.WaitToReadAsync(cancellationToken) && channel.Reader.TryPeek(out var @event))
            {
                yield return @event;
                _ = await channel.Reader.ReadAsync();
            }
        }

        public static void FlushAndDisposeAsync<TEvent>(this Channel<TEvent> channel)
        {
            channel.Writer.Complete();
            if (channel.Reader.Count > 0)
            {
                _ = channel.EnumerateAsync(default).ToBlockingEnumerable().ToArray();
            }
            channel = null;
        }

        public static async Task<bool> TryWriteWhenReadyAsync<TEvent>(this Channel<TEvent> channel, TEvent @event, params CancellationToken[] cancellationTokens)
        {
            try
            {
                return await channel.Writer.WaitToWriteAsync(CancellationTokenSource.CreateLinkedTokenSource(cancellationTokens.Where(token => token != default).ToArray()).Token) && channel.Writer.TryWrite(@event);
            }
            catch (TaskCanceledException tcExp)
            {
                return false;
            }
            catch (OperationCanceledException ocExp)
            {
                return false;
            }
        }
    }
}
