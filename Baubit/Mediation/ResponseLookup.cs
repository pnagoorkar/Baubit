using Baubit.Caching;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Baubit.Mediation
{
    /// <summary>
    /// Provides a response awaiting facility for the asynchronous mediator pipeline.
    /// It observes a typed response cache and completes awaiting tasks when a response
    /// matching a given request id appears.
    /// </summary>
    /// <typeparam name="TResponse">The response type handled by this lookup.</typeparam>
    public class ResponseLookup<TResponse> : IDisposable where TResponse : IResponse
    {
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private ConcurrentDictionary<long, TaskCompletionSource<TResponse>> awaiters = new ConcurrentDictionary<long, TaskCompletionSource<TResponse>>();
        private Task<bool> cacheReader;

        /// <summary>
        /// Creates a new response lookup bound to a response cache.
        /// </summary>
        /// <param name="cache">The ordered cache that stores responses.</param>
        /// <param name="loggerFactory">Factory for creating loggers (reserved for future diagnostics).</param>
        public ResponseLookup(IOrderedCache<TResponse> cache,
                              ILoggerFactory loggerFactory)
        {
            cacheReader = cache.EnumerateEntriesAsync(null, cancellationTokenSource.Token)
                               .AggregateAsync(entry =>
                               {
                                   var awaiter = awaiters.GetOrAdd(entry.Value.ForRequest, static _ => new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously));
                                   awaiter.SetResult(entry.Value);
                                   cache.Remove(entry.Id, out _);
                                   return true;
                               }, cancellationTokenSource.Token);
        }

        /// <summary>
        /// Asynchronously waits for a response whose <see cref="IResponse.ForRequest"/> equals <paramref name="forRequestId"/>.
        /// </summary>
        /// <param name="forRequestId">The id of the request we are awaiting a response for.</param>
        /// <param name="cancellationToken">A token to cancel the wait.</param>
        /// <returns>A task that completes with the matching response.</returns>
        public async Task<TResponse> GetResponseAsync(long forRequestId, CancellationToken cancellationToken = default)
        {
            var awaiter = awaiters.GetOrAdd(forRequestId, static _ => new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously));
            var response = await awaiter.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            awaiters.Remove(forRequestId, out _);
            return response;
        }

        /// <summary>
        /// Stops observing the cache and cancels all pending awaiters.
        /// </summary>
        public void Dispose()
        {
            cancellationTokenSource.Cancel();
        }
    }
}
