using Baubit.IO.Channels.Reasons;
using Baubit.Tasks;
using Baubit.Tasks.Reasons;
using FluentResults;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Baubit.IO.Channels
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

        public static Result FlushAndDispose<TEvent>(this Channel<TEvent> channel)
        {
            try
            {
                channel.Writer.Complete();
                if (channel.Reader.Count > 0)
                {
                    _ = channel.EnumerateAsync(default).ToBlockingEnumerable().ToArray();
                }
                return Result.Ok();
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        public static async Task<Result> TryWriteWhenReadyAsync<T>(this Channel<T> channel,
                                                                   T item,
                                                                   TimeSpan? maxWaitToWrite,
                                                                   CancellationToken cancellationToken)
        {
            var compositeCancellationTokenSource = new CompositeCancellationTokenSource(maxWaitToWrite, cancellationToken);
            try
            {
                bool waitResult = await channel.Writer.WaitToWriteAsync(compositeCancellationTokenSource.Token);
                if (!waitResult) return Result.Fail("").WithReason(new ClosedForWriting());
                if (channel.Writer.TryWrite(item)) return Result.Ok();
                else return Result.Fail("Unknown write error !");
            }
            catch (OperationCanceledException oExp)
            {
                return Result.Fail(new ExceptionalError(oExp)).WithReason(compositeCancellationTokenSource.CancelledDueToTimeOut == true ? new TimedOut() : new CancelledByCaller());
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
    }
}
