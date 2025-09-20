using Baubit.Caching;
using Baubit.Collections;
using Baubit.Observation;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Baubit.Aggregation
{
    public class Aggregator<T> : IAggregator<T>
    {
        public bool CanPublish { get => deliveryTrackers.Count > 0; }
        private bool disposedValue;
        private IOrderedCache<T> _cache;

        ConcurrentList<DeliveryTracker> deliveryTrackers = new ConcurrentList<DeliveryTracker>();
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

        public async Task<bool> SubscribeAsync(ISubscriber<T> subscriber,
                                               CancellationToken cancellationToken = default)
        {
            var deliveryTracker = new DeliveryTracker();
            deliveryTrackers.Add(deliveryTracker);

            if (!_cache.GetLastOrDefault(out var entry)) return false;

            var retVal = await _cache.EnumerateEntriesAsync(entry?.Id, cancellationToken)
                                     .AggregateAsync(next =>
                                     {
                                         return DeliverNext(next, subscriber, deliveryTracker) &&
                                                TryEvict(next.Id);
                                     })
                                     .ConfigureAwait(false);

            deliveryTrackers.Remove(deliveryTracker);
            return retVal;
        }

        private bool DeliverNext(IEntry<T> next, 
                                 ISubscriber<T> subscriber, 
                                 DeliveryTracker deliveryTracker)
        {
            var res = subscriber.OnNextOrError(next.Value);
            deliveryTracker.RecordComplete(next.Id);
            return res;
        }

        private bool TryEvict(long id)
        {
            if (deliveryTrackers.All(tracker => tracker.IsComplete(id)))
            {
                _cache.Remove(id, out _);
                if (!deliveryAwaiters.TryGetValue(id, out var taskCompletionSource))
                {
                    taskCompletionSource = deliveryAwaiters.GetOrAdd(id, static _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
                }
                taskCompletionSource.TrySetResult(true);
            }
            return true;
        }

        #region Dispose
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _cache.Dispose();
                    deliveryTrackers.Clear();
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

        private class DeliveryTracker
        {
            public int Id { get; init; }
            public long? LastCompletedId { get; private set; }

            private int idSeed = 0;

            public void RecordComplete(long id)
            {
                LastCompletedId = id;
            }

            public bool IsComplete(long id)
            {
                return id <= LastCompletedId;
            }
        }
    }
}
