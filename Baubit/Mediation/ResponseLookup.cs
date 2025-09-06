using Baubit.Caching;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Baubit.Mediation
{
    public class ResponseLookup<TResponse> : IDisposable where TResponse : IResponse
    {
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private ConcurrentDictionary<long, TaskCompletionSource<TResponse>> awaiters = new ConcurrentDictionary<long, TaskCompletionSource<TResponse>>();
        private Task<bool> cacheReader;

        public ResponseLookup(IOrderedCache<TResponse> cache,
                              ILoggerFactory loggerFactory)
        {
            cacheReader = cache.EnumerateEntriesAsync(null, cancellationTokenSource.Token)
                               .AggregateAsync(entry =>
                               {
                                   awaiters.GetOrAdd(entry.Value.ForRequest, static _ => new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously))
                                           .SetResult(entry.Value);
                                   cache.Remove(entry.Id, out _);
                                   return true;
                               }, cancellationTokenSource.Token);
        }

        public async Task<TResponse> GetResponseAsync(long forRequestId, CancellationToken cancellationToken = default)
        {
            if (!awaiters.TryGetValue(forRequestId, out var awaiter))
            {
                awaiter = awaiters.GetOrAdd(forRequestId, static _ => new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously));
            }
            var response = await awaiter.Task.WaitAsync(cancellationToken);
            awaiters.Remove(forRequestId, out _);
            return response;
        }

        public void Dispose()
        {
            cancellationTokenSource.Cancel();
        }
    }
}
