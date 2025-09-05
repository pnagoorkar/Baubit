using Baubit.Caching;
using Baubit.Collections;
using Baubit.Observation;
using Microsoft.Extensions.Logging;

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

        public bool Publish(T item)
        {
            return CanPublish ? _cache.Add(item, out _) : false;
        }

        public async Task<bool> SubscribeAsync(ISubscriber<T> subscriber,
                                               CancellationToken cancellationToken = default)
        {

            if (!_cache.GetLastOrDefault(out var entry)) return false;
            var deliveryTracker = new DeliveryTracker(entry?.Id);
            deliveryTrackers.Add(deliveryTracker);

            return await _cache.EnumerateEntriesAsync(entry?.Id, cancellationToken)
                               .AggregateAsync(next => 
                               {
                                   return DeliverNext(next, subscriber, deliveryTracker) && 
                                          TryEvict(next.Id);
                               });
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
            public long? FirstId { get; init; }
            public long? LastCompletedId { get; private set; }

            private int idSeed = 0;
            public DeliveryTracker(long? firstId)
            {
                Id = Interlocked.Increment(ref idSeed);
                FirstId = firstId;
            }

            public void RecordComplete(long id)
            {
                LastCompletedId = id;
            }

            public bool IsComplete(long id)
            {
                if (FirstId == null)
                {
                    return id <= LastCompletedId;
                }
                else
                {
                    return id >= FirstId && id <= LastCompletedId;
                }
            }
        }
    }
}
