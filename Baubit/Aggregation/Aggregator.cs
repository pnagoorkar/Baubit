using Baubit.Caching;
using Baubit.Collections;
using Baubit.Observation;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Baubit.Aggregation
{
    public class Aggregator<T> : IAggregator<T>
    {
        public bool CanPublish { get => trackedIndices.Count > 0; }
        private bool disposedValue;
        protected IOrderedCache<T> _cache;

        ConcurrentList<TrackedIndex> trackedIndices = new ConcurrentList<TrackedIndex>();
        private ILogger<Aggregator<T>> _logger;

        public Aggregator(IOrderedCache<T> cache, 
                          ILoggerFactory loggerFactory)
        {
            _cache = cache;
            _logger = loggerFactory.CreateLogger<Aggregator<T>>();
        }

        public bool Publish(T item, out long? trackingId)
        {
            trackingId = null;
            if (CanPublish)
            {
                if (_cache.Add(item, out var entry))
                {
                    trackingId = entry.Id;
                    return true;
                }
            }
            return false;
        }

        private ConcurrentDictionary<long, TaskCompletionSource<bool>> deliveryAwaiters = new ConcurrentDictionary<long, TaskCompletionSource<bool>>();

        public async Task<bool> AwaitDeliveryAsync(long trackingId, CancellationToken cancellationToken = default)
        {
            if (!deliveryAwaiters.TryGetValue(trackingId, out var taskCompletionSource))
            {
                taskCompletionSource = deliveryAwaiters.GetOrAdd(trackingId, static _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
            }
            return await taskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> SubscribeAsync<TItem>(ISubscriber<TItem> subscriber,
                                                      CancellationToken cancellationToken = default) where TItem : T
        {
            var trackedIndex = StartTracking();

            var retVal = await _cache.EnumerateFutureEntriesAsync(cancellationToken)
                                     .AggregateAsync(next =>
                                     {
                                         try
                                         {
                                             if (next.Value is TItem item)
                                             {
                                                 if (!subscriber.OnNextOrError(item)) return false;
                                             }
                                             return true;
                                         }
                                         finally
                                         {
                                             RecordRead(trackedIndex, next.Id);
                                         }
                                     }).ConfigureAwait(false);

            StopTracking(trackedIndex);
            return retVal;
        }

        protected TrackedIndex StartTracking()
        {
            var trackedIndex = new TrackedIndex();
            trackedIndices.Add(trackedIndex);
            return trackedIndex;
        }

        protected bool StopTracking(TrackedIndex trackedIndex)
        {
            return trackedIndices.Remove(trackedIndex);
        }

        protected bool RecordRead(TrackedIndex trackedIndex, 
                                  long readId)
        {
            trackedIndex.RecordRead(readId);
            return TryEvict(readId);
        }

        protected bool TryEvict(long id)
        {
            if (!CanEvict(id)) return true;

            if (!_cache.Remove(id, out _)) return true;

            if (!deliveryAwaiters.TryGetValue(id, out var taskCompletionSource))
            {
                taskCompletionSource = deliveryAwaiters.GetOrAdd(id, static _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
            }
            return taskCompletionSource.TrySetResult(true);
        }

        private bool CanEvict(long id)
        {
            return trackedIndices.All(index => index.IsRead(id));
        }

        #region Dispose
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _cache.Dispose();
                    trackedIndices.Clear();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        protected class TrackedIndex
        {
            public int Id { get; init; }
            public long? LastReadId { get; private set; }

            private int idSeed = 0;

            public void RecordRead(long id)
            {
                LastReadId = id;
            }

            public bool IsRead(long id)
            {
                return id <= LastReadId;
            }
        }
    }
}
