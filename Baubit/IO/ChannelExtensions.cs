using FluentResults;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Baubit.IO
{
    public static class ChannelExtensions
    {
        public static async Task ReadAsync<T>(this Channel<T> channel,
                                              Func<T, CancellationToken, Task> handler,
                                              CancellationToken cancellationToken)
        {
            await foreach (var item in channel.EnumerateAsync(cancellationToken))
            {
                try
                {
                    await handler(item, cancellationToken);
                }
                catch
                {
                    //handlers must never throw exceptions.
                }
            }
        }

        public static async Task ReadAsync<T>(this Channel<T> channel,
                                              Action<T> handler,
                                              CancellationToken cancellationToken)
        {
            await foreach (var item in channel.EnumerateAsync(cancellationToken))
            {
                try
                {
                    handler(item);
                }
                catch
                {
                    //handlers must never throw exceptions.
                }
            }
        }

        public static async IAsyncEnumerable<T> EnumerateAsync<T>(this Channel<T> channel, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            bool canRead = true;
            while (canRead)
            {
                try
                {
                    canRead = await channel.Reader.WaitToReadAsync(cancellationToken);
                }
                catch (OperationCanceledException exp)
                {
                    break;
                }
                yield return await channel.Reader.ReadAsync(cancellationToken);
            }
        }

        public static void FlushAndDispose<TEvent>(this Channel<TEvent> channel)
        {
            channel.Writer.Complete();
            if (channel.Reader.Count > 0)
            {
                _ = channel.EnumerateAsync(default).ToBlockingEnumerable().ToArray();
            }
        }

        public static async Task<Result> TryWriteWhenReadyAsync<T>(this Channel<T> channel, 
                                                                   T item, 
                                                                   int maxWaitToWriteMS, 
                                                                   CancellationToken cancellationToken)
        {
            try
            {
                var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, new CancellationTokenSource(maxWaitToWriteMS).Token).Token;
                bool waitResult = await channel.Writer.WaitToWriteAsync(linkedCancellationToken);
                if (!waitResult)
                {
                    return Result.Fail("Channel closed for writing !");
                }
                if (channel.Writer.TryWrite(item))
                {
                    return Result.Ok();
                }
                else
                {
                    return Result.Fail("Unknown write error !");
                }
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
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
